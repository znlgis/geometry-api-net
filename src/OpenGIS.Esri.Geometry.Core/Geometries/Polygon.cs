using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenGIS.Esri.Geometry.Core.Geometries;

/// <summary>
///     表示由一个或多个环组成的多边形几何对象。
/// </summary>
public class Polygon : Geometry
{
    private readonly List<List<Point>> _rings;

    /// <summary>
    ///     初始化 <see cref="Polygon" /> 类的新实例。
    /// </summary>
    public Polygon()
    {
        _rings = new List<List<Point>>();
    }

    /// <inheritdoc />
    public override GeometryType Type => GeometryType.Polygon;

    /// <inheritdoc />
    public override bool IsEmpty
    {
        get
        {
            if (_rings.Count == 0)
                return true;

            foreach (var ring in _rings)
                if (ring.Count > 0)
                    return false;
            return true;
        }
    }

    /// <inheritdoc />
    public override int Dimension => 2;

    /// <summary>
    ///     获取多边形中的环数量。
    /// </summary>
    public int RingCount => _rings.Count;

    /// <summary>
    ///     使用鞋带公式计算多边形面积：按嵌套深度定符号（壳为正、洞为负，与环方向无关）。
    /// </summary>
    public double Area
    {
        get
        {
            var rings = _rings.Where(r => CountDistinct(r) >= 3).ToList();
            if (rings.Count == 0) return 0;
            var reps = rings.Select(RingRepresentative).ToList();
            var absArea = rings.Select(r => Math.Abs(RingSignedArea(r)) * 0.5).ToList();
            // 标准嵌套树：按 |面积| 降序处理，环的深度 = 首个（最小面积方向）包含其代表点的已处理父环深度 + 1。
            var order = Enumerable.Range(0, rings.Count).OrderByDescending(i => absArea[i]).ToList();
            var depth = new int[rings.Count];
            foreach (var i in order)
            {
                var rep = reps[i];
                if (rep == null || double.IsNaN(rep[0])) continue;
                int parent = -1;
                foreach (var j in order)
                {
                    if (j == i || absArea[j] <= absArea[i]) break; // 只看更大面积（先处理）的候选
                    if (PointInRing(rep[0], rep[1], rings[j]))
                    {
                        if (parent < 0 || absArea[j] < absArea[parent]) parent = j;
                    }
                }

                depth[i] = parent < 0 ? 0 : depth[parent] + 1;
            }

            var total = 0.0;
            for (var i = 0; i < rings.Count; i++)
                total += depth[i] % 2 == 0 ? absArea[i] : -absArea[i];
            return total;
        }
    }

    private static int CountDistinct(List<Point> ring)
    {
        if (ring.Count >= 2 && ring[0].X == ring[ring.Count - 1].X && ring[0].Y == ring[ring.Count - 1].Y) return ring.Count - 1;
        return ring.Count;
    }

    private static double RingSignedArea(List<Point> ring)
    {
        double s = 0;
        var count = ring.Count;
        for (var i = 0; i < count - 1; i++) s += ring[i].X * ring[i + 1].Y - ring[i + 1].X * ring[i].Y;
        s += ring[count - 1].X * ring[0].Y - ring[0].X * ring[count - 1].Y;
        return s;
    }

    /// <summary>环内部代表点：相邻两顶点弦中点试探，必要时包围盒网格兜底。</summary>
    private static double[]? RingRepresentative(List<Point> ring)
    {
        var n = ring.Count;
        for (var i = 0; i < n; i++)
        {
            var a = ring[(i - 1 + n) % n]; var c = ring[(i + 1) % n];
            double mx = (a.X + c.X) / 2, my = (a.Y + c.Y) / 2;
            if (PointInRing(mx, my, ring)) return new[] { mx, my };
        }
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var p in ring)
        {
            if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y;
        }
        for (var gx = 0; gx < 16; gx++)
        for (var gy = 0; gy < 16; gy++)
        {
            double x = minX + (gx + 0.5) * (maxX - minX) / 16;
            double y = minY + (gy + 0.5) * (maxY - minY) / 16;
            if (PointInRing(x, y, ring)) return new[] { x, y };
        }
        return null;
    }

    private static bool PointInRing(double x, double y, List<Point> ring)
    {
        var inside = false;
        var count = ring.Count;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            double xi = ring[i].X, yi = ring[i].Y, xj = ring[j].X, yj = ring[j].Y;
            if (yi > y != yj > y && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
        }
        return inside;
    }

    /// <summary>
    ///     向多边形添加新的环。
    /// </summary>
    /// <param name="points">构成环的点集合。</param>
    public void AddRing(IEnumerable<Point> points)
    {
        if (points == null) throw new ArgumentNullException(nameof(points));
        _rings.Add(points.ToList());
    }

    /// <summary>
    ///     获取指定索引处的环。
    /// </summary>
    /// <param name="index">环的索引。</param>
    /// <returns>指定索引处的环。</returns>
    public IReadOnlyList<Point> GetRing(int index)
    {
        if (index < 0 || index >= _rings.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return _rings[index].AsReadOnly();
    }

    /// <summary>
    ///     获取多边形中的所有环。
    /// </summary>
    /// <returns>环的可枚举集合。</returns>
    public IEnumerable<IReadOnlyList<Point>> GetRings()
    {
        return _rings.Select(r => r.AsReadOnly());
    }

    /// <inheritdoc />
    public override Envelope GetEnvelope()
    {
        if (IsEmpty) return new Envelope();

        var envelope = new Envelope();
        foreach (var ring in _rings)
        foreach (var point in ring)
            envelope.Merge(point);

        return envelope;
    }
}