using System;
using System.Collections.Generic;

namespace OpenGIS.Esri.Geometry.Core.Internal
{
using OpenGIS.Esri.Geometry.Core.Geometries;
using Geometry = OpenGIS.Esri.Geometry.Core.Geometries.Geometry;
using static OpenGIS.Esri.Geometry.Core.Internal.Nums;



/// <summary>
/// 几何体的关系模型：把任意 Geometry 分解为「内部/边界/外部」三层素材，
/// 提供点分类（In/On/Out）与边段索引，是 9-intersection 谓词引擎的统一底座。
/// 线边界采用 OGC 奇数度规则（端点出现次数为奇数的顶点属于边界）。
/// </summary>
internal sealed class ShapeModel
{
    /// <summary>0=点(0维) 1=线(1维) 2=面(2维)。</summary>
    public readonly int Dim;
    /// <summary>点集的坐标（Dim==0）。</summary>
    public readonly List<double[]> Points = new();
    /// <summary>线组件：每条是坐标串（Dim==1；开放线或闭合环）。</summary>
    public readonly List<List<double[]>> Lines = new();
    /// <summary>面环（Dim==2），去重闭合点；奇偶规则定义内部。</summary>
    public readonly List<List<double[]>> Rings = new();

    public double MinX = double.MaxValue, MinY = double.MaxValue, MaxX = double.MinValue, MaxY = double.MinValue;

    /// <summary>所有 1 维段（环边 + 线段的统一视图），供求交/距离用。</summary>
    public readonly List<(double ax, double ay, double bx, double by)> Segs = new();

    private Dictionary<(double, double), int>? _endDegree;
    private EdgeIndex? _edgeIndex;

    public EdgeIndex Edges => _edgeIndex ??= new EdgeIndex(Segs);

    private ShapeModel(int dim) { Dim = dim; }

    public static ShapeModel Of(Geometry g)
    {
        if (g is Point p)
        {
            var m = new ShapeModel(0);
            if (!double.IsNaN(p.X)) { m.AddTo(m, p.X, p.Y); m.Points.Add(new[] { p.X, p.Y }); }
            return m;
        }
        if (g is MultiPoint mp)
        {
            var m = new ShapeModel(0);
            foreach (var pt in mp.GetPoints())
                if (!double.IsNaN(pt.X)) { m.AddTo(m, pt.X, pt.Y); m.Points.Add(new[] { pt.X, pt.Y }); }
            return m;
        }
        if (g is Line l)
        {
            var m = new ShapeModel(1);
            var coords = new List<double[]> { new[] { l.Start.X, l.Start.Y }, new[] { l.End.X, l.End.Y } };
            m.Lines.Add(coords);
            foreach (var c in coords) m.AddTo(m, c[0], c[1]);
            m.IndexSegments();
            return m;
        }
        if (g is Polyline pl)
        {
            var m = new ShapeModel(1);
            foreach (var path in pl.GetPaths())
            {
                if (path.Count < 2) continue;
                var coords = new List<double[]>();
                foreach (var pt in path) { coords.Add(new[] { pt.X, pt.Y }); m.AddTo(m, pt.X, pt.Y); }
                m.Lines.Add(coords);
            }
            m.IndexSegments();
            return m;
        }
        if (g is Polygon pg)
        {
            var m = new ShapeModel(2);
            foreach (var ring in pg.GetRings())
            {
                var coords = new List<double[]>();
                foreach (var pt in ring) { coords.Add(new[] { pt.X, pt.Y }); m.AddTo(m, pt.X, pt.Y); }
                if (coords.Count >= 3 && SameCoord(coords[0], coords[coords.Count - 1])) coords.RemoveAt(coords.Count - 1);
                if (coords.Count >= 3) m.Rings.Add(coords);
            }
            m.IndexSegments();
            return m;
        }
        if (g is Envelope e)
        {
            var m = new ShapeModel(2);
            var coords = new List<double[]>
            {
                new[]{ e.XMin, e.YMin }, new[]{ e.XMax, e.YMin }, new[]{ e.XMax, e.YMax }, new[]{ e.XMin, e.YMax },
            };
            foreach (var c in coords) m.AddTo(m, c[0], c[1]);
            m.Rings.Add(coords);
            m.IndexSegments();
            return m;
        }
        throw new NotSupportedException($"无法分解几何类型 {g?.GetType().Name}");
    }

    /// <summary>模型无任何素材（空几何）。</summary>
    public bool IsVoid => Points.Count == 0 && Lines.Count == 0 && Rings.Count == 0;

    private void AddTo(ShapeModel m, double x, double y)
    {
        if (x < m.MinX) m.MinX = x;
        if (y < m.MinY) m.MinY = y;
        if (x > m.MaxX) m.MaxX = x;
        if (y > m.MaxY) m.MaxY = y;
    }

    private static bool SameCoord(double[] a, double[] b) => a[0] == b[0] && a[1] == b[1];

    private void IndexSegments()
    {
        foreach (var ring in Rings)
            for (int i = 0, n = ring.Count; i < n; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % n];
                if (a[0] == b[0] && a[1] == b[1]) continue;
                Segs.Add((a[0], a[1], b[0], b[1]));
            }
        foreach (var line in Lines)
            for (int i = 0; i + 1 < line.Count; i++)
            {
                var a = line[i]; var b = line[i + 1];
                if (a[0] == b[0] && a[1] == b[1]) continue;
                Segs.Add((a[0], a[1], b[0], b[1]));
            }
    }

    /// <summary>段端点出现次数（量化到容差网格），用于奇数度边界判定。</summary>
    private Dictionary<(double, double), int> EndDegree => _endDegree ??= BuildEndDegree();

    private Dictionary<(double, double), int> BuildEndDegree()
    {
        var d = new Dictionary<(double, double), int>();
        foreach (var (ax, ay, bx, by) in Segs)
            foreach (var (x, y) in new[] { (ax, ay), (bx, by) })
            {
                var key = Quantize(x, y);
                d[key] = d.TryGetValue(key, out int v) ? v + 1 : 1;
            }
        return d;
    }

    private (double, double) Quantize(double x, double y)
    {
        double s = 1.0 / (GeoMath.Eps * 10);
        return (Math.Round(x * s) / s, Math.Round(y * s) / s);
    }

    /// <summary>某顶点在线模型中是否为边界（奇数度）。</summary>
    public bool IsLineBoundaryVertex(double x, double y)
    {
        var key = Quantize(x, y);
        return EndDegree.TryGetValue(key, out int deg) && deg % 2 == 1;
    }

    private (double,double,double,double)[]? _ringBoxes;

    /// <summary>点分类：0=Out 1=On(边界) 2=In(内部)。点几何：命中即内部。</summary>
    public int Classify(double x, double y)
    {
        switch (Dim)
        {
            case 0:
                foreach (var p in Points)
                    if (Near(p[0], x) && Near(p[1], y)) return 2;
                return 0;
            case 1:
            {
                int deg = 0;
                bool on = false;
                foreach (var (ax, ay, bx, by) in Segs)
                {
                    if (!GeoMath.PointOnSegment(x, y, ax, ay, bx, by)) continue;
                    on = true;
                    if (Near(ax, x) && Near(ay, y)) deg++;
                    if (Near(bx, x) && Near(by, y)) deg++;
                }
                if (!on) return 0;
                if (deg == 0) return 2;
                return deg % 2 == 1 ? 1 : 2;
            }
            default:
            {
                if (Rings.Count == 0) return 0;
                _ringBoxes ??= ComputeRingBoxes();
                int parity = 0;
                for (int ri = 0; ri < Rings.Count; ri++)
                {
                    var (bx0, by0, bx1, by1) = _ringBoxes[ri];
                    if (x < bx0 || x > bx1 || y < by0 || y > by1) continue;
                    int r = GeoMath.ClassifyPointInRing(x, y, Rings[ri]);
                    if (r == 1) return 1;
                    if (r == 2) parity ^= 1;
                }
                return parity == 1 ? 2 : 0;
            }
        }
    }

    private (double, double, double, double)[] ComputeRingBoxes()
    {
        var arr = new (double, double, double, double)[Rings.Count];
        for (int i = 0; i < Rings.Count; i++)
        {
            double xn = double.MaxValue, yn = double.MaxValue, xx = double.MinValue, yx = double.MinValue;
            foreach (var p in Rings[i])
            {
                if (p[0] < xn) xn = p[0]; if (p[0] > xx) xx = p[0];
                if (p[1] < yn) yn = p[1]; if (p[1] > yx) yx = p[1];
            }
            arr[i] = (xn, yn, xx, yx);
        }
        return arr;
    }

    private static bool Near(double a, double b) =>
        Math.Abs(a - b) <= GeoMath.Eps * Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
}

/// <summary>线段均匀网格索引：加速两几何求交与最近段查询。</summary>
internal sealed class EdgeIndex
{
    private readonly List<(double ax, double ay, double bx, double by)> _segs;
    private readonly Dictionary<(int, int), List<int>> _cells = new();
    private readonly double _cell;
    private readonly double _ox, _oy;
    private readonly int _cols, _rows;

    public EdgeIndex(List<(double ax, double ay, double bx, double by)> segs)
    {
        _segs = segs;
        if (segs.Count == 0) { _cell = 1; return; }
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var (ax, ay, bx, by) in segs)
        {
            if (ax < minX) minX = ax; if (bx < minX) minX = bx;
            if (ay < minY) minY = ay; if (by < minY) minY = by;
            if (ax > maxX) maxX = ax; if (bx > maxX) maxX = bx;
            if (ay > maxY) maxY = ay; if (by > maxY) maxY = by;
        }
        double w = Math.Max(maxX - minX, 1e-9), h = Math.Max(maxY - minY, 1e-9);
        _cell = Math.Max(1e-6, Math.Sqrt(w * h / Math.Max(segs.Count, 1)));
        _cols = Math.Min(4096, Math.Max(1, (int)(w / _cell) + 1));
        _rows = Math.Min(4096, Math.Max(1, (int)(h / _cell) + 1));
        _cell = Math.Max(w / _cols, h / _rows);
        _ox = minX; _oy = minY;
        for (int i = 0; i < segs.Count; i++)
        {
            var (ax, ay, bx, by) = segs[i];
            int x0 = Cell(ax), x1 = Cell(bx), y0 = CellY(ay), y1 = CellY(by);
            if (x0 > x1) (x0, x1) = (x1, x0);
            if (y0 > y1) (y0, y1) = (y1, y0);
            for (int cx = x0; cx <= x1; cx++)
                for (int cy = y0; cy <= y1; cy++)
                {
                    var key = (cx, cy);
                    if (!_cells.TryGetValue(key, out var lst)) _cells[key] = lst = new List<int>();
                    lst.Add(i);
                }
        }
    }

    public List<(double ax, double ay, double bx, double by)> All => _segs;
    public int Count => _segs.Count;

    private int Cell(double x) => ClampD((int)Math.Floor((x - _ox) / _cell), 0, _cols - 1);
    private int CellY(double y) => ClampD((int)Math.Floor((y - _oy) / _cell), 0, _rows - 1);

    /// <summary>查询与给定 AABB 可能相交的边索引（粗筛，去重）。</summary>
    public IEnumerable<int> QueryBox(double minX, double minY, double maxX, double maxY)
    {
        if (_segs.Count == 0) yield break;
        int x0 = Cell(minX), x1 = Cell(maxX), y0 = CellY(minY), y1 = CellY(maxY);
        if (x0 > x1) (x0, x1) = (x1, x0);
        if (y0 > y1) (y0, y1) = (y1, y0);
        var seen = new HashSet<int>();
        for (int cx = x0; cx <= x1; cx++)
            for (int cy = y0; cy <= y1; cy++)
                if (_cells.TryGetValue((cx, cy), out var lst))
                    foreach (var i in lst)
                        if (seen.Add(i)) yield return i;
    }
}
}
