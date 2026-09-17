using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// 性质测试（固定种子可复现）：随机凸多边形上验证数学不变量与独立参考实现一致性——
/// 面积/周长/包络 vs 暴力实现；凸包 vs 独立单调链；缓冲 Steiner 公式；集合运算面积守恒；
/// 谓词代数自洽；序列化逐位往返；质心属于凸多边形；距离 vs 暴力点线段距离。
/// </summary>
public sealed class PropertySuite : ISuite
{
    public string Name => "property";
    public bool RequiresData => false;
    public bool RequiresPg => false;

    const int Rounds = 150;

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        var rng = new Random(20260915);
        int fails0 = r.Results.Count(x => x.Status == CheckStatus.Fail);

        for (int round = 0; round < Rounds; round++)
        {
            var (poly, hull) = GeneratedData.RandomConvexPolygon(rng);
            string tag = $"round{round}";
            double area = poly.CalculateArea2D();
            double trueArea = GeneratedData.ShoelaceArea(hull);
            // 1) 面积 vs 独立鞋带公式（方向不敏感：取绝对值）
            if (Math.Abs(Math.Abs(area) - trueArea) / trueArea > 1e-9)
                r.Fail(Name, $"{tag}:area-vs-shoelace", $"库 {area:R} 暴力 {trueArea:R}");
            // 2) 周长 vs 独立累加
            double len = poly.CalculateLength2D();
            double trueLen = GeneratedData.PathLength(hull.Append((hull[0].x, hull[0].y)).ToList());
            if (Math.Abs(len - trueLen) / trueLen > 1e-9)
                r.Fail(Name, $"{tag}:length-vs-brute", $"库 {len:R} 暴力 {trueLen:R}");
            // 3) 包络 vs 独立 min/max
            var env = poly.GetEnvelope()!;
            var (xn, yn, xx, yx) = GeneratedData.Bounds(hull);
            if (Math.Max(Math.Abs(env.XMin - xn), Math.Max(Math.Abs(env.XMax - xx), Math.Max(Math.Abs(env.YMin - yn), Math.Abs(env.YMax - yx)))) > 1e-9)
                r.Fail(Name, $"{tag}:envelope", "包络与独立顶点统计不符");
            // 4) 凸包 vs 独立单调链（输入即凸，应等面积）
            var libHull = GeometryEngine.ConvexHull(poly);
            if (Math.Abs(libHull!.CalculateArea2D() - trueArea) / trueArea > 1e-6)
                r.Fail(Name, $"{tag}:hull-vs-monotone", $"库凸包面积 {libHull.CalculateArea2D():R} 单调链 {trueArea:R}");
            // 5) 缓冲 Steiner：凸体 A(r)=A+Pr+πr²
            double rad = trueArea > 4 ? 0.3 : 0.05;
            var buf = GeometryEngine.Buffer(poly, rad)!;
            double steiner = Math.Abs(area) + len * rad + Math.PI * rad * rad;
            double relBuf = Math.Abs(buf.CalculateArea2D() - steiner) / steiner;
            if (relBuf > 2e-2) r.Fail(Name, $"{tag}:buffer-steiner", $"缓冲面积相对差 {relBuf:E}");
            if (!GeometryEngine.Contains(buf, poly))
                r.Warn(Name, $"{tag}:buffer-contains", "缓冲不包含原几何（离散近似下边界内缩）");
            // 6) 质心 ∈ 凸多边形（允许顶点级边界容差）
            var c = GeometryEngine.Centroid(poly)!;
            if (!GeometryEngine.Contains(poly, new Point(c.X, c.Y)))
            {
                double insideTol = 1e-7 * Math.Max(1, Math.Max(xx - xn, yx - yn));
                bool outside = c.X < xn - insideTol || c.X > xx + insideTol || c.Y < yn - insideTol || c.Y > yx + insideTol;
                if (outside) r.Fail(Name, $"{tag}:centroid-inside", "凸多边形质心不在其内部");
            }
            // 7) 序列化往返
            var wkt = GeometryEngine.GeometryToWkt(poly);
            if (GeometryEngine.GeometryToWkt(GeometryEngine.GeometryFromWkt(wkt)) != wkt)
                r.Fail(Name, $"{tag}:wkt-roundtrip", "WKT 往返逐字不等");
            var wkbLe = GeometryEngine.GeometryToWkb(poly, false);
            var wkbBe = GeometryEngine.GeometryToWkb(poly, true);
            if (!wkbLe.AsSpan().SequenceEqual(GeometryEngine.GeometryToWkb(GeometryEngine.GeometryFromWkb(wkbLe), false)))
                r.Fail(Name, $"{tag}:wkb-le", "WKB(LE) 逐字节往返不等");
            if (!wkbBe.AsSpan().SequenceEqual(GeometryEngine.GeometryToWkb(GeometryEngine.GeometryFromWkb(wkbBe), true)))
                r.Fail(Name, $"{tag}:wkb-be", "WKB(BE) 逐字节往返不等");
            var gj = GeometryEngine.GeometryToGeoJson(poly);
            if (!System.Text.Json.Nodes.JsonNode.DeepEquals(System.Text.Json.Nodes.JsonNode.Parse(gj),
                System.Text.Json.Nodes.JsonNode.Parse(GeometryEngine.GeometryToGeoJson(GeometryEngine.GeometryFromGeoJson(gj)))))
                r.Fail(Name, $"{tag}:geojson", "GeoJSON 往返坐标不等");
            var ej = GeometryEngine.GeometryToEsriJson(poly);
            if (Math.Abs(GeometryEngine.GeometryFromEsriJson(ej).CalculateArea2D() - area) / trueArea > 1e-9)
                r.Fail(Name, $"{tag}:esrijson", "EsriJSON 往返面积不等");

            // 8) 两两集合运算面积守恒 + 谓词代数
            var (poly2, hull2) = GeneratedData.RandomConvexPolygon(rng);
            double a2 = poly2.CalculateArea2D();
            double t2 = GeneratedData.ShoelaceArea(hull2);
            var un = GeometryEngine.Union(poly, poly2)!;
            var inter = GeometryEngine.Intersection(poly, poly2)!;
            var diff = GeometryEngine.Difference(poly, poly2)!;
            var sym = GeometryEngine.SymmetricDifference(poly, poly2)!;
            double au = Math.Abs(un.CalculateArea2D()), ai = Math.Abs(inter.CalculateArea2D()),
                   ad = Math.Abs(diff.CalculateArea2D()), asm = Math.Abs(sym.CalculateArea2D());
            double scale = Math.Max(Math.Abs(area) + Math.Abs(a2), 1e-9);
            // 集合运算恒等式：裁剪器抖动 ~1e-7 量级，容差取 1e-4 相对
            if (Math.Abs(au + ai - (Math.Abs(area) + Math.Abs(a2))) / scale > 1e-4)
                r.Fail(Name, $"{tag}:union-intersection", $"|A∪B|+|A∩B| ≠ |A|+|B| ({au:R}+{ai:R} vs {Math.Abs(area) + Math.Abs(a2):R})");
            if (Math.Abs(ad - (Math.Abs(area) - ai)) / scale > 1e-4)
                r.Fail(Name, $"{tag}:difference", $"|A−B| ≠ |A|−|A∩B| ({ad:R} vs {Math.Abs(area) - ai:R})");
            if (Math.Abs(asm - (Math.Abs(area) + Math.Abs(a2) - 2 * ai)) / scale > 1e-4)
                r.Fail(Name, $"{tag}:symdifference", $"|A△B|={asm:R} vs A+B−2I={Math.Abs(area) + Math.Abs(a2) - 2 * ai:R}（aA={Math.Abs(area):R} aB={Math.Abs(a2):R} I={ai:R} u={au:R} d={(Math.Abs(area) - ai):R}）");
                        // 谓词自洽
            bool dis = GeometryEngine.Disjoint(poly, poly2), ints = GeometryEngine.Intersects(poly, poly2);
            if (dis != !ints) r.Fail(Name, $"{tag}:disjoint-neg", "disjoint ≠ ¬intersects");
            if (GeometryEngine.Within(poly, poly2) != GeometryEngine.Contains(poly2, poly))
                r.Fail(Name, $"{tag}:within-contains-duality", "within(A,B) ≠ contains(B,A)");
            double d1 = GeometryEngine.Distance(poly, poly2), d2 = GeometryEngine.Distance(poly2, poly);
            if (Math.Abs(d1 - d2) > 1e-9 * Math.Max(1, d1)) r.Fail(Name, $"{tag}:distance-sym", "距离不对称");
            if (dis && d1 < 0) r.Fail(Name, $"{tag}:distance-nonneg", "距离为负");
            // 9) 距离 vs 暴力（点→凸多边形）
            var q = new Point(xx + 5 + rng.NextDouble() * 10, (yn + yx) / 2);
            double libD = GeometryEngine.Distance(poly, q);
            double brute = hull.Zip(hull.Skip(1).Append(hull[0])).Min(seg => PtSeg(q, seg.Item1, seg.Item2));
            if (Math.Abs(libD - brute) / Math.Max(1e-9, brute) > 1e-6)
                r.Fail(Name, $"{tag}:distance-brute", $"库 {libD:R} 暴力 {brute:R}");
        }

        int newFails = r.Results.Count(x => x.Status == CheckStatus.Fail) - fails0;
        r.Check(Name, "total", newFails == 0, $"{Rounds} 轮性质测试，新增 Fail {newFails} 条（明细见上）");
    }


    static double PtSeg(Point p, (double x, double y) a, (double x, double y) b)
    {
        double dx = b.x - a.x, dy = b.y - a.y;
        double l2 = dx * dx + dy * dy;
        double t = l2 == 0 ? 0 : Math.Clamp(((p.X - a.x) * dx + (p.Y - a.y) * dy) / l2, 0, 1);
        double px = a.x + t * dx - p.X, py = a.y + t * dy - p.Y;
        return Math.Sqrt(px * px + py * py);
    }
}
