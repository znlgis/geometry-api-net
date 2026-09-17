using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// 集合运算（Union/Intersection/Difference/SymmetricDifference）真实数据对拍：
/// 面积与 PostGIS 结果比较；再把库结果 WKT 交 PG 解析做形状一致性（SymDifference 面积≈0）与合法性验证。
/// </summary>
public sealed class SetOpSuite : ISuite
{
    public string Name => "set-ops";
    public bool RequiresData => true;
    public bool RequiresPg => true;

    const int PairCount = 25;

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        var pg = ctx.Pg!;
        var areal = ctx.Corpus.OfKind(CorpusKind.Areal).FirstOrDefault()
                 ?? ctx.Corpus.Files.FirstOrDefault(f => f.Kind != CorpusKind.Unknown);
        var linear = ctx.Corpus.OfKind(CorpusKind.Linear).FirstOrDefault();
        if (areal is null) { r.Skip(Name, "no-corpus", "无语料"); return; }

        var geoms = areal.Features.Select((f, i) => (i, G: LibGeo.FromGeoJson(f.GeometryJson))).ToList();
        var table = LibGeo.Upload(pg, areal);
        var pairs = PickOverlapPairs(r, pg, table, geoms, linear);
        r.Pass(Name, "pairs", $"{pairs.Count} 对");

        foreach (var (ia, ib) in pairs)
        {
            var A = geoms[ia].G; var B = geoms[ib].G;
            string caseTag = $"{areal.Name}#{ia}|#{ib}";
            r.Try(Name, caseTag + ":ops", () =>
            {
                var pgRow = pg.Query($"""
                    SELECT ST_Area(ST_Union(A.g,B.g)), ST_Area(ST_Intersection(A.g,B.g)),
                           ST_Area(ST_Difference(A.g,B.g)), ST_Area(ST_SymDifference(A.g,B.g)),
                           ST_AsText(ST_Union(A.g,B.g)), ST_AsText(ST_Intersection(A.g,B.g)),
                           ST_AsText(ST_Difference(A.g,B.g)), ST_AsText(ST_SymDifference(A.g,B.g))
                    FROM {table} A JOIN {table} B ON A.id={ia} AND B.id={ib}
                    """)[0];

                (string libName, Geometry libRes, double pgArea, string pgWkt)[] ops =
                {
                    ("union", GeometryEngine.Union(A, B), Convert.ToDouble(pgRow[0]), (string)pgRow[4]),
                    ("intersection", GeometryEngine.Intersection(A, B), Convert.ToDouble(pgRow[1]), (string)pgRow[5]),
                    ("difference", GeometryEngine.Difference(A, B), Convert.ToDouble(pgRow[2]), (string)pgRow[6]),
                    ("symdifference", GeometryEngine.SymmetricDifference(A, B), Convert.ToDouble(pgRow[3]), (string)pgRow[7]),
                };
                foreach (var (opName, libRes, pgArea, pgWkt) in ops)
                {
                    if (libRes is null) { r.Fail(Name, $"{caseTag}:{opName}:null", "库返回 null"); continue; }
                    double libArea = libRes.CalculateArea2D();
                    double scale = Math.Max(Math.Abs(pgArea), 1e-12);
                    // 1) 面积对拍（两引擎数值一致）
                    if (Math.Abs(libArea - pgArea) / scale > 1e-4)
                        r.Fail(Name, $"{caseTag}:{opName}:area", $"库 {libArea:R} vs PG {pgArea:R}");
                    // 2) 库结果经 PG 解析合法、回读面积自洽、与 PG 结果形状一致
                    string libWkt = Wkt(GeometryEngine.GeometryToWkt(libRes));
                    var parsed = pg.Query(
                        $"SELECT ST_IsValid(ST_GeomFromText('{libWkt}',4326)), ST_Area(ST_GeomFromText('{libWkt}',4326)), " +
                        $"ST_SymDifference(ST_GeomFromText('{libWkt}',4326), '{Wkt(pgWkt)}'::geometry)");
                    var p0 = parsed[0];
                    if (!(bool)p0[0]) r.Fail(Name, $"{caseTag}:{opName}:lib-wkt-invalid", "库结果 WKT 经 PostGIS 解析为非法几何");
                    double parseBack = Convert.ToDouble(p0[1]);
                    if (Math.Abs(parseBack - libArea) / scale > 1e-6)
                        r.Fail(Name, $"{caseTag}:{opName}:wkt-parseback", $"库面积 {libArea:R} 与 PG 回读 {parseBack:R} 不符");
                    double symd = Convert.ToDouble(p0[2]);
                    if (symd / scale > 1e-5)
                        r.Fail(Name, $"{caseTag}:{opName}:shape", $"与 PG 结果 SymDiff 面积占比 {symd / scale:E} 超限");
                    // 3) 空结果语义一致
                    bool libEmpty = libRes.IsEmpty || libArea <= 1e-14;
                    bool pgEmpty = pgArea <= 1e-14;
                    if (libEmpty != pgEmpty)
                        r.Fail(Name, $"{caseTag}:{opName}:empty-mismatch", $"空语义不一致 libEmpty={libEmpty} pgEmpty={pgEmpty}");
                }
            });
        }
    }

    static string Wkt(string s) => s.Replace("'", "''");

    static List<(int, int)> PickOverlapPairs(CheckRunner r, PgClient pg, string table, List<(int i, Geometry G)> geoms, CorpusFile? linear)
    {
        // 面×面：PG 侧筛出真实相交对（交集体积显著非零），等距排序取前 PairCount 对
        var list = new List<(int, int)>();
        var rows = pg.Query($"""
            SELECT A.id, B.id FROM {table} A JOIN {table} B ON A.id < B.id
            WHERE ST_Intersects(A.g,B.g) AND ST_Area(ST_Intersection(A.g,B.g)) > 1e-8
            ORDER BY A.id, B.id LIMIT {PairCount}
            """);
        foreach (var row in rows) list.Add((Convert.ToInt32(row[0]), Convert.ToInt32(row[1])));
        return list;
    }
}
