using System;
using System.Collections.Generic;
using System.Linq;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     在几何对象周围创建缓冲区（Minkowski 和圆形近似，顶点圆角连接）。
///     Point → 正多边形圆；Line/Polyline → 胶囊段并集；Polygon/Envelope → 各环偏移 + 裁剪清理。
///     负距离实现内缩（erosion）。
/// </summary>
public class BufferOperator : IGeometryOperator<Polygon>
{
    /// <summary>整圆默认离散段数（Esri Java 默认每 6° 一段 = 60；此处 100 提高精度）。</summary>
    public const int DefaultCircleSegments = 100;

    private static readonly Lazy<BufferOperator> _instance = new(() => new BufferOperator());

    private BufferOperator()
    {
    }

    /// <summary>
    ///     获取 BufferOperator 的单例实例。
    /// </summary>
    public static BufferOperator Instance => _instance.Value;

    /// <inheritdoc />
    public Polygon Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
    {
        throw new NotImplementedException(
            "Buffer operator requires a distance parameter. Use Execute(geometry, distance, spatialRef) instead.");
    }

    /// <summary>围绕几何对象创建缓冲区多边形。</summary>
    public Polygon Execute(Geometries.Geometry geometry, double distance,
        SpatialReference.SpatialReference? spatialRef = null)
        => Execute(geometry, distance, DefaultCircleSegments, spatialRef);

    /// <summary>带圆离散度控制的缓冲。</summary>
    public Polygon Execute(Geometries.Geometry geometry, double distance, int circleSegments,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));
        if (geometry.IsEmpty) return new Polygon();
        if (distance == 0) return GeometryOf(geometry);
        if (circleSegments < 8) circleSegments = 8;

        if (geometry is Point p)
            return ToPolygon(new List<PolygonClipper.Ring> { CircleRing(p.X, p.Y, Math.Abs(distance), circleSegments) });

        if (geometry is MultiPoint mpt)
        {
            var rings = new List<PolygonClipper.Ring>();
            foreach (var pt in mpt.GetPoints())
                rings.Add(CircleRing(pt.X, pt.Y, Math.Abs(distance), circleSegments));
            return ToPolygon(PolygonClipper.MakeValid(rings));
        }

        if (geometry is Line or Polyline)
        {
            var rings = new List<PolygonClipper.Ring>();
            foreach (var path in (geometry as Polyline)?.GetPaths().Select(pt => pt.ToList())
                         ?? new List<List<Point>> { new() { ((Line)geometry).Start, ((Line)geometry).End } })
            {
                for (int i = 0; i + 1 < path.Count; i++)
                {
                    var a = path[i]; var b = path[i + 1];
                    if (a.X == b.X && a.Y == b.Y)
                    {
                        rings.Add(CircleRing(a.X, a.Y, Math.Abs(distance), circleSegments));
                        continue;
                    }
                    rings.Add(StadiumRing(a.X, a.Y, b.X, b.Y, Math.Abs(distance), circleSegments));
                }
                if (path.Count == 1)
                    rings.Add(CircleRing(path[0].X, path[0].Y, Math.Abs(distance), circleSegments));
            }
            if (distance < 0) return new Polygon();
            return ToPolygon(PolygonClipper.MakeValid(rings));
        }

        if (geometry is Polygon or Envelope)
        {
            var rings = RingsOf(geometry);
            if (rings.Count == 0) return new Polygon();
            if (distance < 0)
                return ToPolygon(BufferNegative(rings, -distance, circleSegments));
            // 正缓冲：每环偏移曲线（凸角圆弧、凹角 miter），连同原图形作为一个非零绕数图形，
            // 一次 MakeValid（DCEL 面遍历）归一化，消解偏移环自交。
            var offset = new List<PolygonClipper.Ring>();
            foreach (var ring in PolygonClipper.OrientRings(rings))
                offset.Add(OffsetRing(ring, distance, circleSegments));
            offset.RemoveAll(r => r.Count < 3);
            return ToPolygon(PolygonClipper.MakeValid(offset.Concat(rings).ToList()));
        }

        throw new NotSupportedException($"Buffer operation for {geometry.Type} is not supported.");
    }

    /// <summary>
    /// 正缓冲片元分解：每条边 → 外推平行四边形；每个「凸角」（相对环方向的外侧转角，且转角超过圆弧步长）
    /// → 顶点圆盘（半径微放大避免与四边形顶点精确重合的退化）。全部片元简单无自交，经两两归并树求并。
    /// </summary>
    private static List<PolygonClipper.Ring> BufferPieces(List<PolygonClipper.Ring> rings, double r, int segments)
    {
        var pieces = new List<PolygonClipper.Ring>();
        double angleStep = 2 * Math.PI / segments;
        double rDisk = r; // 凸角圆盘与相邻四边形角点精确重合的退化交由裁剪器抖动化解
        foreach (var ring in PolygonClipper.OrientRings(rings))
        {
            double sigma = GeoMath.SignedArea2X(ring) > 0 ? 1 : -1; // 壳 CCW=+1、洞 CW=-1
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % n];
                double dx = b[0] - a[0], dy = b[1] - a[1];
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-14) continue;
                // 图形外侧法向：CCW 环右手法向 (dy,-dx)/len；CW 洞环其"外侧"= 朝向洞盘 = 同式（沿行进方向右手）
                double nx = sigma * dy / len, ny = -sigma * dx / len;
                var strip = new PolygonClipper.Ring(new[]
                {
                    a, b, new[] { b[0] + nx * r, b[1] + ny * r }, new[] { a[0] + nx * r, a[1] + ny * r },
                });
                // 片元必须与图形同向（正绕数），否则与非零绕数主体叠加时被抵消
                if (OpenGIS.Esri.Geometry.Core.Internal.GeoMath.SignedArea2X(strip) < 0) strip.Reverse();
                JitterPiece(strip, pieces.Count);
                pieces.Add(strip);
                // 凸角圆盘：入边×出边 的转角与 sigma 同号且大于圆弧步长
                var c = ring[(i + 2) % n];
                double dx2 = c[0] - b[0], dy2 = c[1] - b[1];
                double l2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                if (l2 < 1e-14) continue;
                double cross = (dx / len) * (dy2 / l2) - (dy / len) * (dx2 / l2);
                double turn = Math.Asin((cross < -1 ? -1 : cross > 1 ? 1 : cross));
                if (sigma * turn > angleStep * 0.75)
                {
                    var disk = CircleRing(b[0], b[1], rDisk, segments);
                    if (OpenGIS.Esri.Geometry.Core.Internal.GeoMath.SignedArea2X(disk) < 0) disk.Reverse();
                    JitterPiece(disk, pieces.Count);
                    pieces.Add(disk);
                }
            }
        }

        pieces.RemoveAll(p => p.Count < 3);
        return pieces;
    }

    /// <summary>负缓冲：erode(F,r) = F − grow(Fᶜ∩box, r)，其中 Fᶜ 用大矩形挖洞表示，全部输入均为简单环。</summary>
    private static List<PolygonClipper.Ring> BufferNegative(List<PolygonClipper.Ring> rings, double r, int segments)
    {
        rings = PolygonClipper.OrientRings(rings);
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var ring in rings)
            foreach (var p in ring)
            {
                if (p[0] < minX) minX = p[0]; if (p[0] > maxX) maxX = p[0];
                if (p[1] < minY) minY = p[1]; if (p[1] > maxY) maxY = p[1];
            }
        double pad = r + Math.Max(maxX - minX, maxY - minY) * 1e-6 + 1e-6;
        var box = new PolygonClipper.Ring(new[]
        {
            new[] { minX - pad, minY - pad }, new[] { maxX + pad, minY - pad },
            new[] { maxX + pad, maxY + pad }, new[] { minX + pad, maxY + pad },
        });
        // complement = box − F
        var comp = PolygonClipper.Boolean(new List<PolygonClipper.Ring> { box }, rings, PolygonClipper.ClipOp.Difference);
        // grow complement
        var grown = TournamentUnion(BufferPieces(comp, r, segments).Concat(comp).ToList());
        // result = F − grown
        return PolygonClipper.Boolean(rings, grown, PolygonClipper.ClipOp.Difference);
    }

    /// <summary>两两归并树求并：O(n log n) 次布尔，避免顺序折叠的 O(n²)。</summary>
    internal static List<PolygonClipper.Ring> DebugTournament(List<PolygonClipper.Ring> rings, System.IO.TextWriter log) => TournamentUnionLogged(rings, log);
    internal static List<PolygonClipper.Ring> DebugPieces(List<PolygonClipper.Ring> rings, double r, int seg) => BufferPieces(rings, r, seg);

    private static List<PolygonClipper.Ring> TournamentUnion(List<PolygonClipper.Ring> rings)
    {
        return TournamentUnionLogged(rings, null);
    }

    private static List<PolygonClipper.Ring> TournamentUnionLogged(List<PolygonClipper.Ring> rings, System.IO.TextWriter log)
    {
        var swLevel = System.Diagnostics.Stopwatch.StartNew();
        if (rings.Count == 0) return rings;
        var level = rings;
        while (level.Count > 1)
        {
            var next = new List<PolygonClipper.Ring>();
            for (int i = 0; i < level.Count; i += 2)
            {
                if (i + 1 == level.Count) { next.Add(level[i]); continue; }
                next.AddRange(PolygonClipper.Boolean(new List<PolygonClipper.Ring> { level[i] },
                    new List<PolygonClipper.Ring> { level[i + 1] }, PolygonClipper.ClipOp.Union));
            }
            level = PolygonClipper.OrientRings(next); // 归一层，控制规模与方向规范
            log?.WriteLine($"  level -> rings {level.Count} edges {level.Sum(r => r.Count)} elapsedMs={swLevel.ElapsedMilliseconds}");
            swLevel.Restart();
        }
        return level;
    }

    /// <summary>
    /// 环偏移：沿右手法向（相对环方向；壳 CCW → 外侧、洞 CW → 洞内侧，统一规则 distance&gt;0 扩张图形）。
    /// 负 distance 反向。凸角用圆弧接缝（弦中点不在环盘内则补弧），凹角 miter 相交；自交由裁剪器清理。
    /// </summary>
    private static PolygonClipper.Ring OffsetRing(PolygonClipper.Ring ring, double distance, int segments)
    {
        int n = ring.Count;
        double r = distance;
        // 右法向 = (dy, -dx)/len；对 CCW 壳向外、CW 洞向洞内 → distance>0 时膨胀图形
        double s = r >= 0 ? 1 : -1;
        r = Math.Abs(r);
        var norms = new double[n][];
        for (int i = 0; i < n; i++)
        {
            var p = ring[i]; var q = ring[(i + 1) % n];
            double dx = q[0] - p[0], dy = q[1] - p[1];
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-15) { norms[i] = new[] { 0.0, 0.0 }; continue; }
            norms[i] = new[] { s * dy / len * r, -s * dx / len * r };
        }

        var outRing = new PolygonClipper.Ring();

        double angleStep = 2 * Math.PI / segments;
        for (int i = 0; i < n; i++)
        {
            var v = ring[i];
            var nPrev = norms[(i - 1 + n) % n];
            var nCur = norms[i];
            var a1 = new[] { v[0] + nPrev[0], v[1] + nPrev[1] }; // 前一条偏移边的终点位置（在顶点处）
            var a2 = new[] { v[0] + nCur[0], v[1] + nCur[1] };   // 当前偏移边的起点位置
            // 两偏移直线求交（miter）
            double? mx = null, my = null;
            {
                var p0 = ring[(i - 1 + n) % n]; var p1 = v;
                var p2 = v; var p3 = ring[(i + 1) % n];
                double d1x = p1[0] - p0[0], d1y = p1[1] - p0[1];
                double d2x = p3[0] - p2[0], d2y = p3[1] - p2[1];
                double det = d1x * d2y - d1y * d2x;
                if (Math.Abs(det) > 1e-12)
                {
                    double ox1 = a1[0], oy1 = a1[1], ox2 = a2[0], oy2 = a2[1];
                    double t = ((ox2 - ox1) * d2y - (oy2 - oy1) * d2x) / det;
                    mx = p0[0] + t * d1x; my = p0[1] + t * d1y;
                }
            }
            // 局部转向判据：σ=+1（扩张）时左转角（cross>0）为凸角缺口需补弧；右转角为凹角重叠用 miter。
            // σ=-1（收缩）相反。避免逐顶点全环绕数的 O(E²)。
            double td1x = v[0] - ring[(i - 1 + n) % n][0], td1y = v[1] - ring[(i - 1 + n) % n][1];
            double td2x = ring[(i + 1) % n][0] - v[0], td2y = ring[(i + 1) % n][1] - v[1];
            double crossTurn = td1x * td2y - td1y * td2x;
            bool overlap = s * crossTurn <= 0;
            double chordMx = (a1[0] + a2[0]) / 2, chordMy = (a1[1] + a2[1]) / 2;
            if (overlap)
            {
                // 凹角：miter 相交点；miter 不存在/过远时 butt 直连（缺口由整体自交归一补上），
                // 绝不可像凸角那样补外侧弧（方向错误会造成 flap 自交碎环）
                if (mx is double mX && my is double mY &&
                    Math.Sqrt((mX - v[0]) * (mX - v[0]) + (mY - v[1]) * (mY - v[1])) <= r * 4.1)
                    outRing.Add(new[] { mX, mY });
                else
                {
                    outRing.Add(a1);
                    outRing.Add(a2);
                }
                continue;
            }
            {
                outRing.Add(a1);
                double t1 = Math.Atan2(a1[1] - v[1], a1[0] - v[0]);
                double t2 = Math.Atan2(a2[1] - v[1], a2[0] - v[0]);
                double delta = t2 - t1;
                while (delta > Math.PI) delta -= 2 * Math.PI;
                while (delta <= -Math.PI) delta += 2 * Math.PI;
                int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(delta) / angleStep));
                for (int k = 1; k < steps; k++)
                {
                    double t = t1 + delta * k / steps;
                    outRing.Add(new[] { v[0] + r * Math.Cos(t), v[1] + r * Math.Sin(t) });
                }
            }
        }
        return outRing;
    }

    private static List<PolygonClipper.Ring> UnionAll(List<PolygonClipper.Ring> ringsA, List<PolygonClipper.Ring>? ringsB)
    {
        var current = ringsA;
        if (ringsB is { Count: > 0 })
        {
            current = PolygonClipper.Boolean(current, ringsB, PolygonClipper.ClipOp.Union);
            if (current.Count == 0) current = ringsA; // 数值兜底
            return current;
        }
        // 自并：逐个吸收（先两两合并到稳定集）
        var acc = new List<PolygonClipper.Ring>();
        foreach (var r in ringsA)
        {
            acc = PolygonClipper.Boolean(acc, new List<PolygonClipper.Ring> { r }, PolygonClipper.ClipOp.Union);
            if (acc.Count == 0) acc = new List<PolygonClipper.Ring> { r };
        }
        return acc;
    }

    /// <summary>
    /// 片元去退化微移：piece 之间共享构造点（同一顶点的条带端/圆盘边界严格重合），
    /// 统一做伪随机 ±1e-8·extent 独立平移破坏精确重合——布尔/自重叠归一化的数值稳定性远大于该误差收益。
    /// </summary>
    private static void JitterPiece(PolygonClipper.Ring piece, int seed)
    {
        double ext = 1;
        foreach (var p in piece) ext = Math.Max(ext, Math.Max(Math.Abs(p[0]), Math.Abs(p[1])));
        double amp = 1e-8 * ext;
        double h1 = Math.Sin(seed * 12.9898 + 78.233) * 43758.5453;
        double h2 = Math.Sin(seed * 39.425 + 11.135) * 24634.6345;
        double ox = (h1 - Math.Floor(h1) - 0.5) * 2 * amp;
        double oy = (h2 - Math.Floor(h2) - 0.5) * 2 * amp;
        foreach (var p in piece) { p[0] += ox; p[1] += oy; }
    }

    private static PolygonClipper.Ring CircleRing(double cx, double cy, double r, int segments)
    {
        var ring = new PolygonClipper.Ring();
        for (int i = 0; i < segments; i++)
        {
            double a = 2 * Math.PI * i / segments;
            ring.Add(new[] { cx + r * Math.Cos(a), cy + r * Math.Sin(a) });
        }
        return ring;
    }

    private static PolygonClipper.Ring StadiumRing(double ax, double ay, double bx, double by, double r, int segments)
    {
        double dx = bx - ax, dy = by - ay;
        double len = Math.Sqrt(dx * dx + dy * dy);
        double ux = dx / len, uy = dy / len;
        double baseAng = Math.Atan2(uy, ux);
        int half = Math.Max(2, segments / 2);
        var ring = new PolygonClipper.Ring();
        for (int i = 0; i <= half; i++)
        {
            double a = baseAng - Math.PI / 2 + Math.PI * i / half;
            ring.Add(new[] { bx + r * Math.Cos(a), by + r * Math.Sin(a) });
        }
        for (int i = 0; i <= half; i++)
        {
            double a = baseAng + Math.PI / 2 + Math.PI * i / half;
            ring.Add(new[] { ax + r * Math.Cos(a), ay + r * Math.Sin(a) });
        }
        return ring;
    }

    private static List<PolygonClipper.Ring> RingsOf(Geometries.Geometry g)
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
        foreach (var ring in PolygonClipper.OrientRings(rings))
        {
            var pts = ring.Select(p => new Point(p[0], p[1])).ToList();
            pts.Add(pts[0]);
            poly.AddRing(pts);
        }
        return poly;
    }

    private static Polygon GeometryOf(Geometries.Geometry g)
    {
        if (g is Polygon p) return p;
        if (g is Envelope e)
        {
            var poly = new Polygon();
            poly.AddRing(new[]
            {
                new Point(e.XMin, e.YMin), new Point(e.XMax, e.YMin), new Point(e.XMax, e.YMax), new Point(e.XMin, e.YMax), new Point(e.XMin, e.YMin),
            });
            return poly;
        }

        return new Polygon();
    }
}

/// <summary>内部诊断入口（测试 harness 可见）。</summary>
internal static class BufferInspector
{
    public static System.Collections.Generic.List<OpenGIS.Esri.Geometry.Core.Internal.PolygonClipper.Ring> Pieces(
        System.Collections.Generic.List<OpenGIS.Esri.Geometry.Core.Internal.PolygonClipper.Ring> rings)
        => BufferOperator.DebugPieces(rings, 0.05, BufferOperator.DefaultCircleSegments);
    public static System.Collections.Generic.List<OpenGIS.Esri.Geometry.Core.Internal.PolygonClipper.Ring> Tournament(
        System.Collections.Generic.List<OpenGIS.Esri.Geometry.Core.Internal.PolygonClipper.Ring> rings, System.IO.TextWriter log)
        => BufferOperator.DebugTournament(rings, log);
}
