using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// 大地测量对拍：GeodesicDistance vs ST_Distance(geography)（Vincenty vs Karney，理论差毫米级），
/// GeodesicArea vs ST_Area(geography)（球面过量法 vs 椭球积分，方法学差异按实测容差断言并文档化）。
/// </summary>
public sealed class GeodesicSuite : ISuite
{
    public string Name => "geodesic";
    public bool RequiresData => true;
    public bool RequiresPg => true;

    const int PointCap = 600;
    const int AreaCap = 40;

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        var pg = ctx.Pg!;
        var points = ctx.Corpus.OfKind(CorpusKind.Pointish).FirstOrDefault();
        if (points is not null)
        {
            var table = LibGeo.Upload(pg, points);
            int stride = Math.Max(1, points.Features.Count / PointCap);
            int n = 0;
            double worstRel = 0, worstAbs = 0;
            var pts = pg.Query($"SELECT id, ST_X(g), ST_Y(g) FROM {table} WHERE id % {stride}=0 ORDER BY id")
                        .Select(x => (Id: Convert.ToInt32(x[0]), X: Convert.ToDouble(x[1]), Y: Convert.ToDouble(x[2]))).ToList();
            for (int i = 0; i + 1 < pts.Count; i += 2)
            {
                var (id1, x1, y1) = pts[i];
                var (id2, x2, y2) = pts[i + 1];
                if (x1 == x2 && y1 == y2) continue;
                n++;
                double lib = GeometryEngine.GeodesicDistance(new Point(x1, y1), new Point(x2, y2));
                double pgd = pg.Scalar<double>($"SELECT ST_Distance(ST_SetSRID(ST_MakePoint({D(x1)},{D(y1)}),4326)::geography, ST_SetSRID(ST_MakePoint({D(x2)},{D(y2)}),4326)::geography)");
                double ad = Math.Abs(lib - pgd);
                worstAbs = Math.Max(worstAbs, ad);
                worstRel = Math.Max(worstRel, ad / Math.Max(1.0, pgd));
                if (ad > Math.Max(1.0, pgd * 1e-6))
                    r.Fail(Name, $"dist:{id1}-{id2}", $"Vincenty={lib:F4}m Karney={pgd:F4}m 差 {ad:F6}m");
            }
            r.Check(Name, "distance-worst", worstRel <= 1e-5 && worstAbs <= 1.0,
                $"最坏差异 rel={worstRel:E} abs={worstAbs:F4}m（样本 {n}）");
            r.Pass(Name, "distance-samples", $"{n} 对大地距离样本");
        }

        var areal = ctx.Corpus.OfKind(CorpusKind.Areal).FirstOrDefault();
        if (areal is not null)
        {
            var table = LibGeo.Upload(pg, areal);
            int stride = Math.Max(1, areal.Features.Count / AreaCap);
            int m = 0; double worstAreaRel = 0;
            foreach (var row in pg.Query($"SELECT id FROM {table} WHERE id % {stride}=0 ORDER BY id LIMIT {AreaCap * stride}"))
            {
                int id = Convert.ToInt32(row[0]);
                if (id >= areal.Features.Count) continue;
                if (areal.Features[id].GeometryJson.Length > 200_000) continue; // 控制 PG 侧耗时
                var g = LibGeo.FromGeoJson(areal.Features[id].GeometryJson);
                if (g is not Polygon poly) continue;
                m++;
                double lib = GeometryEngine.GeodesicArea(poly);
                double pgd = pg.Scalar<double>($"SELECT ST_Area(g::geography) FROM {table} WHERE id={id}");
                double rel = Math.Abs(lib - pgd) / Math.Max(1.0, pgd);
                worstAreaRel = Math.Max(worstAreaRel, rel);
            }
            // 球面过量法与椭球积分的方法学差异：实测校准后固化阈值（首轮跑完写进报告）
            r.Check(Name, "area-worst-rel", worstAreaRel <= 1.2e-2,
                $"GeodesicArea 与 ST_Area(geography) 最坏相对差 {worstAreaRel:E}（样本 {m}，阈值 1.2e-2——球面过量法与椭球积分的方法学差异上界，见报告 S-3）");
            r.Pass(Name, "area-samples", $"{m} 个大面样本");
        }
    }

    static string D(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
}
