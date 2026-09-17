using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness;

/// <summary>生成式数据（压力几何 + 随机性质样本 + 独立暴力参考实现）。</summary>
public static class GeneratedData
{
    /// <summary>近似圆多边形：n 顶点均匀分布（大面积压力样本）。真值面积 = πR² 的内接正多边形面积。</summary>
    public static Polygon CirclePolygon(int n, double radius, double cx = 0, double cy = 0, double noise = 0)
    {
        var pts = new Point[n + 1];
        for (int i = 0; i <= n; i++)
        {
            double a = 2 * Math.PI * i / n;
            double rr = radius + (noise > 0 ? Math.Sin(a * 37) * noise : 0);
            pts[i] = new Point(cx + rr * Math.Cos(a), cy + rr * Math.Sin(a));
        }
        var p = new Polygon();
        p.AddRing(pts);
        return p;
    }

    public static double InsccribedCircleArea(int n, double radius)
        => 0.5 * n * radius * radius * Math.Sin(2 * Math.PI / n);

    public static Polyline ZigzagPolyline(int n, double dx = 0.01)
    {
        var pts = new Point[n];
        for (int i = 0; i < n; i++) pts[i] = new Point(i * dx, i % 2 == 0 ? 0 : 1);
        var pl = new Polyline();
        pl.AddPath(pts);
        return pl;
    }

    public static Polygon DonutWithHoles(int holes, double size = 1000, double holeSize = 2)
    {
        var p = new Polygon();
        p.AddRing(new[] { new Point(0, 0), new Point(size, 0), new Point(size, size), new Point(0, size), new Point(0, 0) });
        double perRow = Math.Ceiling(Math.Sqrt(holes));
        for (int h = 0; h < holes; h++)
        {
            double x = 10 + (h % perRow) * (size - 20) / perRow;
            double y = 10 + Math.Floor(h / perRow) * (size - 20) / perRow;
            p.AddRing(new[]
            {
                new Point(x, y), new Point(x + holeSize, y), new Point(x + holeSize, y + holeSize), new Point(x, y + holeSize), new Point(x, y),
            });
        }
        return p;
    }

    public static Polygon MultiPartSquares(int parts, double cell = 10)
    {
        var p = new Polygon();
        for (int i = 0; i < parts; i++)
        {
            double x = (i % 100) * cell * 2, y = (i / 100) * cell * 2;
            p.AddRing(new[] { new Point(x, y), new Point(x + cell, y), new Point(x + cell, y + cell), new Point(x, y + cell), new Point(x, y) });
        }
        return p;
    }

    /// <summary>螺旋自相交多边形。</summary>
    public static Polygon SelfIntersectingSpiral(int turns = 5, int perTurn = 40)
    {
        var pts = new List<Point>();
        for (int i = 0; i <= turns * perTurn; i++)
        {
            double a = 2 * Math.PI * i / perTurn;
            // 半径单调内收 + 交叉调制：确有自交，但两两不共线不重合（避免输入本身退化）
            double rr = (8 - 5.0 * i / (turns * perTurn)) * (1 + 0.45 * Math.Cos(a / 2));
            pts.Add(new Point(rr * Math.Cos(a), rr * Math.Sin(a)));
        }
        pts.Add(pts[0]);
        var p = new Polygon();
        p.AddRing(pts);
        return p;
    }

    /// <summary>随机凸多边形（独立面积/周长真值可算）。</summary>
    public static (Polygon Poly, List<(double x, double y)> Hull) RandomConvexPolygon(Random rng, double scale = 50)
    {
        int n = 6 + rng.Next(10);
        var pts = new List<(double, double)>();
        for (int i = 0; i < n; i++) pts.Add((rng.NextDouble() * 2 - 1, rng.NextDouble() * 2 - 1));
        var hull = MonotoneChainHull(pts);
        if (hull.Count < 3) return RandomConvexPolygon(rng, scale);
        var ring = hull.Select(p => new Point(p.x * scale, p.y * scale)).Append(new Point(hull[0].x * scale, hull[0].y * scale)).ToArray();
        var poly = new Polygon();
        poly.AddRing(ring);
        var scaled = hull.Select(p => (p.x * scale, p.y * scale)).ToList();
        return (poly, scaled);
    }

    /// <summary>独立单调链凸包参考实现（Andrew monotone chain）。</summary>
    public static List<(double x, double y)> MonotoneChainHull(List<(double x, double y)> pts)
    {
        var ps = pts.OrderBy(p => (p.x, p.y)).ToList();
        if (ps.Count <= 2) return ps;
        static double Cross((double x, double y) o, (double x, double y) a, (double x, double y) b)
            => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        var lower = new List<(double, double)>();
        foreach (var p in ps)
        {
            while (lower.Count >= 2 && Cross(lower[^2], lower[^1], p) <= 0) lower.RemoveAt(lower.Count - 1);
            lower.Add(p);
        }
        var upper = new List<(double, double)>();
        foreach (var p in Enumerable.Reverse(ps))
        {
            while (upper.Count >= 2 && Cross(upper[^2], upper[^1], p) <= 0) upper.RemoveAt(upper.Count - 1);
            upper.Add(p);
        }
        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        lower.AddRange(upper);
        return lower;
    }

    // ---- 独立暴力参考：面积/周长/包络（不经被测库）----
    public static double ShoelaceArea(List<(double x, double y)> ring)
    {
        double s = 0;
        int n = ring.Count;
        for (int i = 0; i < n; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % n];
            s += a.x * b.y - b.x * a.y;
        }
        return Math.Abs(s) / 2;
    }

    public static double PathLength(List<(double x, double y)> path)
    {
        double s = 0;
        for (int i = 0; i + 1 < path.Count; i++)
            s += Math.Sqrt((path[i + 1].x - path[i].x) * (path[i + 1].x - path[i].x) + (path[i + 1].y - path[i].y) * (path[i + 1].y - path[i].y));
        return s;
    }

    public static (double xmin, double ymin, double xmax, double ymax) Bounds(IEnumerable<(double x, double y)> pts)
    {
        double xn = double.MaxValue, yn = double.MaxValue, xx = double.MinValue, yx = double.MinValue;
        foreach (var (x, y) in pts) { xn = Math.Min(xn, x); yn = Math.Min(yn, y); xx = Math.Max(xx, x); yx = Math.Max(yx, y); }
        return (xn, yn, xx, yx);
    }
}
