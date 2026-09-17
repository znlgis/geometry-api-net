using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// 加工类算子套件：Clip 对拍 ST_Intersection；Densify/Generalize/Simplify 用独立暴力实现验证
/// 偏差上界与保属性；SimplifyOGC 合法性验证（自相交修复）；Proximity2D/Offset/Buffer 性质验证。
/// 不依赖 PG 的部分（性质类）始终执行。
/// </summary>
public sealed class SimplifyClipSuite : ISuite
{
    public string Name => "simplify-clip";
    public bool RequiresData => true;
    public bool RequiresPg => false;

    const int Cap = 120;

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        var areal = ctx.Corpus.OfKind(CorpusKind.Areal).FirstOrDefault();
        var linear = ctx.Corpus.OfKind(CorpusKind.Linear).FirstOrDefault();

        // ---- 自相交合成几何（SimplifyOGC / IsSimpleOGC）----
        string[] selfIntersecting =
        [
            "POLYGON ((0 0, 10 10, 10 0, 0 10, 0 0))",
            "POLYGON ((0 0, 20 20, 20 0, 0 20, 0 0))",
            "POLYGON ((0 0, 10 0, 5 15, 10 15, 0 10, 15 10, 5 -5, 0 0))",
        ];
        for (int i = 0; i < selfIntersecting.Length; i++)
        {
            var w = selfIntersecting[i];
            var g = GeometryEngine.GeometryFromWkt(w);
            if (GeometryEngine.IsSimpleOGC(g, SpatialRef4326()))
                r.Fail(Name, $"issimple-bowtie#{i}", "自相交几何被判为 simple");
            var fixed_ = GeometryEngine.SimplifyOGC(g, SpatialRef4326());
            if (fixed_ is null) { r.Fail(Name, $"simplifyogc#{i}:null", "SimplifyOGC 返回 null"); continue; }
            if (!GeometryEngine.IsSimpleOGC(fixed_, SpatialRef4326()))
                r.Fail(Name, $"simplifyogc#{i}:still", "修复后仍非 simple");
            var again = GeometryEngine.SimplifyOGC(fixed_, SpatialRef4326());
            r.CheckRel(Name, $"simplifyogc#{i}:idem", again!.CalculateArea2D(), fixed_.CalculateArea2D(), 1e-9, 1e-12);
            if (ctx.Pg is { } pg)
            {
                bool valid = pg.Scalar<bool>($"SELECT ST_IsValid(ST_GeomFromText('{GeometryEngine.GeometryToWkt(fixed_).Replace("'", "''")}',4326))");
                r.Check(Name, $"simplifyogc#{i}:pg-valid", valid, "PG 判定修复结果仍非法");
            }
        }

        // ---- 真实语料合法性回扫 + Clip 对拍 ----
        if (areal is not null && ctx.Pg is { } pgA)
        {
            var table = LibGeo.Upload(pgA, areal);
            int invalid = 0;
            foreach (var row in pgA.Query($"SELECT id FROM {table} WHERE NOT ST_IsValid(g) LIMIT 20"))
            {
                invalid++;
                int id = Convert.ToInt32(row[0]);
                if (GeometryEngine.IsSimpleOGC(LibGeo.FromGeoJson(areal.Features[id].GeometryJson), SpatialRef4326()))
                    r.Warn(Name, $"corpus-invalid#{id}", "PG 判非法但库 IsSimpleOGC 判合法（语义差异样本，入报告）");
            }
            r.Pass(Name, "corpus-validity-scan", $"PG 非法几何 {invalid} 个（详见 WARN）");

            // Clip：用几何中心半尺寸盒裁剪，库结果 vs ST_Intersection
            int stride = Math.Max(1, areal.Features.Count / Cap);
            double maxClipSym = 0;
            foreach (var f in areal.Features.Where((_, i) => i % stride == 0))
            {
                var g = LibGeo.FromGeoJson(f.GeometryJson);
                var env = g.GetEnvelope();
                if (env is null) continue;
                double cx = (env.XMin + env.XMax) / 2, cy = (env.YMin + env.YMax) / 2;
                double hw = (env.XMax - env.XMin) / 4, hh = (env.YMax - env.YMin) / 4;
                var box = new Envelope(cx - hw, cy - hh, cx + hw, cy + hh);
                var clipped = GeometryEngine.Clip(g, box);
                if (clipped is null) { r.Fail(Name, $"clip#{f.Index}:null", "Clip 返回 null"); continue; }
                string libWkt = GeometryEngine.GeometryToWkt(clipped).Replace("'", "''");
                var row = pgA.Query($"""
                    SELECT ST_Area(ST_Intersection(g, ST_MakeEnvelope({N(cx - hw)},{N(cy - hh)},{N(cx + hw)},{N(cy + hh)},4326))),
                           ST_Area(ST_GeomFromText('{libWkt}',4326)),
                           ST_Area(ST_SymDifference(ST_GeomFromText('{libWkt}',4326), ST_Intersection(g, ST_MakeEnvelope({N(cx - hw)},{N(cy - hh)},{N(cx + hw)},{N(cy + hh)},4326))))
                    FROM {table} WHERE id={f.Index}
                    """)[0];
                double pgArea = Convert.ToDouble(row[0]), libArea = Convert.ToDouble(row[1]), symd = Convert.ToDouble(row[2]);
                if (Math.Abs(pgArea - clipped.CalculateArea2D()) / Math.Max(1e-12, pgArea) > 1e-4)
                    r.Fail(Name, $"clip#{f.Index}:area", $"PG {pgArea:R} vs 库 {clipped.CalculateArea2D():R}");
                if (Math.Abs(libArea - clipped.CalculateArea2D()) / Math.Max(1e-12, pgArea) > 1e-6)
                    r.Fail(Name, $"clip#{f.Index}:parseback", "Clip 结果 WKT 经 PG 回读面积不自洽");
                maxClipSym = Math.Max(maxClipSym, symd / Math.Max(1e-12, pgArea));
            }
            r.Check(Name, "clip-worst-sym", maxClipSym <= 1e-4, $"Clip 与 ST_Intersection 最坏形状差占比 {maxClipSym:E}");
        }

        // ---- Densify / Generalize / Simplify 独立暴力性质（线状语料）----
        if (linear is not null)
        {
            int stride = Math.Max(1, linear.Features.Count / Cap);
            foreach (var f in linear.Features.Where((_, i) => i % stride == 0).Take(Cap))
            {
                var g = LibGeo.FromGeoJson(f.GeometryJson);
                if (LibVerts(g) > 800) continue; // 暴力偏差检查 O(n²)，超长折线交给 stress 时限断言
                var paths = g is Polyline pl0 ? pl0.GetPaths().Select(p => p.Select(pt => (pt.X, pt.Y)).ToList()).ToList()
                            : new List<List<(double, double)>> { new() { (0, 0) } };

                // Densify：所有新增长度≤maxLen+eps、总长不变、点数不减
                double maxLen = Math.Max(1e-6, g.CalculateLength2D() / 500);
                var dense = GeometryEngine.Densify(g, maxLen);
                var dpaths = ToPaths(dense);
                bool segOk = true, lenOk = true, grow = true;
                foreach (var p in dpaths)
                    for (int i = 0; i + 1 < p.Count; i++)
                        if (Dist(p[i], p[i + 1]) > maxLen * (1 + 1e-9) + 1e-12) segOk = false;
                lenOk = Math.Abs(dense!.CalculateLength2D() - g.CalculateLength2D()) <= g.CalculateLength2D() * 1e-9;
                grow = dpaths.Sum(p => p.Count) >= paths.Sum(p => p.Count);
                if (!segOk) r.Fail(Name, $"densify#{f.Index}:seg", "存在超过 maxSegmentLength 的加密段");
                if (!lenOk) r.Fail(Name, $"densify#{f.Index}:len", "Densify 改变总长度");
                if (!grow) r.Fail(Name, $"densify#{f.Index}:pts", "Densify 顶点数减少");

                // Generalize：原始顶点到结果的偏差 ≤ maxDeviation（独立点到线段距离暴力实现）
                double maxDev = Math.Max(1e-9, g.CalculateLength2D() / 100);
                var gen = GeometryEngine.Generalize(g, maxDev);
                var gpaths = ToPaths(gen!);
                double worst = 0;
                foreach (var (src, dst) in paths.Zip(gpaths))
                    foreach (var pt in src)
                        worst = Math.Max(worst, BrutePointToPathsDist(pt, dst));
                if (worst > maxDev * (1 + 1e-6) + 1e-9)
                    r.Fail(Name, $"generalize#{f.Index}:dev", $"最大偏移 {worst:R} 超 maxDeviation {maxDev:R}");

                // Simplify(DP)：同上界 + 端点保持
                var simp = GeometryEngine.Simplify(g, maxDev);
                var spaths = ToPaths(simp!);
                double worstS = 0;
                foreach (var (src, dst) in paths.Zip(spaths))
                    foreach (var pt in src)
                        worstS = Math.Max(worstS, BrutePointToPathsDist(pt, dst));
                if (worstS > maxDev * (1 + 1e-6) + 1e-9)
                    r.Fail(Name, $"simplify#{f.Index}:dev", $"DP 最大偏移 {worstS:R} 超容差");
                foreach (var (src, dst) in paths.Zip(spaths))
                    if (src.Count > 1 && (src[0] != dst[0] || src[^1] != dst[^1]))
                    { r.Fail(Name, $"simplify#{f.Index}:endpoints", "DP 端点漂移"); break; }
            }
            r.Pass(Name, "linear-props", "Densify/Generalize/Simplify 性质验证完成");
        }

        // ---- Proximity2D vs 独立暴力最近（真实线+随机点）----
        if (linear is not null)
        {
            var rng = new Random(20260915);
            var baseFeat = linear.Features.First(ft =>
            {
                var gg = LibGeo.FromGeoJson(ft.GeometryJson);
                return LibVerts(gg) <= 2000;
            });
            var baseG = LibGeo.FromGeoJson(baseFeat.GeometryJson);
            var env = baseG.GetEnvelope()!;
            var paths = ToPaths(baseG);
            double span = Math.Max(env.XMax - env.XMin, env.YMax - env.YMin);
            for (int i = 0; i < 30; i++)
            {
                var q = new Point(env.XMin + rng.NextDouble() * span, env.YMin + rng.NextDouble() * span);
                var res = GeometryEngine.GetNearestCoordinate(baseG, q, false);
                double brute = paths.SelectMany(p => SegDistances(q, p)).Min();
                if (!CheckRunner.RelOk(res!.Distance, brute, 1e-6, 1e-9))
                    r.Fail(Name, $"proximity#{i}", $"最近坐标距离 库={res.Distance:R} 暴力={brute:R}");
                var rv = GeometryEngine.GetNearestVertex(baseG, q);
                double bruteV = double.MaxValue;
                foreach (var p in paths) foreach (var v in p) bruteV = Math.Min(bruteV, Dist(q, v));
                if (!CheckRunner.RelOk(rv!.Distance, bruteV, 1e-6, 1e-9))
                    r.Fail(Name, $"proximity-vertex#{i}", $"最近顶点距离 库={rv.Distance:R} 暴力={bruteV:R}");
            }
            r.Pass(Name, "proximity", "Proximity2D 暴力对拍 30 样本");
        }

        // ---- Buffer 性质（点缓冲=近圆）----
        foreach (double rad in new[] { 1.0, 0.25, 7.5 })
        {
            var buf = GeometryEngine.Buffer(new Point(0, 0), rad)!;
            double area = buf.CalculateArea2D();
            double exact = Math.PI * rad * rad;
            r.CheckRel(Name, $"buffer-point-area:{rad}", area, exact, 2e-2, 1e-12);
            foreach (var v in ToPaths(buf).SelectMany(p => p))
                if (Math.Abs(Math.Sqrt(v.x * v.x + v.y * v.y) - rad) > rad * 2e-2)
                { r.Fail(Name, $"buffer-vertex-radial:{rad}", $"缓冲顶点径向距离偏离半径：({v.x},{v.y})"); break; }
        }

        // ---- Offset：直线偏移 |d| 性质 ----
        foreach (double d in new[] { 2.0, -2.0 })
        {
            var line = new Polyline();
            line.AddPath(new[] { new Point(0, 0), new Point(10, 0), new Point(20, 0) });
            var off = GeometryEngine.Offset(line, d);
            if (off is null) { r.Fail(Name, $"offset:{d}", "Offset 返回 null"); continue; }
            var op = ToPaths(off);
            foreach (var p in op)
                foreach (var (x, y) in p)
                    if (Math.Abs(Math.Abs(y) - Math.Abs(d)) > Math.Abs(d) * 1e-6 + 1e-9)
                    { r.Fail(Name, $"offset:{d}:dist", $"偏移顶点 ({x},{y}) 距原线 ≠ |d|={d}"); break; }
        }
    }

    static OpenGIS.Esri.Geometry.Core.SpatialReference.SpatialReference SpatialRef4326() => OpenGIS.Esri.Geometry.Core.SpatialReference.SpatialReference.Wgs84();
    static int LibVerts(Geometry g) => g switch
    {
        Polygon pg => pg.GetRings().Sum(r => r.Count),
        Polyline pl => pl.GetPaths().Sum(p => p.Count),
        MultiPoint mp => mp.Count,
        _ => 1,
    };

    static string N(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    static List<List<(double x, double y)>> ToPaths(Geometry g) => g switch
    {
        Polyline pl => pl.GetPaths().Select(p => p.Select(pt => (pt.X, pt.Y)).ToList()).ToList(),
        Polygon pg => pg.GetRings().Select(p => p.Select(pt => (pt.X, pt.Y)).ToList()).ToList(),
        Point pt => new List<List<(double, double)>> { new() { (pt.X, pt.Y) } },
        _ => new(),
    };

    static double Dist((double x, double y) a, (double x, double y) b) => Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    static double Dist(Point p, (double x, double y) v) => Math.Sqrt((p.X - v.x) * (p.X - v.x) + (p.Y - v.y) * (p.Y - v.y));

    static IEnumerable<double> SegDistances(Point q, List<(double x, double y)> path)
    {
        if (path.Count == 1) { yield return Dist(q, path[0]); yield break; }
        for (int i = 0; i + 1 < path.Count; i++)
            yield return PointSegDist(q, path[i], path[i + 1]);
    }

    static double BrutePointToPathsDist((double x, double y) pt, List<(double x, double y)> path)
    {
        if (path.Count == 1) return Math.Sqrt((pt.x - path[0].x) * (pt.x - path[0].x) + (pt.y - path[0].y) * (pt.y - path[0].y));
        double best = double.MaxValue;
        for (int i = 0; i + 1 < path.Count; i++)
            best = Math.Min(best, PointSegDist(pt, path[i], path[i + 1]));
        return best;
    }

    static double PointSegDist((double x, double y) p, (double x, double y) a, (double x, double y) b)
    {
        double dx = b.x - a.x, dy = b.y - a.y;
        double len2 = dx * dx + dy * dy;
        double t = len2 == 0 ? 0 : Math.Clamp(((p.x - a.x) * dx + (p.y - a.y) * dy) / len2, 0, 1);
        double px = a.x + t * dx - p.x, py = a.y + t * dy - p.y;
        return Math.Sqrt(px * px + py * py);
    }

    static double PointSegDist(Point p, (double x, double y) a, (double x, double y) b)
    {
        double dx = b.x - a.x, dy = b.y - a.y;
        double len2 = dx * dx + dy * dy;
        double t = len2 == 0 ? 0 : Math.Clamp(((p.X - a.x) * dx + (p.Y - a.y) * dy) / len2, 0, 1);
        double px = a.x + t * dx - p.X, py = a.y + t * dy - p.Y;
        return Math.Sqrt(px * px + py * py);
    }
}
