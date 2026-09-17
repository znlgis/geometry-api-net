using System;
using System.Collections.Generic;
using System.Linq;
namespace OpenGIS.Esri.Geometry.Core.Internal
{
using OpenGIS.Esri.Geometry.Core.Geometries;
using Geometry = OpenGIS.Esri.Geometry.Core.Geometries.Geometry;

/// <summary>
/// OGC 简单性检测与 SimplifyOGC（makeValid 语义）实现：
///   IsSimple：线段集合在均匀网格加速下做两两相交判定（排除合法的相邻公共端点/部件共享端点）。
///   Execute：多边形 → 环集交点分裂 + 非零绕数边界重选 + 重组（复用 PolygonClipper.MakeValid）；
///            折线 → 自交点处断开重排；多点 → 去重。
/// </summary>
internal static class MakeValidOps
{
    /// <summary>几何 → 折线部件（多边形为闭环串、线为开放串）。</summary>
    public static List<List<double[]>> Parts(Geometry g)
    {
        var parts = new List<List<double[]>>();
        switch (g)
        {
            case Polygon pg:
                parts.AddRange(pg.GetRings().Select(ring => ring.Select(p => new[] { p.X, p.Y }).ToList()));
                break;
            case Envelope e:
                parts.Add(new List<double[]>
                {
                    new[] { e.XMin, e.YMin }, new[] { e.XMax, e.YMin }, new[] { e.XMax, e.YMax }, new[] { e.XMin, e.YMax }, new[] { e.XMin, e.YMin },
                });
                break;
            case Polyline pl:
                parts.AddRange(pl.GetPaths().Select(path => path.Select(p => new[] { p.X, p.Y }).ToList()));
                break;
            case Line l:
                parts.Add(new List<double[]> { new[] { l.Start.X, l.Start.Y }, new[] { l.End.X, l.End.Y } });
                break;
        }

        return parts.Where(p => p.Count >= 2).ToList();
    }

    public static bool IsRingClose(List<double[]> p) =>
        p.Count >= 3 && p[0][0] == p[p.Count - 1][0] && p[0][1] == p[p.Count - 1][1];

    public static bool IsAreal(Geometry g) => g is Polygon or Envelope;

    /// <summary>是否 OGC-simple。</summary>
    public static bool IsSimple(Geometry g)
    {
        if (g is MultiPoint mp)
        {
            var seen = new HashSet<(long, long)>();
            foreach (var p in mp.GetPoints())
                if (!seen.Add(((long)Math.Round(p.X * 1e9), (long)Math.Round(p.Y * 1e9)))) return false;
            return true;
        }

        if (g is Point) return true;

        bool area = IsAreal(g);
        var parts = Parts(g);
        var edges = new List<(int part, double ax, double ay, double bx, double by, int seq)>();
        for (int pi = 0; pi < parts.Count; pi++)
        {
            var p = parts[pi];
            int segCount = p.Count - 1;
            for (int i = 0; i < segCount; i++)
            {
                var a = p[i]; var b = p[i + 1];
                if (a[0] == b[0] && a[1] == b[1]) continue;
                edges.Add((pi, a[0], a[1], b[0], b[1], i));
            }
        }
        var isRingPart = parts.Select(IsRingClose).ToList();

        var grid = SegGrid.Build(edges.Select(e => (e.ax, e.ay, e.bx, e.by)).ToList());

        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            foreach (int j in grid.Query(e.ax, e.ay, e.bx, e.by))
            {
                if (j <= i) continue;
                var f = edges[j];

                // 同部件相邻边：允许共享端点相交；其它相交一律非简单
                bool samePartAdjacent = e.part == f.part &&
                    (Math.Abs(e.seq - f.seq) == 1 ||
                     (isRingPart[e.part] && Math.Abs(e.seq - f.seq) == SegmentCountOf(parts[e.part]) - 1));
                // 多边形相邻边=同环相连；不同部件（环）之间一切相交/相接皆非简单
                int kind = GeoMath.SegIntersect(e.ax, e.ay, e.bx, e.by, f.ax, f.ay, f.bx, f.by,
                    out double ta, out double tb, out double ta0, out double ta1, out double tc0, out double tc1);
                if (kind == 0) continue;
                if (kind == 2) return false;

                bool endpointsTouch = (ta <= 1e-9 || ta >= 1 - 1e-9) && (tb <= 1e-9 || tb >= 1 - 1e-9);
                if (area)
                {
                    // OGC 有效性语义：多边形各环仅允许"顶点级"接触（含多部件点触），禁止穿越/重叠。
                    if (endpointsTouch) continue;
                    // 顶点触边（交点为一边端点、另一边内部）亦按允许接触处理（GEOS 边界情形从宽）
                    if ((ta <= 1e-9 || ta >= 1 - 1e-9) || (tb <= 1e-9 || tb >= 1 - 1e-9)) continue;
                    return false;
                }
                else
                {
                    if (endpointsTouch && (samePartAdjacent || (e.part != f.part && SharesEndpoint(e, f))))
                        continue; // 折线部件共享端点视为简单（GEOS 语义）
                    if (endpointsTouch && samePartAdjacent) continue;
                    return false;
                }
            }
        }

        return true;
    }

    private static int SegmentCountOf(List<double[]> p) => IsRingClose(p) ? p.Count - 1 : p.Count - 1;

    private static bool SharesEndpoint((int part, double ax, double ay, double bx, double by, int seq) e,
        (int part, double ax, double ay, double bx, double by, int seq) f)
    {
        bool Eq(double x1, double y1, double x2, double y2) => Math.Abs(x1 - x2) <= 1e-12 && Math.Abs(y1 - y2) <= 1e-12;
        return Eq(e.ax, e.ay, f.ax, f.ay) || Eq(e.ax, e.ay, f.bx, f.by) ||
               Eq(e.bx, e.by, f.ax, f.ay) || Eq(e.bx, e.by, f.bx, f.by);
    }

    /// <summary>SimplifyOGC / makeValid。</summary>
    public static Geometry Execute(Geometry g, SpatialReference.SpatialReference? spatialRef)
    {
        if (g.IsEmpty || g is Point) return g.Copy();
        if (g is MultiPoint mpt)
        {
            var res = new MultiPoint();
            var seen = new HashSet<(long, long)>();
            foreach (var p in mpt.GetPoints())
                if (seen.Add(((long)Math.Round(p.X * 1e9), (long)Math.Round(p.Y * 1e9)))) res.Add(p);
            return res;
        }

        if (IsAreal(g))
        {
            var rings = PolygonClipper.OrientRings(Parts(g).Select(p => new PolygonClipper.Ring(p)).ToList());
            var fixedRings = PolygonClipper.MakeValid(rings);
            var poly = new Polygon();
            foreach (var r in fixedRings)
            {
                var pts = r.Select(p => new Point(p[0], p[1])).ToList();
                pts.Add(pts[0]);
                poly.AddRing(pts);
            }

            return poly;
        }

        // 折线：自交点断开重组
        var outPaths = SplitPolylines(Parts(g));
        var plOut = new Polyline();
        foreach (var p in outPaths)
            if (p.Count >= 2) plOut.AddPath(p.Select(c => new Point(c[0], c[1])).ToList());
        return plOut;
    }

    private static List<List<double[]>> SplitPolylines(List<List<double[]>> parts)
    {
        var splits = new Dictionary<long, List<double>>();
        var segs = new List<(int part, int seq, double ax, double ay, double bx, double by)>();
        for (int pi = 0; pi < parts.Count; pi++)
        {
            var p = parts[pi];
            for (int i = 0; i + 1 < p.Count; i++)
            {
                var a = p[i]; var b = p[i + 1];
                if (a[0] == b[0] && a[1] == b[1]) continue;
                segs.Add((pi, i, a[0], a[1], b[0], b[1]));
            }
        }

        var grid = SegGrid.Build(segs.Select(s => (s.ax, s.ay, s.bx, s.by)).ToList());
        for (int i = 0; i < segs.Count; i++)
        {
            var e = segs[i];
            foreach (int j in grid.Query(e.ax, e.ay, e.bx, e.by))
            {
                if (j <= i) continue;
                var f = segs[j];
                bool adjacentSamePath = e.part == f.part && Math.Abs(e.seq - f.seq) == 1;
                int kind = GeoMath.SegIntersect(e.ax, e.ay, e.bx, e.by, f.ax, f.ay, f.bx, f.by,
                    out double ta, out double tb, out double ta0, out double ta1, out double tc0, out double tc1);
                if (kind == 1)
                {
                    bool endpointTouch = (ta <= 1e-9 || ta >= 1 - 1e-9) && (tb <= 1e-9 || tb >= 1 - 1e-9);
                    if (endpointTouch && adjacentSamePath) continue;
                    if (endpointTouch && e.part != f.part && SharesEndpoint((e.part, e.ax, e.ay, e.bx, e.by, e.seq), (f.part, f.ax, f.ay, f.bx, f.by, f.seq)))
                        continue;
                    AddT(e.part, e.seq, ta);
                    AddT(f.part, f.seq, tb);
                }
                else if (kind == 2)
                {
                    AddT(e.part, e.seq, ta0); AddT(e.part, e.seq, ta1);
                    AddT(f.part, f.seq, tc0); AddT(f.part, f.seq, tc1);
                }
            }
        }

        void AddT(int path, int seg, double t)
        {
            if (t < 0) t = 0; if (t > 1) t = 1;
            if (t <= 1e-12 || t >= 1 - 1e-12) return;
            var key = (long)path * 1_000_000 + seg;
            if (!splits.TryGetValue(key, out var l)) splits[key] = l = new List<double>();
            l.Add(t);
        }

        var outPaths = new List<List<double[]>>();
        for (int pi = 0; pi < parts.Count; pi++)
        {
            var p = parts[pi];
            var piece = new List<double[]> { p[0] };
            for (int i = 0; i + 1 < p.Count; i++)
            {
                var ts = new List<double> { 1 };
                if (splits.TryGetValue((long)pi * 1_000_000 + i, out var extra)) ts.AddRange(extra);
                ts.Sort();
                foreach (var t in ts)
                {
                    var pt = At(p[i], p[i + 1], t);
                    if (piece[piece.Count - 1][0] == pt[0] && piece[piece.Count - 1][1] == pt[1]) continue;
                    piece.Add(pt);
                    if (t < 1)
                    {
                        outPaths.Add(piece);
                        piece = new List<double[]> { pt };
                    }
                }
            }

            if (piece.Count >= 2) outPaths.Add(piece);
        }

        return outPaths.Where(x => x.Count >= 2).ToList();
    }

    private static double[] At(double[] a, double[] b, double t) =>
        new[] { a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t };
}

/// <summary>线段均匀网格（查询矩形 → 候选边索引）。</summary>
internal sealed class SegGrid
{
    private readonly Dictionary<(int, int), List<int>> _cells = new();
    private readonly double _minX, _minY, _cell;
    private readonly int _cols, _rows;

    private SegGrid(List<(double ax, double ay, double bx, double by)> segs, double minX, double minY, double cell, int cols, int rows)
    {
        (_minX, _minY, _cell, _cols, _rows) = (minX, minY, cell, cols, rows);
        for (int i = 0; i < segs.Count; i++)
        {
            var (ax, ay, bx, by) = segs[i];
            int x0 = CX(Math.Min(ax, bx)), x1 = CX(Math.Max(ax, bx)), y0 = CY(Math.Min(ay, by)), y1 = CY(Math.Max(ay, by));
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    if (!_cells.TryGetValue((x, y), out var l)) _cells[(x, y)] = l = new List<int>();
                    if (!l.Contains(i)) l.Add(i);
                }
        }
    }

    public static SegGrid Build(List<(double ax, double ay, double bx, double by)> segs)
    {
        if (segs.Count == 0) return new SegGrid(segs, 0, 0, 1, 1, 1);
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var (ax, ay, bx, by) in segs)
        {
            if (ax < minX) minX = ax; if (bx < minX) minX = bx;
            if (ay < minY) minY = ay; if (by < minY) minY = by;
            if (ax > maxX) maxX = ax; if (bx > maxX) maxX = bx;
            if (ay > maxY) maxY = ay; if (by > maxY) maxY = by;
        }

        double w = Math.Max(maxX - minX, 1e-9), h = Math.Max(maxY - minY, 1e-9);
        double cell = Math.Sqrt(w * h / Math.Max(segs.Count, 1));
        int cols = Math.Min(4096, Math.Max(1, (int)(w / cell) + 1));
        int rows = Math.Min(4096, Math.Max(1, (int)(h / cell) + 1));
        cell = Math.Max(w / cols, h / rows);
        var g = new SegGrid(segs, minX, minY, cell, cols, rows);
        return g;
    }

    private int CX(double x)
    {
        int v = (int)Math.Floor((x - _minX) / _cell);
        return v < 0 ? 0 : v > _cols - 1 ? _cols - 1 : v;
    }

    private int CY(double y)
    {
        int v = (int)Math.Floor((y - _minY) / _cell);
        return v < 0 ? 0 : v > _rows - 1 ? _rows - 1 : v;
    }

    public IEnumerable<int> Query(double minX, double minY, double maxX, double maxY)
    {
        int x0 = CX(minX), x1 = CX(maxX), y0 = CY(minY), y1 = CY(maxY);
        var seen = new HashSet<int>();
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                if (_cells.TryGetValue((x, y), out var l))
                    foreach (var i in l)
                        if (seen.Add(i)) yield return i;
    }
}
}
