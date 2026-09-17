using System.Text.Json.Nodes;

namespace GeoHarness;

public enum CorpusKind { Areal, Linear, Pointish, Unknown }

public sealed record CorpusFeature(int Index, string GeoJsonType, string GeometryJson, JsonNode? Props);

public sealed class CorpusFile
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required CorpusKind Kind { get; init; }
    public required List<CorpusFeature> Features { get; init; }

    /// <summary>同名 .shp/.dbf 文件头独立解析的期望（缺失为 null）。</summary>
    public ShpHeader? ShpExpected { get; set; }
    public long? DbfRecordCount { get; set; }
}

public sealed record ShpHeader(int RecordCount, int ShapeType, List<int> Types)
{
    public string KindName => ShapeType switch
    {
        1 or 11 or 21 => "Point",
        3 or 13 or 23 => "LineString",
        5 or 15 or 25 => "Polygon",
        8 or 18 or 28 => "MultiPoint",
        _ => $"Type{ShapeType}",
    };
}

/// <summary>语料集合：目录内所有 .jsonl/.geojson 文件 + 可选同名 .shp/.dbf 头交叉信息。</summary>
public sealed class CorpusSet
{
    public List<CorpusFile> Files { get; } = new();
    public List<string> LoadErrors { get; } = new();

    public bool IsEmpty => Files.Count == 0;
    public IEnumerable<CorpusFile> OfKind(CorpusKind k) => Files.Where(f => f.Kind == k);
    public CorpusFile? FirstOfKind(CorpusKind k) => OfKind(k).FirstOrDefault();

    public static CorpusSet LoadOrEmpty(string? dataDir)
    {
        var set = new CorpusSet();
        if (dataDir is null || !Directory.Exists(dataDir)) return set;

        var jsonls = Directory.EnumerateFiles(dataDir, "*.jsonl", SearchOption.AllDirectories).ToList();
        var jsonlBases = jsonls.Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.Ordinal);
        foreach (var path in jsonls
                     .Concat(Directory.EnumerateFiles(dataDir, "*.geojson", SearchOption.AllDirectories)
                         .Where(f => !jsonlBases.Contains(Path.GetFileNameWithoutExtension(f))))
                     .Order(StringComparer.Ordinal))
        {
            try { set.Files.Add(LoadFile(path)); }
            catch (Exception ex) { set.LoadErrors.Add($"{Path.GetFileName(path)}: {ex.Message}"); }
        }

        // 同名 .shp/.dbf 文件头交叉信息（不经 GDAL、不经被测库）
        foreach (var f in set.Files)
        {
            var baseName = Path.GetFileNameWithoutExtension(f.Path);
            baseName = baseName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(baseName) : baseName;
            var shp = Directory.EnumerateFiles(dataDir, baseName + ".shp", SearchOption.AllDirectories).FirstOrDefault();
            var dbf = shp is null ? null : Path.ChangeExtension(shp, ".dbf");
            if (shp is not null) { f.ShpExpected = ShpFile.ReadHeader(shp); }
            if (dbf is not null && File.Exists(dbf)) { f.DbfRecordCount = DbfFile.ReadRecordCount(dbf); }
        }
        return set;
    }

    private static CorpusFile LoadFile(string path)
    {
        var feats = new List<CorpusFeature>();
        CorpusKind? kind = null;
        var isJsonl = path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase);

        void Handle(JsonNode node, int idx)
        {
            var (type, geomJson, props) = Extract(node, idx);
            kind ??= Classify(type);
            feats.Add(new CorpusFeature(idx, type, geomJson, props));
        }

        if (isJsonl)
        {
            using var rd = new StreamReader(path);
            string? line; int idx = 0;
            while ((line = rd.ReadLine()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                Handle(JsonNode.Parse(line)!, idx++);
            }
        }
        else
        {
            var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            int idx = 0;
            foreach (var feat in root["features"]!.AsArray())
                Handle(feat!, idx++);
        }

        return new CorpusFile
        {
            Name = Path.GetFileName(path),
            Path = path,
            Kind = kind ?? CorpusKind.Unknown,
            Features = feats,
        };
    }

    private static (string Type, string GeometryJson, JsonNode? Props) Extract(JsonNode node, int idx)
    {
        if (node is JsonObject o && o["type"]?.GetValue<string>() == "Feature")
        {
            var g = o["geometry"] as JsonObject ?? throw new InvalidDataException($"特征 {idx} 无几何");
            return (g["type"]!.GetValue<string>(), g.ToJsonString(), o["properties"] as JsonObject);
        }
        if (node is JsonObject g2 && g2["type"] is not null)
            return (g2["type"]!.GetValue<string>(), g2.ToJsonString(), null);
        throw new InvalidDataException($"特征 {idx} 不是 Feature/Geometry");
    }

    private static CorpusKind Classify(string geoJsonType) => geoJsonType switch
    {
        "Polygon" or "MultiPolygon" => CorpusKind.Areal,
        "LineString" or "MultiLineString" => CorpusKind.Linear,
        "Point" or "MultiPoint" => CorpusKind.Pointish,
        _ => CorpusKind.Unknown,
    };
}

/// <summary>最小 SHP 读取器：头部记录数/类型 + 记录链遍历（8 字节记录头，BE 内容长度）。用于文件头交叉校验。</summary>
public static class ShpFile
{
    public static ShpHeader ReadHeader(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        var all = br.ReadBytes((int)fs.Length);
        if (all.Length < 100) throw new InvalidDataException("SHP 头过短");
        int fileLenWords = Be32(all, 24);
        long fileLenBytes = (long)fileLenWords * 2;
        int type = Le32(all, 32);

        var types = new List<int>();
        long pos = 100;
        while (pos + 8 <= Math.Min(fileLenBytes, all.Length))
        {
            // 记录头: 记录号(BE) + 内容长度(BE, words)
            int contentWords = Be32(all, (int)pos + 4);
            long contentBytes = (long)contentWords * 2;
            pos += 8;
            if (pos + contentBytes > all.Length) break;
            if (contentBytes >= 4)
                types.Add(Le32(all, (int)pos)); // 每条记录的几何类型码
            pos += contentBytes;
        }
        return new ShpHeader(types.Count, type, types);
    }

    private static int Le32(byte[] b, int off) => BitConverter.ToInt32(b, off);
    private static int Be32(byte[] b, int off) => (b[off] << 24) | (b[off + 1] << 16) | (b[off + 2] << 8) | b[off + 3];
}

/// <summary>最小 DBF 读取器：偏移 4 处 uint32 LE = 记录数。</summary>
public static class DbfFile
{
    public static long ReadRecordCount(string path)
    {
        using var fs = File.OpenRead(path);
        var head = new byte[8];
        fs.ReadExactly(head);
        return (uint)(head[4] | head[5] << 8 | head[6] << 16 | head[7] << 24);
    }
}
