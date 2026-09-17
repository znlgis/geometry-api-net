using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenGIS.Esri.Geometry.Core.Internal
{
using OpenGIS.Esri.Geometry.Core.Geometries;
using Geometry = OpenGIS.Esri.Geometry.Core.Geometries.Geometry;



/// <summary>
/// 集合运算核心：面×面走 PolygonClipper 真布尔运算；线×线、线×面通过段分裂实现；
/// 点类走集合语义。混合维度不支持时抛 NotSupportedException（明确语义，不再返回包络假结果）。
/// </summary>
internal static class SetOpsCore
{
    private static bool IsAreal(Geometry g) => g is Polygon or Envelope;
    private static bool IsLinear(Geometry g) => g is Polyline or Line;
    private static bool IsPointish(Geometry g) => g is Point or MultiPoint;

    public static Geometry Union(Geometry a, Geometry b)
    {
        if (a.IsEmpty) return b.Copy();
        if (b.IsEmpty) return a.Copy();

        if (IsAreal(a) && IsAreal(b))
            return ToPolygon(PolygonClipper.Boolean(RingsOf(a), RingsOf(b), PolygonClipper.ClipOp.Union));
        if (IsPointish(a) && IsPointish(b))
        {
            var merged = new List<Point>();
            var seen = new HashSet<(long, long)>();
            foreach (var p in PointsOf(a).Concat(PointsOf(b)))
                if (seen.Add(((long)Math.Round(p.X * 1e9), (long)Math.Round(p.Y * 1e9)))) merged.Add(p);
            return ComposePoints(merged);
        }
        if (IsLinear(a) && IsLinear(b))
        {
            // 对齐 GEOS：在一切交点处拆段，A/B 重叠区间去重（保留 B 侧）
            var pl = new Polyline();
            foreach (var path in SplitLineUnion(ToCoordPaths(PathsOf(a)), ToCoordPaths(PathsOf(b))))
                pl.AddPath(path.Select(c => new Point(c[0], c[1])).ToList());
            return pl;
        }
        throw new NotSupportedException($"Union 不支持的几何组合：{a.Type} + {b.Type}");
    }

    public static Geometry Intersection(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return EmptyLike(a, b);

        if (IsAreal(a) && IsAreal(b))
            return ToPolygon(PolygonClipper.Boolean(RingsOf(a), RingsOf(b), PolygonClipper.ClipOp.Intersection));
        if (IsAreal(a) && IsLinear(b)) return IntersectionAreaLine(a, b);
        if (IsLinear(a) && IsAreal(b)) return IntersectionAreaLine(b, a);
        if (IsLinear(a) && IsLinear(b)) return IntersectionLineLine(a, b);
        if (IsPointish(a) && IsPointish(b))
        {
            var common = new List<Point>();
            var seenCommon = new HashSet<(long, long)>();
            foreach (var p in PointsOf(a))
                foreach (var q in PointsOf(b))
                    if (p.X == q.X && p.Y == q.Y && seenCommon.Add(((long)Math.Round(p.X * 1e9), (long)Math.Round(p.Y * 1e9))))
                    { common.Add(p); break; }
            return ComposePoints(common);
        }
        if (IsAreal(a) && IsPointish(b) || IsPointish(a) && IsAreal(b))
        {
            var area = IsAreal(a) ? a : b;
            var pts = IsAreal(a) ? PointsOf(b) : PointsOf(a);
            var keep = pts.Where(p => ContainsPoint(area, p)).ToList();
            return ComposePoints(keep);
        }
        if ((IsAreal(a) || IsLinear(a)) && IsPointish(b) || IsPointish(a) && (IsAreal(b) || IsLinear(b)))
        {
            var host = IsPointish(a) ? b : a;
            var pts = IsPointish(a) ? PointsOf(a) : PointsOf(b);
            var shape = ShapeModel.Of(host);
            var keep = pts.Where(p => shape.Classify(p.X, p.Y) != 0).ToList();
            return ComposePoints(keep);
        }
        throw new NotSupportedException($"Intersection 不支持的几何组合：{a.Type} ∩ {b.Type}");
    }

    public static Geometry Difference(Geometry a, Geometry b)
    {
        if (a.IsEmpty) return a.Copy();
        if (b.IsEmpty) return a.Copy();

        if (IsAreal(a) && IsAreal(b))
            return ToPolygon(PolygonClipper.Boolean(RingsOf(a), RingsOf(b), PolygonClipper.ClipOp.Difference));
        if (IsLinear(a) && (IsAreal(b) || IsLinear(b)))
            return TrimLines(a, ShapeModel.Of(b), keepInside: false, b);
        if (IsPointish(a) && (IsAreal(b) || IsLinear(b) || IsPointish(b)))
        {
            var shape = ShapeModel.Of(b);
            var keep = PointsOf(a).Where(p =>
            {
                if (IsPointish(b)) return !PointsOf(b).Any(q => q.X == p.X && q.Y == p.Y);
                return shape.Classify(p.X, p.Y) == 0;
            }).ToList();
            return ComposePoints(keep);
        }
        throw new NotSupportedException($"Difference 不支持的几何组合：{a.Type} − {b.Type}");
    }

    public static Geometry SymmetricDifference(Geometry a, Geometry b)
    {
        if (a.IsEmpty) return b.Copy();
        if (b.IsEmpty) return a.Copy();

        if (IsAreal(a) && IsAreal(b))
            return ToPolygon(PolygonClipper.Boolean(RingsOf(a), RingsOf(b), PolygonClipper.ClipOp.SymmetricDifference));
        if (IsPointish(a) && IsPointish(b))
        {
            var pa = PointsOf(a); var pb = PointsOf(b);
            var res = new MultiPoint();
            foreach (var p in pa) if (!pb.Any(q => q.X == p.X && q.Y == p.Y)) res.Add(p);
            foreach (var p in pb) if (!pa.Any(q => q.X == p.X && q.Y == p.Y)) res.Add(p);
            return res;
        }
        if (IsLinear(a) && (IsLinear(b) || IsAreal(b)) || IsLinear(b) && IsAreal(a))
        {
            var r1 = TrimLines(a, ShapeModel.Of(b), keepInside: false, b);
            var r2 = TrimLines(b, ShapeModel.Of(a), keepInside: false, a);
            if (IsLinear(r1) && IsLinear(r2)) return Union(r1, r2);
            if (r1.IsEmpty) return r2;
            if (r2.IsEmpty) return r1;
            throw new NotSupportedException("SymmetricDifference 混合维度暂不支持");
        }
        throw new NotSupportedException($"SymmetricDifference 不支持的几何组合：{a.Type} △ {b.Type}");
    }

    // ---- 面×线 / 线×线 ----

    private static Geometry IntersectionAreaLine(Geometry area, Geometry line)
    {
        return TrimLines(line, ShapeModel.Of(area), keepInside: true, area);
    }

    /// <summary>把线几何按与 shape 的关系裁剪：keepInside=true 保留在内部的段，false 保留外部段。</summary>
    private static Geometry TrimLines(Geometry line, ShapeModel shape, bool keepInside, Geometry otherGeom)
    {
        var pathsOut = new List<List<Point>>();
        var pointsOut = new List<Point>();
        foreach (var path in PathsOf(line))
        {
            for (int i = 0; i + 1 < path.Count; i++)
            {
                var a = path[i]; var b = path[i + 1];
                SplitSegmentAgainstShape(a, b, shape, pathsOut, pointsOut, keepInside);
            }
            // 孤立顶点（单点路径）
            if (path.Count == 1)
            {
                bool inside = shape.Classify(path[0].X, path[0].Y) == 2;
                if (inside == keepInside) pointsOut.Add(path[0]);
            }
        }
        return Compose(pathsOut, pointsOut);
    }

    private static void SplitSegmentAgainstShape(Point a, Point b, ShapeModel shape,
        List<List<Point>> pathsOut, List<Point> pointsOut, bool keepInside)
    {
        var ts = new List<double> { 0, 1 };
        foreach (var (ax, ay, bx, by) in shape.Segs)
        {
            int kind = GeoMath.SegIntersect(a.X, a.Y, b.X, b.Y, ax, ay, bx, by,
                out double ta, out double tb, out double ta0, out double ta1, out _, out _);
            if (kind == 1) ts.Add(ta);
            else if (kind == 2) { ts.Add(ta0); ts.Add(ta1); }
        }
        ts.Sort();
        var uniq = new List<double>();
        foreach (var t in ts) if (uniq.Count == 0 || t - uniq[uniq.Count - 1] > 1e-12) uniq.Add(t);

        for (int k = 0; k + 1 < uniq.Count; k++)
        {
            double t0 = uniq[k], t1 = uniq[k + 1];
            if (t1 - t0 < 1e-12) continue;
            double tm = (t0 + t1) / 2;
            int loc = shape.Classify(a.X + (b.X - a.X) * tm, a.Y + (b.Y - a.Y) * tm);
            bool inside = loc == 2;
            if (inside == keepInside)
                pathsOut.Add(new List<Point>
                {
                    At(a, b, t0), At(a, b, t1),
                });
        }
        // 交点本身（在边界上）不计入结果内部
        if (uniq.Count > 2)
        {
            foreach (var t in uniq.Skip(1).Take(uniq.Count - 2))
            {
                int loc = shape.Classify(At(a, b, t).X, At(a, b, t).Y);
                if (loc == 1) { /* 边界交点：OGC 交集含边界点，但线∩面常用内部段表示；不输出切点 */ }
            }
        }
    }

    private static Point At(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    private static Geometry IntersectionLineLine(Geometry a, Geometry b)
    {
        var ma = ShapeModel.Of(a);
        var mb = ShapeModel.Of(b);
        var pts = new List<Point>();
        var segs = new List<List<Point>>();
        foreach (var (ax, ay, bx, by) in ma.Segs)
            foreach (var (cx, cy, dx, dy) in mb.Segs)
            {
                int kind = GeoMath.SegIntersect(ax, ay, bx, by, cx, cy, dx, dy,
                    out double ta, out double tb, out double ta0, out double ta1, out double tc0, out double tc1);
                if (kind == 1)
                {
                    if (ta > 1e-9 && ta < 1 - 1e-9) pts.Add(new Point(ax + (bx - ax) * ta, ay + (by - ay) * ta));
                }
                else if (kind == 2 && ta1 - ta0 > 1e-12)
                    segs.Add(new List<Point>
                    {
                        new(ax + (bx - ax) * ta0, ay + (by - ay) * ta0),
                        new(ax + (bx - ax) * ta1, ay + (by - ay) * ta1),
                    });
            }
        if (segs.Count > 0)
        {
            var pl = new Polyline();
            foreach (var s in segs) pl.AddPath(s);
            return pl;
        }
        var seenKeys = new System.Collections.Generic.HashSet<(long, long)>();
        pts = pts.Where(p => seenKeys.Add(((long)Math.Round(p.X * 1e9), (long)Math.Round(p.Y * 1e9)))).ToList();
        return pts.Count switch { 0 => new Point(), 1 => (Geometry)pts[0], _ => new MultiPoint(pts) };
    }

    private static Geometry ComposePoints(List<Point> points) => points.Count switch
    {
        0 => new Point(),
        1 => points[0],
        _ => new MultiPoint(points),
    };

    private static Geometry Compose(List<List<Point>> paths, List<Point> points)
    {
        if (paths.Count > 0)
        {
            var pl = new Polyline();
            foreach (var p in paths) pl.AddPath(p);
            return pl;
        }
        if (points.Count > 0) return points.Count == 1 ? points[0] : new MultiPoint(points);
        return new Polyline();
    }

    // ---- 辅助 ----

    private static List<PolygonClipper.Ring> RingsOf(Geometry g)
    {
        var rings = new List<PolygonClipper.Ring>();
        if (g is Polygon pg)
            foreach (var ring in pg.GetRings())
            {
                var r = new PolygonClipper.Ring(ring.Select(p => new[] { p.X, p.Y }));
                if (r.Count >= 3 && r[0][0] == r[r.Count - 1][0] && r[0][1] == r[r.Count - 1][1]) r.RemoveAt(r.Count - 1);
                if (r.Count >= 3) rings.Add(r);
            }
        else if (g is Envelope e)
            rings.Add(new PolygonClipper.Ring(new[]
            {
                new[] { e.XMin, e.YMin }, new[] { e.XMax, e.YMin }, new[] { e.XMax, e.YMax }, new[] { e.XMin, e.YMax },
            }));
        return rings;
    }

    private static Polygon ToPolygon(List<PolygonClipper.Ring> rings)
    {
        var poly = new Polygon();
        foreach (var ring in rings)
        {
            var pts = ring.Select(p => new Point(p[0], p[1])).ToList();
            pts.Add(pts[0]);
            poly.AddRing(pts);
        }
        return poly;
    }

    private static IEnumerable<Point> PointsOf(Geometry g) => g switch
    {
        Point p => new[] { p },
        MultiPoint mp => mp.GetPoints().ToList(),
        _ => Array.Empty<Point>(),
    };

    private static List<List<Point>> PathsOf(Geometry g) => g switch
    {
        Polyline pl => pl.GetPaths().Select(p => p.ToList()).ToList(),
        Line l => new List<List<Point>> { new() { l.Start, l.End } },
        _ => new(),
    };


    private static List<List<double[]>> ToCoordPaths(List<List<Point>> paths) =>
        paths.Select(p => p.Select(pt => new[] { pt.X, pt.Y }).ToList()).ToList();

    /// <summary>
    /// 线并集拆段：A、B 全部段在交点处互相分裂；A 上与 B 共线重叠的区间丢弃（去重），
    /// 相邻共端点子段链接回路径。
    /// </summary>
    private static List<List<double[]>> SplitLineUnion(List<List<double[]>> aParts, List<List<double[]>> bParts)
    {
        var segs = new List<(int side, int part, int seq, double ax, double ay, double bx, double by)>();
        void AddAll(List<List<double[]>> parts, int side)
        {
            for (int pi = 0; pi < parts.Count; pi++)
                for (int si = 0; si + 1 < parts[pi].Count; si++)
                {
                    var a = parts[pi][si]; var b = parts[pi][si + 1];
                    if (a[0] == b[0] && a[1] == b[1]) continue;
                    segs.Add((side, pi, si, a[0], a[1], b[0], b[1]));
                }
        }
        AddAll(aParts, 0);
        AddAll(bParts, 1);
        if (segs.Count == 0) return new List<List<double[]>>();

        var grid = SegGrid.Build(segs.Select(x => (x.ax, x.ay, x.bx, x.by)).ToList());
        var splits = new Dictionary<int, List<double>>();
        var drops = new Dictionary<int, List<(double, double)>>(); // A 侧待丢弃参数区间
        void AddSplit(int i, double t)
        {
            if (t <= 1e-12) return;
            if (t >= 1 - 1e-12) return;
            if (!splits.TryGetValue(i, out var l)) splits[i] = l = new List<double>();
            l.Add(t);
        }
        void AddDrop(int i, double t0, double t1)
        {
            if (!drops.TryGetValue(i, out var l)) drops[i] = l = new List<(double, double)>();
            l.Add((Math.Min(t0, t1), Math.Max(t0, t1)));
        }

        for (int i = 0; i < segs.Count; i++)
        {
            var si = segs[i];
            foreach (int j in grid.Query(Math.Min(si.ax, si.bx), Math.Min(si.ay, si.by), Math.Max(si.ax, si.bx), Math.Max(si.ay, si.by)))
            {
                if (j <= i) continue;
                var sj = segs[j];
                bool adjacentSamePath = si.side == sj.side && si.part == sj.part && Math.Abs(si.seq - sj.seq) == 1;
                if (adjacentSamePath) continue;
                int kind = GeoMath.SegIntersect(si.ax, si.ay, si.bx, si.by, sj.ax, sj.ay, sj.bx, sj.by,
                    out double ta, out double tb, out double ta0, out double ta1, out double tc0, out double tc1);
                if (kind == 1)
                {
                    // 端点接触只在需要时分裂（保持路径整洁：仅在真穿越时分裂两侧）
                    bool touchEnd = (ta <= 1e-9 || ta >= 1 - 1e-9) && (tb <= 1e-9 || tb >= 1 - 1e-9);
                    if (touchEnd) continue;
                    AddSplit(i, ta);
                    AddSplit(j, tb);
                }
                else if (kind == 2)
                {
                    AddSplit(i, ta0); AddSplit(i, ta1);
                    AddSplit(j, tc0); AddSplit(j, tc1);
                    // 重叠区间：A 侧丢弃（保留 B 侧表示）
                    if (si.side == 0) AddDrop(i, ta0, ta1);
                    if (sj.side == 0) AddDrop(j, tc0, tc1);
                }
            }
        }

        // 生成子段并链接路径
        var result = new List<List<double[]>>();
        for (int side = 0; side < 2; side++)
        {
            var parts = side == 0 ? aParts : bParts;
            for (int pi = 0; pi < parts.Count; pi++)
            {
                var p = parts[pi];
                var piece = new List<double[]> { p[0] };
                for (int si = 0; si + 1 < p.Count; si++)
                {
                    var key = side * 1_000_000_000 + pi * 1_000_000 + si;
                    if (!segsExists(segs, side, pi, si))
                    {
                        // 零长度段：仅闭合点
                        if (side == 1) { }
                        continue;
                    }
                    var ts = new List<double> { 0, 1 };
                    if (splits.TryGetValue((int)key, out var sp)) ts.AddRange(sp);
                    ts.Sort();
                    drops.TryGetValue((int)key, out var dropList);
                    for (int k = 0; k < ts.Count - 1; k++)
                    {
                        if (ts[k + 1] - ts[k] < 1e-12) continue;
                        double tm = (ts[k] + ts[k + 1]) / 2;
                        if (dropList != null && dropList.Any(d => tm >= d.Item1 - 1e-12 && tm <= d.Item2 + 1e-12))
                            continue; // A 侧重叠丢弃
                        var q = At2(p[si], p[si + 1], ts[k]);
                        var r = At2(p[si], p[si + 1], ts[k + 1]);
                        if (piece.Count > 0 && (piece[piece.Count - 1][0] != q[0] || piece[piece.Count - 1][1] != q[1]))
                        {
                            result.Add(piece);
                            piece = new List<double[]> { q };
                        }
                        else if (piece.Count > 0)
                        {
                            // 延续当前 piece
                        }
                        else piece.Add(q);
                        piece.Add(r);
                    }
                }
                if (piece.Count >= 2) result.Add(piece);
                else if (piece.Count == 1 && parts[pi].Count == 1 && side == 1) result.Add(piece);
            }
        }
        return result.Where(x => x.Count >= 2).ToList();

        bool segsExists(List<(int side, int part, int seq, double ax, double ay, double bx, double by)> all, int s2, int p2, int q2)
            => all.Any(x => x.side == s2 && x.part == p2 && x.seq == q2);
    }

    private static double[] At2(double[] a, double[] b, double t) => new[] { a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t };

    private static Geometry EmptyLike(Geometry a, Geometry b)
    {
        if (IsAreal(a) && IsAreal(b)) return new Polygon();
        if (IsLinear(a) || IsLinear(b)) return IsAreal(a) || IsAreal(b) ? new Polygon() : (Geometry)new Polyline();
        return new Point();
    }

    private static bool ContainsPoint(Geometry area, Point p)
    {
        var m = ShapeModel.Of(area);
        return m.Classify(p.X, p.Y) != 0;
    }
}
}
