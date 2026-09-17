using System.Text.Json.Nodes;

namespace GeoHarness;

/// <summary>套件接口。RequiresData/RequiresPg 为真而上下文缺失时由框架统一记 Skip。</summary>
public interface ISuite
{
    string Name { get; }
    bool RequiresData { get; }
    bool RequiresPg { get; }
    void Run(CheckRunner runner, HarnessContext ctx);
}

public sealed class HarnessContext
{
    public string? DataDir { get; init; }
    public PgClient? Pg { get; init; }
    public string? CasesFile { get; init; }
    /// <summary>语料抽样上限（0=默认）。大语料按等距 stride 抽样以控制运行时长。</summary>
    public int Sample { get; init; }
    public bool Verbose { get; init; }

    private CorpusSet? _corpus;
    public CorpusSet Corpus => _corpus ??= CorpusSet.LoadOrEmpty(DataDir);

    public int StrideFor(int total) => Sample > 0 && total > Sample ? (int)Math.Ceiling(total / (double)Sample) : 1;

    public static IEnumerable<ISuite> AllSuites() =>
    [
        new Suites.OgcPredicateSuite(),
        new Suites.RealUnarySuite(),
        new Suites.RealPairSuite(),
        new Suites.SetOpSuite(),
        new Suites.IoRoundtripSuite(),
        new Suites.GeodesicSuite(),
        new Suites.SimplifyClipSuite(),
        new Suites.StressSuite(),
        new Suites.PropertySuite(),
    ];
}

/// <summary>GeoJSON 独立结构解析（不经被测库）：提取类型与顶点数，用于交叉校验导入结果。</summary>
public static class GeoJsonWalker
{
    public static (string Type, int VertexCount, int CoordDims, bool HasZ) Walk(string geoJson)
    {
        var node = JsonNode.Parse(geoJson) ?? throw new InvalidDataException("null json");
        var (type, geom) = UnwrapFeature(node);
        int verts = 0, dims = 0; bool hasZ = false;
        switch (type)
        {
            case "Point":
                (verts, dims, hasZ) = CountCoord(geom["coordinates"]!.AsArray());
                break;
            case "MultiPoint":
            case "LineString":
                foreach (var c in geom["coordinates"]!.AsArray())
                { var t = CountCoord(c!.AsArray()); verts += t.Vertices; dims = Math.Max(dims, t.Dims); hasZ |= t.HasZ; }
                break;
            case "MultiLineString":
            case "Polygon":
                foreach (var ring in geom["coordinates"]!.AsArray())
                    foreach (var c in ring!.AsArray())
                    { var t = CountCoord(c!.AsArray()); verts += t.Vertices; dims = Math.Max(dims, t.Dims); hasZ |= t.HasZ; }
                break;
            case "MultiPolygon":
                foreach (var poly in geom["coordinates"]!.AsArray())
                    foreach (var ring in poly!.AsArray())
                        foreach (var c in ring!.AsArray())
                        { var t = CountCoord(c!.AsArray()); verts += t.Vertices; dims = Math.Max(dims, t.Dims); hasZ |= t.HasZ; }
                break;
            case "GeometryCollection":
                foreach (var g in geom["geometries"]!.AsArray())
                { var t = Walk(((JsonNode)g!).ToJsonString()); verts += t.VertexCount; dims = Math.Max(dims, t.CoordDims); hasZ |= t.HasZ; }
                break;
            default:
                throw new InvalidDataException($"未知 GeoJSON 类型: {type}");
        }
        return (type, verts, dims, hasZ);
    }

    private static (string, JsonObject) UnwrapFeature(JsonNode node)
    {
        var obj = node as JsonObject ?? throw new InvalidDataException("json 不是对象");
        var t = obj["type"]?.GetValue<string>() ?? "";
        if (t == "Feature")
        {
            var g = obj["geometry"] as JsonObject ?? throw new InvalidDataException("Feature 无 geometry");
            return (g["type"]!.GetValue<string>(), g);
        }
        return (t, obj);
    }

    private static (int Vertices, int Dims, bool HasZ) CountCoord(JsonArray arr)
        => (1, arr.Count, arr.Count >= 3);
}
