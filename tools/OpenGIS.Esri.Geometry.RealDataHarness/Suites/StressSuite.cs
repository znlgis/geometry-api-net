using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// 大规模生成几何：性能阈值断言（超时即 FAIL，可在 CI 复现性能回归）+ 大图元数值正确性。
/// 单项阈值默认 5s（构造/一元）、IO 20s、集合运算 60s，可环境变量 GEOM_STRESS_FACTOR 缩放。
/// </summary>
public sealed class StressSuite : ISuite
{
    public string Name => "stress";
    public bool RequiresData => false;
    public bool RequiresPg => false;

    static double K => double.TryParse(Environment.GetEnvironmentVariable("GEOM_STRESS_FACTOR"), out var v) && v > 0 ? v : 1.0;

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        // ---- 20 万顶点圆多边形 ----
        const int N = 200_000, R = 90;
        var circle = r.Timed(Name, "build:200k-circle", 10_000 * K, () => GeneratedData.CirclePolygon(N, R));
        r.CheckRel(Name, "area:200k-circle", Math.Abs(circle.CalculateArea2D()), GeneratedData.InsccribedCircleArea(N, R), 1e-9, 1e-6);
        double perim = r.Timed(Name, "length:200k", 5_000 * K, () => circle.CalculateLength2D());
        r.CheckRel(Name, "length:200k", perim, 2 * Math.PI * R, 1e-4);
        var env = circle.GetEnvelope()!;
        r.Check(Name, "envelope:200k", Math.Abs(env.XMax - R) < 1e-9 && Math.Abs(env.YMin + R) < 1e-9, "包络与半径不符");
        r.Timed(Name, "centroid:200k", 5_000 * K, () => GeometryEngine.Centroid(circle));
        var hull = r.Timed(Name, "hull:200k", 10_000 * K, () => GeometryEngine.ConvexHull(circle));
        r.CheckRel(Name, "hull:200k-area", hull!.CalculateArea2D(), GeneratedData.InsccribedCircleArea(N, R), 1e-6);
        var buf = r.Timed(Name, "buffer:200k", 30_000 * K, () => GeometryEngine.Buffer(circle, 1.0));
        r.Check(Name, "buffer:200k-area-grows", buf!.CalculateArea2D() > circle.CalculateArea2D(), "缓冲面积未增大");
        var clipBox = new Envelope(-50, -50, 50, 50);
        var clipped = r.Timed(Name, "clip:200k", 10_000 * K, () => GeometryEngine.Clip(circle, clipBox));
        r.CheckRel(Name, "clip:200k-area", Math.Abs(clipped!.CalculateArea2D()), 10000.0, 1e-6, 1e-6); // 裁剪盒(100x100)整体含于圆内 → 面积为盒面积
        string wkt = r.Timed(Name, "wkt-export:200k", 20_000 * K, () => GeometryEngine.GeometryToWkt(circle));
        var back = r.Timed(Name, "wkt-import:200k", 30_000 * K, () => GeometryEngine.GeometryFromWkt(wkt));
        r.CheckRel(Name, "wkt-roundtrip-area", back.CalculateArea2D(), circle.CalculateArea2D(), 1e-9);
        byte[] wkb = r.Timed(Name, "wkb-export:200k", 10_000 * K, () => GeometryEngine.GeometryToWkb(circle, false));
        var wkbBack = r.Timed(Name, "wkb-import:200k", 20_000 * K, () => GeometryEngine.GeometryFromWkb(wkb));
        r.CheckRel(Name, "wkb-roundtrip-area", wkbBack.CalculateArea2D(), circle.CalculateArea2D(), 1e-9);
        string gj = r.Timed(Name, "geojson-export:200k", 20_000 * K, () => GeometryEngine.GeometryToGeoJson(circle));
        var gjBack = r.Timed(Name, "geojson-import:200k", 30_000 * K, () => GeometryEngine.GeometryFromGeoJson(gj));
        r.CheckRel(Name, "geojson-roundtrip-area", gjBack.CalculateArea2D(), circle.CalculateArea2D(), 1e-9);

        // ---- 大图元两两 ----
        var circle2 = GeneratedData.CirclePolygon(20_000, R, cx: R * 1.5);
        r.Timed(Name, "intersects:20k+20k", 30_000 * K, () => GeometryEngine.Intersects(circle2, GeneratedData.CirclePolygon(20_000, R, cx: R * 1.5 + 5)));
        var u = r.Timed(Name, "union:20k+20k", 60_000 * K, () => GeometryEngine.Union(circle2, GeneratedData.CirclePolygon(20_000, R, cx: R * 1.5 - 20)));
        r.Check(Name, "union:20k+20k-nonempty", u is not null && !u.IsEmpty && u.CalculateArea2D() > circle2.CalculateArea2D(), "并集为空或不增大");
        double dist = r.Timed(Name, "distance:200k-pt", 20_000 * K, () => GeometryEngine.Distance(circle, new Point(500, 500)));
        r.CheckRel(Name, "distance:200k-pt", dist, Math.Sqrt(2) * 500 - R, 1e-6); // 点到圆 = |PC| − r

        // ---- 千洞多边形 ----
        var donut = r.Timed(Name, "build:1000-holes", 10_000 * K, () => GeneratedData.DonutWithHoles(1000));
        double dArea = r.Timed(Name, "area:1000-holes", 5_000 * K, () => donut.CalculateArea2D());
        r.CheckRel(Name, "area:1000-holes", Math.Abs(dArea), 1000 * 1000 - 1000 * 4, 1e-3);
        r.Timed(Name, "simplifyogc:1000-holes", 30_000 * K, () => GeometryEngine.SimplifyOGC(donut, OpenGIS.Esri.Geometry.Core.SpatialReference.SpatialReference.Wgs84()));

        // ---- 多部件多边形 ----
        var multi = r.Timed(Name, "build:2000-parts", 10_000 * K, () => GeneratedData.MultiPartSquares(2000));
        string mw = r.Timed(Name, "wkt:2000-parts", 20_000 * K, () => GeometryEngine.GeometryToWkt(multi));
        var mb = GeometryEngine.GeometryFromWkt(mw);
        r.CheckRel(Name, "wkt:2000-parts-area", Math.Abs(mb.CalculateArea2D()), Math.Abs(multi.CalculateArea2D()), 1e-9);

        // ---- 10 万段折线 ----
        var zig = r.Timed(Name, "build:100k-polyline", 10_000 * K, () => GeneratedData.ZigzagPolyline(100_000));
        double zl = r.Timed(Name, "length:100k", 5_000 * K, () => zig.CalculateLength2D());
        r.CheckRel(Name, "length:100k", zl, 99_999 * Math.Sqrt(0.01 * 0.01 + 1), 1e-9);
        r.Timed(Name, "generalize:100k", 10_000 * K, () => GeometryEngine.Generalize(zig, 0.25));
        r.Timed(Name, "densify:100k", 20_000 * K, () => GeometryEngine.Densify(zig, 0.001));

        // ---- 自相交螺旋：SimplifyOGC 修复时限 ----
        var spiral = GeneratedData.SelfIntersectingSpiral();
        var fixedS = r.Timed(Name, "simplifyogc:spiral", 30_000 * K, () => GeometryEngine.SimplifyOGC(spiral, OpenGIS.Esri.Geometry.Core.SpatialReference.SpatialReference.Wgs84()));
        r.Check(Name, "simplifyogc:spiral-fixed", fixedS is not null && GeometryEngine.IsSimpleOGC(fixedS, OpenGIS.Esri.Geometry.Core.SpatialReference.SpatialReference.Wgs84()), "自相交修复后仍非 simple");

        // ---- 退化几何 ----
        var zline = new Line(new Point(1, 1), new Point(1, 1));
        r.Check(Name, "degenerate:zerolength-length", zline.Length == 0, "零长线段长度非 0");
        r.Try(Name, "degenerate:zerolength-ops", () =>
        {
            GeometryEngine.Buffer(zline, 2);
            GeometryEngine.ConvexHull(zline);
            GeometryEngine.Centroid(zline);
            GeometryEngine.Distance(zline, new Point(5, 5));
            GeometryEngine.Intersects(zline, new Point(1, 1));
        });
        var dupPoly = new Polygon();
        dupPoly.AddRing(new[] { new Point(0, 0), new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0) });
        r.Try(Name, "degenerate:duplicate-vertex", () =>
        {
            double a = dupPoly.CalculateArea2D();
            r.Check(Name, "degenerate:duplicate-vertex-area", Math.Abs(Math.Abs(a) - 100) < 1e-9, $"重复点面积 {a:R}");
        });
        r.Try(Name, "degenerate:buffer-zero", () => GeometryEngine.Buffer(new Point(0, 0), 0));
        r.Try(Name, "degenerate:empty-wkt-import", () =>
        {
            var e = GeometryEngine.GeometryFromWkt("POINT EMPTY");
            _ = e is null || e.IsEmpty;
        });

        // ---- 大规模 + PG 数值抽验 ----
        if (ctx.Pg is { } pg)
        {
            var mid = GeneratedData.CirclePolygon(20_000, 90);
            string mgj = GeometryEngine.GeometryToGeoJson(mid);
            double pgArea = pg.Scalar<double>("SELECT ST_Area(ST_SetSRID(ST_GeomFromGeoJSON(:g),4326))", ("g", (object)mgj));
            r.CheckRel(Name, "pg-area:20k-circle", mid.CalculateArea2D(), pgArea, 1e-6);
        }
    }
}
