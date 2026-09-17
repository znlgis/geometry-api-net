using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// OGC SF1.1999.7 风格标准谓词用例：期望值为人工按 9-intersection 推导（第一权威源）。
/// 被测库必须与期望一致（Fail）；PostGIS 可用时作为第二独立引擎参与对拍，
/// 若 PostGIS 与期望不一致但与库一致 → 记 Warn（GEOS 实现差异，如 within(A,A)）；
/// 若 PostGIS 与期望一致而库不一致 → 库 Fail（已由前者覆盖）。
/// </summary>
public sealed class OgcPredicateSuite : ISuite
{
    public string Name => "ogc-predicates";
    public bool RequiresData => false;
    public bool RequiresPg => false;

    public sealed record Case(string Id, string Desc, string WktA, string WktB,
        Dictionary<string, bool?> Expected, double Distance);

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        if (ctx.CasesFile is null || !File.Exists(ctx.CasesFile))
        {
            r.Skip(Name, "cases-file", "未找到 testcases/ogc-sf-relate-cases.tsv");
            return;
        }
        var cases = ParseCases(File.ReadAllLines(ctx.CasesFile));
        r.Pass(Name, "cases-loaded", $"{cases.Count} 例");

        // 库侧
        foreach (var c in cases)
        {
            r.Try(Name, $"{c.Id}:exec", () =>
            {
                var a = GeometryEngine.GeometryFromWkt(c.WktA);
                var b = GeometryEngine.GeometryFromWkt(c.WktB);
                CheckPredicates(r, "lib", c,
                    GeometryEngine.Contains(a, b), GeometryEngine.Within(a, b), GeometryEngine.Crosses(a, b),
                    GeometryEngine.Touches(a, b), GeometryEngine.Overlaps(a, b), GeometryEngine.Equals(a, b),
                    GeometryEngine.Disjoint(a, b), GeometryEngine.Intersects(a, b),
                    GeometryEngine.Distance(a, b));
            });
        }

        // PG 侧（同一期望表三方对拍）
        if (ctx.Pg is { } pg)
        {
            var table = pg.TrackTable("geom_test_ogc_cases");
            pg.Exec($"DROP TABLE IF EXISTS {table}; CREATE TABLE {table}(id text, a geometry, b geometry)");
            foreach (var c in cases)
                pg.Exec($"INSERT INTO {table} VALUES ('{c.Id.Replace("'", "''")}', ST_GeomFromText('{c.WktA}',4326), ST_GeomFromText('{c.WktB}',4326))");
            var rows = pg.Query($"""
                SELECT id,
                  ST_Contains(a,b), ST_Within(a,b), ST_Crosses(a,b), ST_Touches(a,b), ST_Overlaps(a,b),
                  ST_Equals(a,b), ST_Disjoint(a,b), ST_Intersects(a,b), ST_Distance(a,b)
                FROM {table} ORDER BY id
                """);
            var byId = rows.ToDictionary(x => (string)x[0], x => x);
            foreach (var c in cases)
            {
                if (!byId.TryGetValue(c.Id, out var row)) { r.Warn(Name, $"{c.Id}:pg-missing", "PG 无此用例"); continue; }
                CheckPredicates(r, "pg", c,
                    (bool)row[1], (bool)row[2], (bool)row[3], (bool)row[4], (bool)row[5],
                    (bool)row[6], (bool)row[7], (bool)row[8], Convert.ToDouble(row[9]));
            }
        }
    }

    private void CheckPredicates(CheckRunner r, string side, Case c,
        bool contains, bool within, bool crosses, bool touches, bool overlaps, bool equals,
        bool disjoint, bool intersects, double distance)
    {
        Check1(r, side, c, "contains", contains, c.Expected["contains"]);
        Check1(r, side, c, "within", within, c.Expected["within"]);
        Check1(r, side, c, "crosses", crosses, c.Expected["crosses"]);
        Check1(r, side, c, "touches", touches, c.Expected["touches"]);
        Check1(r, side, c, "overlaps", overlaps, c.Expected["overlaps"]);
        Check1(r, side, c, "equals", equals, c.Expected["spatial_equals"]);
        Check1(r, side, c, "disjoint", disjoint, c.Expected["disjoint"]);
        Check1(r, side, c, "intersects", intersects, c.Expected["intersects"]);
        string suffix = side == "pg" ? ":pg-diff" : "";
        if (side == "lib")
            r.CheckRel(Name, $"{c.Id}:distance", distance, c.Distance, 1e-9, 1e-9);
        else if (!CheckRunner.RelOk(distance, c.Distance, 1e-9, 1e-9))
            r.Warn(Name, $"{c.Id}:distance{suffix}", $"PG 距离={distance:R} 期望={c.Distance:R}");
    }

    private static void Check1(CheckRunner r, string side, Case c, string pred, bool actual, bool? expected)
    {
        if (expected is null) return; // '-' 不强制
        string name = $"{c.Id}:{pred}:{side}";
        if (actual == expected) { if (side == "lib") r.Pass("ogc-predicates", name); else r.Pass("ogc-predicates-pg-ok", name); return; }
        if (side == "lib")
            r.Fail("ogc-predicates", name, $"{c.Desc}: 库={actual} 期望={expected}");
        else
            r.Warn("ogc-predicates-pgdiff", name, $"{c.Desc}: GEOS={actual} 期望={expected}（GEOS 实现差异，见语料头注释）");
    }

    public static List<Case> ParseCases(string[] lines)
    {
        var list = new List<Case>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var f = line.Split('\t');
            if (f.Length < 13 || f[0] == "id") continue;
            var expected = new Dictionary<string, bool?>
            {
                ["contains"] = ParseBool(f[4]), ["within"] = ParseBool(f[5]), ["crosses"] = ParseBool(f[6]),
                ["touches"] = ParseBool(f[7]), ["overlaps"] = ParseBool(f[8]), ["spatial_equals"] = ParseBool(f[9]),
                ["disjoint"] = ParseBool(f[10]), ["intersects"] = ParseBool(f[11]),
            };
            list.Add(new Case(f[0], f[1], f[2], f[3], expected, double.Parse(f[12], System.Globalization.CultureInfo.InvariantCulture)));
        }
        return list;
    }

    private static bool? ParseBool(string s) => s == "1" ? true : s == "0" ? false : null;
}
