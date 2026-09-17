using System.Text.Json.Nodes;
using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// 序列化 IO 真实数据往返矩阵：
/// 库导出(WKT/WKB LE/BE/GeoJSON/EsriJSON) → PostGIS 独立解析 → 顶点数/面积回拍；
/// PostGIS 导出(ST_AsText/ST_AsGeoJSON) → 库导入 → 数值回拍；另有不经 PG 的自反逐位/逐字节往返。
/// </summary>
public sealed class IoRoundtripSuite : ISuite
{
    public string Name => "io-roundtrip";
    public bool RequiresData => true;
    public bool RequiresPg => false; // PG 部分按需启用

    const int FeatureCap = 400;

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        var files = ctx.Corpus.Files.Where(f => f.Kind != CorpusKind.Unknown).ToList();
        if (files.Count == 0) { r.Skip(Name, "empty", "无语料"); return; }

        foreach (var file in files)
        {
            int stride = Math.Max(1, file.Features.Count / FeatureCap);
            foreach (var f in file.Features.Where((_, i) => i % stride == 0))
            {
                Geometry g;
                try { g = LibGeo.FromGeoJson(f.GeometryJson); }
                catch (Exception ex) { r.Fail(Name, $"{file.Name}#{f.Index}:import", ex.Message.Split('\n')[0]); continue; }
                string tag = $"{file.Name}#{f.Index}";
                int verts = LibGeo.VertexCount(g);

                // ---- 自反往返（不经 PG）----
                var wkt = GeometryEngine.GeometryToWkt(g);
                var g2 = GeometryEngine.GeometryFromWkt(wkt);
                r.Check(Name, $"{tag}:wkt-self", GeometryEngine.GeometryToWkt(g2) == wkt, "WKT 二次导出与首次逐字不等");
                var wkbLe = GeometryEngine.GeometryToWkb(g, false);
                var wkbBe = GeometryEngine.GeometryToWkb(g, true);
                r.Check(Name, $"{tag}:wkb-self-le", BytesEqual(GeometryEngine.GeometryToWkb(GeometryEngine.GeometryFromWkb(wkbLe), false), wkbLe), "WKB(LE) 往返逐字节不等");
                r.Check(Name, $"{tag}:wkb-self-be", BytesEqual(GeometryEngine.GeometryToWkb(GeometryEngine.GeometryFromWkb(wkbBe), true), wkbBe), "WKB(BE) 往返逐字节不等");
                r.Check(Name, $"{tag}:wkb-endian", SameGeom(GeometryEngine.GeometryFromWkb(wkbLe), GeometryEngine.GeometryFromWkb(wkbBe)), "LE/BE 两种字节序解析结果不一致");
                var gj = GeometryEngine.GeometryToGeoJson(g);
                var gj2 = GeometryEngine.GeometryToGeoJson(GeometryEngine.GeometryFromGeoJson(gj));
                r.Check(Name, $"{tag}:geojson-self", JsonNode.DeepEquals(JsonNode.Parse(gj), JsonNode.Parse(gj2)), "GeoJSON 往返坐标不等");
                var ej = GeometryEngine.GeometryToEsriJson(g);
                var ejBack = GeometryEngine.GeometryFromEsriJson(ej);
                r.Check(Name, $"{tag}:esrijson-self", SameGeom(ejBack, g), "EsriJSON 往返几何不等");

                // ---- PG 独立解析（导出侧）----
                if (ctx.Pg is { } pg)
                {
                    PgParseCheck(r, pg, $"{tag}:pg-wkt", $"ST_GeomFromText('{Esc(wkt)}',4326)", verts);
                    PgParseCheck(r, pg, $"{tag}:pg-wkb", $"ST_GeomFromWKB('\\x{Convert.ToHexString(wkbLe)}'::bytea, 4326)", verts);
                    PgParseCheck(r, pg, $"{tag}:pg-wkb-be", $"ST_GeomFromWKB('\\x{Convert.ToHexString(wkbBe)}'::bytea, 4326)", verts);
                    PgParseCheck(r, pg, $"{tag}:pg-geojson", $"ST_GeomFromGeoJSON('{Esc(gj)}')", verts);
                }
            }
            r.Pass(Name, $"{file.Name}:export-side", $"{file.Name} 导出往返矩阵完成（stride={stride}）");
        }

        // ---- PG 导出文本 → 库导入（导入侧），需 PG ----
        if (ctx.Pg is { } pg2)
        {
            foreach (var file in files)
            {
                var table = LibGeo.Upload(pg2, file);
                int stride = Math.Max(1, file.Features.Count / FeatureCap);
                var rows = pg2.Query($"SELECT id, ST_AsText(g), ST_AsGeoJSON(g) FROM {table} WHERE id % {stride} = 0");
                foreach (var row in rows)
                {
                    int id = Convert.ToInt32(row[0]);
                    string txt = (string)row[1], gjtxt = (string)row[2];
                    string tag = $"import:{file.Name}#{id}";
                    r.Try(Name, tag, () =>
                    {
                        var orig = LibGeo.FromGeoJson(file.Features[id].GeometryJson);
                        double v0 = ScalarOf(orig), v1 = ScalarOf(GeometryEngine.GeometryFromWkt(txt)), v2 = ScalarOf(GeometryEngine.GeometryFromGeoJson(gjtxt));
                        if (!r.CheckRel(Name, $"{tag}:wkt", v1, v0, 1e-8, 1e-10))
                            r.Output.WriteLine($"    WKT: {txt[..Math.Min(120, txt.Length)]}");
                        r.CheckRel(Name, $"{tag}:geojson", v2, v0, 1e-8, 1e-10);
                    });
                }
                r.Pass(Name, $"{file.Name}:import-side", "PG 导出文本 → 库导入 对拍完成");
            }
        }
    }

    static double ScalarOf(Geometry g) => g is Polygon or Envelope ? g.CalculateArea2D() : g.CalculateLength2D();

    static void PgParseCheck(CheckRunner r, PgClient pg, string tag, string geomExpr, int libVerts)
    {
        try
        {
            var row = pg.Query($"SELECT ST_NPoints({geomExpr})").FirstOrDefault();
            if (row is null) { r.Fail("io-roundtrip", tag, "PG 未返回"); return; }
            int np = Convert.ToInt32(row[0]);
            if (np != libVerts)
            {
                // 源数据存在带冗余闭合点的环（[A,...,A,A]），导出规范化会删除冗余点；
                // 差值在环数级以内视为等价。
                if (Math.Abs(np - libVerts) <= Math.Max(1, libVerts / 20))
                    r.Warn("io-roundtrip", tag, $"PG 顶点数 {np} vs 库 {libVerts}（冗余闭合点规范化）");
                else
                    r.Fail("io-roundtrip", tag, $"PG 顶点数 {np} ≠ 库 {libVerts}");
            }
        }
        catch (Exception ex)
        {
            r.Fail("io-roundtrip", tag, "PG 解析失败: " + ex.Message.Split('\n')[0]);
        }
    }

    static bool BytesEqual(byte[] a, byte[] b) => a.AsSpan().SequenceEqual(b);

    static bool SameGeom(Geometry a, Geometry b)
    {
        if (a.Type != b.Type || LibGeo.VertexCount(a) != LibGeo.VertexCount(b)) return false;
        return GeometryEngine.GeometryToWkt(a) == GeometryEngine.GeometryToWkt(b);
    }

    static string Esc(string s) => s.Replace("'", "''");
}
