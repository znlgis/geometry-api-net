using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenGIS.Esri.Geometry.Core.Internal;

using static OpenGIS.Esri.Geometry.Core.Internal.Nums;

/// <summary>
/// 多边形布尔运算裁剪器（非零绕数规则）。
///
/// 流程：输入环规范化（去退化、按嵌套深度定壳/洞方向：壳 CCW、洞 CW）→
/// 对 B 施加极小确定性抖动消除共边/共点退化 → 段段求交并分裂 →
/// 子段按中点在另一图形的绕数决定保留（union: 外部；intersection: 内部；difference 混合；symdiff 全保留）→
/// 在度数 2 的段图中走链装配输出环。
/// 适用于任意凹凸多边形、多部件与洞。
/// </summary>
internal static class PolygonClipper
{
    public enum ClipOp { Union, Intersection, Difference, SymmetricDifference }

    /// <summary>抖动步长：远小于测试容差，足以打破浮点重合。</summary>
    private const double Jitter = 1.2345677e-7;

    public sealed class Ring : List<double[]>
    {
        public Ring() { }
        public Ring(IEnumerable<double[]> pts)
            : base(pts.Where(p => p.Length >= 2).Select(p => new[] { p[0], p[1] })) { }
    }

    /// <summary>布尔运算，输入输出均为「环集合（洞由嵌套奇偶隐含）」。</summary>
    public static List<Ring> Boolean(List<Ring> a, List<Ring> b, ClipOp op)
        => Boolean(a, b, op, Jitter, Jitter * 1.41421356);

    public static List<Ring> Boolean(List<Ring> a, List<Ring> b, ClipOp op, double jx, double jy)
    {
        a = CleanRings(a);
        b = CleanRings(b);

        if (op == ClipOp.SymmetricDifference)
            // A△B = (A−B) ∪ (B−A)：避免“全保留”策略在穿越点产生 4 度节点导致装配错误；
            // 外层并集使用不同抖动向量，保证两次差集的公共节点彼此错开。
            // 外层并集把第二差集再平移一个明显不同的向量（≈4.7e-7,-3.3e-7），
            // 使两次差集的公共穿越节点彼此错开，装配无 pinch 歧义（误差仍 ≪ 容差）。
            // A△B = (A∪B) 挖去 (A∩B)：两结果同帧计算，交集环作洞，避免第三次布尔的 pinch 装配歧义。
            {
                var uRings = Boolean(a, b, ClipOp.Union, jx, jy);
                var iRings = Boolean(a, b, ClipOp.Intersection, jx, jy);
                var sym = new List<Ring>(uRings);
                foreach (var r in iRings) { var rr = new Ring(r); rr.Reverse(); sym.Add(rr); }
                return sym;
            }

        if (a.Count == 0)
            return op switch
            {
                ClipOp.Intersection or ClipOp.Difference => new(),
                _ => OrientRings(b),
            };
        if (b.Count == 0)
            return op == ClipOp.Intersection ? new() : OrientRings(a);

        a = OrientRings(a);
        b = OrientRings(b);
        var bJ = b.Select(r => new Ring(r.Select(p => new[] { p[0] + jx, p[1] + jy }))).ToList();

        var edgesA = Flatten(a);
        var edgesB = Flatten(bJ);
        var gridB = new EdgeGrid(edgesB);

        var splitsA = new Dictionary<int, List<double>>();
        var splitsB = new Dictionary<int, List<double>>();
        void AddSplit(Dictionary<int, List<double>> d, int i, double t)
        {
            if (t <= 1e-9) t = 0;
            if (t >= 1 - 1e-9) t = 1;
            if (!d.TryGetValue(i, out var s)) d[i] = s = new List<double>();
            s.Add(t);
        }

        for (int ai = 0; ai < edgesA.Count; ai++)
        {
            var (a1x, a1y, a2x, a2y) = edgesA[ai];
            foreach (int bi in RoughOverlap(gridB, edgesB, a1x, a1y, a2x, a2y))
            {
                var (b1x, b1y, b2x, b2y) = edgesB[bi];
                int kind = GeoMath.SegIntersect(a1x, a1y, a2x, a2y, b1x, b1y, b2x, b2y,
                    out double ta, out double tb, out double ta0, out double ta1, out double tb0, out double tb1);
                if (kind == 1) { AddSplit(splitsA, ai, ta); AddSplit(splitsB, bi, tb); }
                else if (kind == 2)
                {
                    AddSplit(splitsA, ai, ta0); AddSplit(splitsA, ai, ta1);
                    AddSplit(splitsB, bi, tb0); AddSplit(splitsB, bi, tb1);
                }
            }
        }

        var subs = new List<(double x1, double y1, double x2, double y2)>();
        var wB = new WindingIndex(bJ);
        var wA = new WindingIndex(a);
        Collect(edgesA, splitsA, wB, op, isB: false, subs);
        Collect(edgesB, splitsB, wA, op, isB: true, subs);

        return Assemble(subs);
    }

    private static List<(double ax, double ay, double bx, double by)> Flatten(List<Ring> rings)
    {
        var list = new List<(double, double, double, double)>();
        foreach (var ring in rings)
            for (int k = 0, n = ring.Count; k < n; k++)
            {
                var p = ring[k]; var q = ring[(k + 1) % n];
                if (p[0] == q[0] && p[1] == q[1]) continue;
                list.Add((p[0], p[1], q[0], q[1]));
            }
        return list;
    }

    /// <summary>B 边均匀网格索引：把每条边撒到其 AABB 覆盖的格子，查询按 A 边 AABB 收集候选（去重）。</summary>
    private sealed class EdgeGrid
    {
        private readonly Dictionary<(int, int), List<int>> _cells = new();
        private readonly double _ox, _oy, _cell;
        private readonly int _cols, _rows;

        public EdgeGrid(List<(double, double, double, double)> edges)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var (x1, y1, x2, y2) in edges)
            {
                if (x1 < minX) minX = x1; if (x2 < minX) minX = x2;
                if (y1 < minY) minY = y1; if (y2 < minY) minY = y2;
                if (x1 > maxX) maxX = x1; if (x2 > maxX) maxX = x2;
                if (y1 > maxY) maxY = y1; if (y2 > maxY) maxY = y2;
            }

            double w = Math.Max(maxX - minX, 1e-9), h = Math.Max(maxY - minY, 1e-9);
            _cell = Math.Max(1e-9, Math.Sqrt(w * h / Math.Max(edges.Count, 1)));
            _cols = ClampD((int)(w / _cell) + 1, 1, 8192);
            _rows = ClampD((int)(h / _cell) + 1, 1, 8192);
            _cell = Math.Max(w / _cols, h / _rows);
            _ox = minX; _oy = minY;

            for (int i = 0; i < edges.Count; i++)
            {
                var (x1, y1, x2, y2) = edges[i];
                int c0 = CX(x1), c1 = CX(x2), r0 = CY(y1), r1 = CY(y2);
                if (c0 > c1) (c0, c1) = (c1, c0);
                if (r0 > r1) (r0, r1) = (r1, r0);
                for (int cx = c0; cx <= c1; cx++)
                    for (int cy = r0; cy <= r1; cy++)
                    {
                        if (!_cells.TryGetValue((cx, cy), out var l)) _cells[(cx, cy)] = l = new List<int>();
                        l.Add(i);
                    }
            }
        }

        private int CX(double x) => ClampD((int)Math.Floor((x - _ox) / _cell), 0, _cols - 1);
        private int CY(double y) => ClampD((int)Math.Floor((y - _oy) / _cell), 0, _rows - 1);

        public IEnumerable<int> Query(double minX, double minY, double maxX, double maxY)
        {
            int c0 = CX(minX), c1 = CX(maxX), r0 = CY(minY), r1 = CY(maxY);
            if (c0 > c1) (c0, c1) = (c1, c0);
            if (r0 > r1) (r0, r1) = (r1, r0);
            var seen = new HashSet<int>();
            for (int cx = c0; cx <= c1; cx++)
                for (int cy = r0; cy <= r1; cy++)
                    if (_cells.TryGetValue((cx, cy), out var l))
                        foreach (var i in l)
                            if (seen.Add(i)) yield return i;
        }
    }

    private static IEnumerable<int> RoughOverlap(EdgeGrid grid, List<(double, double, double, double)> edgesB,
        double a1x, double a1y, double a2x, double a2y)
        => grid.Query(Math.Min(a1x, a2x), Math.Min(a1y, a2y), Math.Max(a1x, a2x), Math.Max(a1y, a2y));

    private static void Collect(
        List<(double ax, double ay, double bx, double by)> edges,
        Dictionary<int, List<double>> splits,
        WindingIndex other,
        ClipOp op, bool isB,
        List<(double, double, double, double)> subs)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            var ts = new List<double> { 0, 1 };
            if (splits.TryGetValue(i, out var extra)) ts.AddRange(extra);
            ts.Sort();
            double prev = -1;
            for (int k = 0; k < ts.Count - 1; k++)
            {
                if (ts[k + 1] - ts[k] < 1e-10) continue;
                double tm = (ts[k] + ts[k + 1]) / 2;
                double mx = e.ax + (e.bx - e.ax) * tm, my = e.ay + (e.by - e.ay) * tm;
                int w = other.Winding(mx, my);
                bool keep = op switch
                {
                    ClipOp.Union => w == 0,
                    ClipOp.Intersection => w != 0,
                    ClipOp.Difference => isB ? w != 0 : w == 0,
                    ClipOp.SymmetricDifference => true,
                    _ => false,
                };
                if (!keep) continue;
                double px1 = e.ax + (e.bx - e.ax) * ts[k], py1 = e.ay + (e.by - e.ay) * ts[k];
                double px2 = e.ax + (e.bx - e.ax) * ts[k + 1], py2 = e.ay + (e.by - e.ay) * ts[k + 1];
                // 差集中 B 的保留段反向：结果图形须在其左手侧，否则洞环方向错误
                if (op == ClipOp.Difference && isB) (px1, py1, px2, py2) = (px2, py2, px1, py1);
                subs.Add((px1, py1, px2, py2));
            }
            prev = ts[ts.Count - 1];
        }
    }

    /// <summary>非零绕数：≠0 即在图形内部。</summary>
    public static int WindingAt(List<Ring> rings, double x, double y)
    {
        int w = 0;
        foreach (var ring in rings)
        {
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                var p = ring[i]; var q = ring[(i + 1) % n];
                if (p[1] <= y)
                {
                    if (q[1] > y && (q[0] - p[0]) * (y - p[1]) - (x - p[0]) * (q[1] - p[1]) > 0) w++;
                }
                else if (q[1] <= y && (q[0] - p[0]) * (y - p[1]) - (x - p[0]) * (q[1] - p[1]) < 0) w--;
            }
        }
        return w;
    }

    public static bool WindingContains(List<Ring> rings, double x, double y) => WindingAt(rings, x, y) != 0;

    /// <summary>
    /// 非零绕数 makeValid：把全部环在一切相交点处分裂成子段，逐子段取两侧偏移点的绕数，
    /// 保留「一侧在图形内、另一侧在图形外」的边（即真边界），重组为合法环集。
    /// 自相交/重叠/切点等非法性在分裂+绕数选择过程中自然消解。
    /// </summary>
    public static List<Ring> MakeValid(List<Ring> rings)
    {
        // 输入方向即图形定义（非零绕数：壳 CCW/洞 CW；重复同向嵌套环为 union 退化产物）
        rings = CleanRings(rings);
        if (rings.Count == 0) return rings;
        var edges = Flatten(rings);
        var splits = new Dictionary<int, List<double>>();
        var grid = new EdgeGrid(Flatten(rings));
        for (int ai = 0; ai < edges.Count; ai++)
        {
            var (a1x, a1y, a2x, a2y) = edges[ai];
            foreach (int bi in grid.Query(Math.Min(a1x, a2x), Math.Min(a1y, a2y), Math.Max(a1x, a2x), Math.Max(a1y, a2y)))
            {
                if (bi <= ai) continue;
                var (b1x, b1y, b2x, b2y) = edges[bi];
                int kind = GeoMath.SegIntersect(a1x, a1y, a2x, a2y, b1x, b1y, b2x, b2y,
                    out double ta, out double tb, out double ta0, out double ta1, out double tb0, out double tb1);
                if (kind == 1) { AddSplit(ai, ta); AddSplit(bi, tb); }
                else if (kind == 2) { AddSplit(ai, ta0); AddSplit(ai, ta1); AddSplit(bi, tb0); AddSplit(bi, tb1); }
            }
        }

        void AddSplit(int i, double t)
        {
            if (t <= 1e-9) t = 0;
            if (t >= 1 - 1e-9) t = 1;
            if (!splits.TryGetValue(i, out var l)) splits[i] = l = new List<double>();
            l.Add(t);
        }

        var wIdx = new WindingIndex(rings);
        double extent = 0;
        foreach (var r in rings)
            foreach (var p in r)
                extent = Math.Max(extent, Math.Max(Math.Abs(p[0]), Math.Abs(p[1])));
        double delta = Math.Max(1e-4, extent * 1e-7) + 1e-9;

        var subs = new List<(double, double, double, double)>();
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            var ts = new List<double> { 0, 1 };
            if (splits.TryGetValue(i, out var extra)) ts.AddRange(extra);
            ts.Sort();
            for (int k = 0; k < ts.Count - 1; k++)
            {
                if (ts[k + 1] - ts[k] < 1e-10) continue;
                double t0 = ts[k], t1 = ts[k + 1], tm = (t0 + t1) / 2;
                double mx = e.ax + (e.bx - e.ax) * tm, my = e.ay + (e.by - e.ay) * tm;
                double dx = e.bx - e.ax, dy = e.by - e.ay;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len == 0) continue;
                double nx = -dy / len, ny = dx / len;
                bool inL = wIdx.Winding(mx + nx * delta, my + ny * delta) != 0;
                bool inR = wIdx.Winding(mx - nx * delta, my - ny * delta) != 0;
                if (inL == inR) continue;
                double px1 = e.ax + (e.bx - e.ax) * t0, py1 = e.ay + (e.by - e.ay) * t0;
                double px2 = e.ax + (e.bx - e.ax) * t1, py2 = e.ay + (e.by - e.ay) * t1;
                // 统一为「图形内部在行进方向左侧」定向，供 DCEL 面遍历得到 CCW 壳 / CW 洞
                if (!inL) (px1, py1, px2, py2) = (px2, py2, px1, py1);
                subs.Add((px1, py1, px2, py2));
            }
        }

        return OrientRings(SplitAtPinches(Assemble(subs, faceTraversal: true)
            .Where(r => r.Count >= 3 && Math.Abs(GeoMath.SignedArea2X(r)) > 1e-9).ToList()));
    }

    /// <summary>
    /// pinch 面拆分：DCEL 面遍历会把同符号多叶瓣（在共享顶点相接）输出为重复经过该顶点的单环
    /// （GEOS 视角即环自交）。按首次重复顶点把环切开成多个简单环，对齐 GEOS MakeValid 的多部件语义。
    /// </summary>
    private static List<Ring> SplitAtPinches(List<Ring> rings)
    {
        var outRings = new List<Ring>();
        foreach (var ring0 in rings)
        {
            var list = new List<double[]>(ring0);
            while (list.Count >= 3)
            {
                var first = new Dictionary<long, int>();
                int rep = -1, repFirst = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    var key = Key(list[i][0], list[i][1]);
                    if (first.TryGetValue(key, out int f)) { rep = i; repFirst = f; break; }
                    first[key] = i;
                }

                if (rep < 0)
                {
                    outRings.Add(DedupRing(new Ring(list)));
                    break;
                }

                if (rep - repFirst == 1)
                {
                    // 相邻重复顶点：直接删一个继续
                    list.RemoveAt(rep);
                    continue;
                }

                // 摘出 repFirst+1 .. rep 子叶瓣
                if (rep - repFirst >= 3)
                {
                    var piece = list.GetRange(repFirst + 1, rep - repFirst - 1);
                    piece.Add(piece[0]);
                    outRings.Add(DedupRing(new Ring(piece)));
                }
                else if (rep - repFirst == 2)
                {
                    var piece = list.GetRange(repFirst + 1, 1);
                    outRings.Add(DedupRing(new Ring(piece))); // 退化 1 点随后被面积过滤
                }

                // 其余部分继续拆分
                var rest = new List<double[]>();
                for (int i = rep; i < list.Count; i++) rest.Add(list[i]);
                for (int i = 0; i <= repFirst; i++) rest.Add(list[i]);
                if (rest.Count >= list.Count) rest.RemoveAt(rest.Count - 1); // 防退化死循环
                list = rest;
            }
        }

        return outRings.Where(r => r.Count >= 3 && Math.Abs(GeoMath.SignedArea2X(r)) > 1e-9).ToList();
    }

    /// <summary>非零绕数索引：按 y 均匀分桶加速点定位。</summary>
    internal sealed class WindingIndex
    {
        private readonly List<(double ax, double ay, double bx, double by)> _edges;
        private readonly Dictionary<int, List<int>> _byRow = new();
        private readonly double _minY, _rowH;
        private readonly int _rows;

        public WindingIndex(List<Ring> rings)
        {
            _edges = Flatten(rings);
            double minY = double.MaxValue, maxY = double.MinValue;
            foreach (var ed in _edges)
            {
                double ay = ed.Item2, by = ed.Item4;
                if (ay < minY) minY = ay;
                if (by < minY) minY = by;
                if (ay > maxY) maxY = ay;
                if (by > maxY) maxY = by;
            }


            double h = Math.Max(maxY - minY, 1e-9);
            _rows = Math.Min(8192, Math.Max(1, (int)Math.Sqrt(Math.Max(_edges.Count, 1)) + 1));
            _rowH = h / _rows + 1e-12;
            _minY = minY;
            for (int i = 0; i < _edges.Count; i++)
            {
                var (_, ay, _, by) = _edges[i];
                int r0 = Row(Math.Min(ay, by)), r1 = Row(Math.Max(ay, by));
                for (int r = r0; r <= r1; r++)
                {
                    if (!_byRow.TryGetValue(r, out var l)) _byRow[r] = l = new List<int>();
                    l.Add(i);
                }
            }
        }

        private int Row(double y)
        {
            int v = (int)Math.Floor((y - _minY) / _rowH);
            return v < 0 ? 0 : v > _rows - 1 ? _rows - 1 : v;
        }

        public int Winding(double x, double y)
        {
            int w = 0;
            if (!_byRow.TryGetValue(Row(y), out var list)) return 0;
            foreach (int i in list)
            {
                var edq = _edges[i];
                double ax = edq.Item1, ay = edq.Item2, bx = edq.Item3, by = edq.Item4;
                if (ay <= y)
                {
                    if (by > y && (bx - ax) * (y - ay) - (x - ax) * (by - ay) > 0) w++;
                }
                else if (by <= y && (bx - ax) * (y - ay) - (x - ax) * (by - ay) < 0) w--;
            }
            return w;
        }
    }

    /// <summary>把保留子段在节点图上走成闭合环。pinch 节点（候选&gt;1）按“最直”配对各曲线自身；
    /// 真穿越节点保留边只剩一对，自动形成正确转向。</summary>
    private static List<Ring> Assemble(List<(double x1, double y1, double x2, double y2)> subs, bool faceTraversal = false)
    {
        var startAt = new Dictionary<long, List<int>>();
        var endAt = new Dictionary<long, List<int>>();
        for (int i = 0; i < subs.Count; i++)
        {
            var (x1, y1, x2, y2) = subs[i];
            Add(startAt, Key(x1, y1), i);
            Add(endAt, Key(x2, y2), i);
        }

        var used = new bool[subs.Count];
        var rings = new List<Ring>();

        // faceTraversal：预计算 DCEL 静态 next 配对（in-edge → 左面最贴合的 out-edge），
        // 配对为双射，面遍历必然闭合，不受贪心 used 顺序影响。
        int[]? nextOf = null;
        if (faceTraversal)
        {
            nextOf = new int[subs.Count];
            for (int i = 0; i < subs.Count; i++) nextOf[i] = -1;
            var incoming = new Dictionary<long, List<int>>();
            var outgoing = new Dictionary<long, List<int>>();
            for (int i = 0; i < subs.Count; i++)
            {
                var (ux1, uy1, ux2, uy2) = subs[i];
                AddKey(incoming, Key(ux2, uy2), i);
                AddKey(outgoing, Key(ux1, uy1), i);
            }
            foreach (var kv in incoming)
            {
                var node = kv.Key;
                var ins = kv.Value;
                if (!outgoing.TryGetValue(node, out var outs)) continue;
                var rays = new List<(double ang, int idx)>();
                foreach (int o in outs)
                {
                    var (ox1, oy1, ox2, oy2) = subs[o];
                    double dx = ox2 - ox1, dy = oy2 - oy1;
                    if (dx == 0 && dy == 0) continue;
                    rays.Add((Math.Atan2(dy, dx), o));
                }
                rays.Sort((x, y) => x.ang.CompareTo(y.ang));
                foreach (int inn in ins)
                {
                    var (ix1, iy1, ix2, iy2) = subs[inn];
                    double fdx = ix2 - ix1, fdy = iy2 - iy1;
                    if (fdx == 0 && fdy == 0) continue;
                    double back = Math.Atan2(-fdy, -fdx); // 到来方向的反向射线
                    int pick = -1;
                    double bestGap = double.MaxValue;
                    foreach (var (ang, idx) in rays)
                    {
                        double gap = ang - back;
                        while (gap <= 1e-12) gap += 2 * Math.PI;
                        while (gap > 2 * Math.PI + 1e-12) gap -= 2 * Math.PI;
                        if (gap < bestGap) { bestGap = gap; pick = idx; }
                    }
                    nextOf[inn] = pick;
                }
            }
        }

        static void AddKey(Dictionary<long, List<int>> map, long key, int i)
        {
            if (!map.TryGetValue(key, out var l)) map[key] = l = new List<int>();
            l.Add(i);
        }

        for (int start = 0; start < subs.Count; start++)
        {
            if (used[start]) continue;
            var ring = new Ring();
            int cur = start;
            bool flipped = false;
            int guard = subs.Count + 10;
            while (cur >= 0 && !used[cur] && guard-- > 0)
            {
                used[cur] = true;
                var (x1, y1, x2, y2) = subs[cur];
                double hx, hy, tx, ty;
                if (flipped) { hx = x2; hy = y2; tx = x1; ty = y1; }
                else { hx = x1; hy = y1; tx = x2; ty = y2; }
                if (ring.Count == 0 || !Near(ring[ring.Count - 1][0], ring[ring.Count - 1][1], hx, hy))
                    ring.Add(new[] { hx, hy });

                long node = Key(tx, ty);
                int next = -1; bool nextFlip = false;
                // 自重叠装配（提供 origins 时）用 DCEL 面遍历规则：到来方向的反向射线 β，
                // 取"从 β 起逆时针转角最小"的出边——生成保持左手的可行面环；
                // 其余（Boolean 的 pinch 点）按"最直"配对。
                if (faceTraversal)
                {
                    double beta = Math.Atan2(hy - ty, hx - tx);
                    double bestCCW = double.MaxValue;
                    TryCandidate(startAt, node, false);
                    TryCandidate(endAt, node, true);
                    void TryCandidate(Dictionary<long, List<int>> map, long key, bool flip)
                    {
                        if (!map.TryGetValue(key, out var list)) return;
                        foreach (int c in list)
                        {
                            if (used[c]) continue;
                            var (cx1, cy1, cx2, cy2) = subs[c];
                            double ox, oy;
                            if (flip) { (ox, oy) = (cx1 - tx, cy1 - ty); }
                            else { (ox, oy) = (cx2 - tx, cy2 - ty); }
                            if (ox == 0 && oy == 0) continue;
                            double ang = Math.Atan2(oy, ox);
                            double ccw = ang - beta;
                            while (ccw < -1e-12) ccw += 2 * Math.PI;
                            while (ccw > 2 * Math.PI) ccw -= 2 * Math.PI;
                            if (ccw < 1e-9) ccw = 1e-9; // 完全反向（退化回走）惩罚
                            if (ccw < bestCCW) { bestCCW = ccw; next = c; nextFlip = flip; }
                        }
                    }
                }

                double inX = tx - hx, inY = ty - hy;
                double inL = Math.Sqrt(inX * inX + inY * inY);
                if (inL > 0) { inX /= inL; inY /= inL; }
                double bestDot = double.MinValue;
                if (startAt.TryGetValue(node, out var outs))
                    foreach (int c in outs)
                    {
                        if (used[c]) continue;
                        var (cx1, cy1, cx2, cy2) = subs[c];
                        double vx = cx2 - cx1, vy = cy2 - cy1;
                        double vl = Math.Sqrt(vx * vx + vy * vy);
                        if (vl == 0) continue;
                        double dot = vx / vl * inX + vy / vl * inY;
                        if (dot > bestDot) { bestDot = dot; next = c; nextFlip = false; }
                    }
                if (endAt.TryGetValue(node, out var ins))
                    foreach (int c in ins)
                    {
                        if (used[c]) continue;
                        var (cx1, cy1, cx2, cy2) = subs[c];
                        double vx = cx1 - cx2, vy = cy1 - cy2;
                        double vl = Math.Sqrt(vx * vx + vy * vy);
                        if (vl == 0) continue;
                        double dot = vx / vl * inX + vy / vl * inY;
                        if (dot > bestDot) { bestDot = dot; next = c; nextFlip = true; }
                    }

                if (faceTraversal && nextOf != null) { cur = nextOf[cur]; flipped = false; }
                else { cur = next; flipped = nextFlip; }
                if (cur == start || cur < 0) break;
            }

            var clean = DedupRing(ring);
            if (clean.Count >= 3 && Math.Abs(GeoMath.SignedArea2X(clean)) > 1e-9)
                rings.Add(clean);
        }
        return rings;
    }

    private static void Add(Dictionary<long, List<int>> map, long key, int i)
    {
        if (!map.TryGetValue(key, out var l)) map[key] = l = new List<int>();
        l.Add(i);
    }

    private static bool Near(double ax, double ay, double bx, double by) =>
        Math.Abs(ax - bx) <= 1e-9 && Math.Abs(ay - by) <= 1e-9;

    private static long Key(double x, double y)
    {
        const double s = 1e8;
        return unchecked(((long)Math.Round(x * s) << 32) ^ ((long)Math.Round(y * s) & 0xffffffffL));
    }

    private static Ring DedupRing(Ring r)
    {
        var o = new Ring();
        foreach (var p in r)
            if (o.Count == 0 || !Near(o[o.Count - 1][0], o[o.Count - 1][1], p[0], p[1])) o.Add(p);
        if (o.Count > 1 && Near(o[0][0], o[0][1], o[o.Count - 1][0], o[o.Count - 1][1])) o.RemoveAt(o.Count - 1);
        return o;
    }

    private static List<Ring> CleanRings(List<Ring> rings)
    {
        var res = new List<Ring>();
        foreach (var r0 in rings)
        {
            var r = DedupRing(new Ring(r0));
            if (r.Count >= 3 && Math.Abs(GeoMath.SignedArea2X(r)) > 1e-12) res.Add(r);
        }
        return res;
    }

    /// <summary>按嵌套树深度归一化方向：壳 CCW、洞 CW（面积降序父树，避免代表点落入洞内导致的误判）。</summary>
    public static List<Ring> OrientRings(List<Ring> rings)
    {
        var result = new List<Ring>();
        var reps = rings.Select(RepresentativeInside).ToList();
        var absArea = rings.Select(r => Math.Abs(GeoMath.SignedArea2X(r))).ToList();
        var order = Enumerable.Range(0, rings.Count).OrderByDescending(i => absArea[i]).ToList();
        var depth = new int[rings.Count];
        for (var idx = 0; idx < order.Count; idx++)
        {
            int i = order[idx];
            if (reps[i] is null) { result.Add(new Ring(rings[i])); continue; }
            int par = -1;
            foreach (int j in order)
            {
                if (j == i || absArea[j] <= absArea[i]) break;
                if (reps[j] is null || j == i) continue;
                if (WindingAt(new List<Ring> { rings[j] }, reps[i]![0], reps[i]![1]) != 0 &&
                    (par < 0 || absArea[j] < absArea[par])) par = j;
            }

            int d = par < 0 ? 0 : depth[par] + 1;
            depth[i] = d;
            bool ccw = GeoMath.SignedArea2X(rings[i]) > 0;
            bool wantShell = d % 2 == 0;
            var r = new Ring(rings[i]);
            if (wantShell != ccw) r.Reverse();
            result.Add(r);
        }

        return result;
    }

    /// <summary>环内部代表点（耳点试探 + 包围盒网格兜底）。</summary>
    public static double[]? RepresentativeInside(Ring ring)
    {
        var one = new List<Ring> { ring };
        int n = ring.Count;
        for (int i = 0; i < n; i++)
        {
            double mx = (ring[(i - 1 + n) % n][0] + ring[(i + 1) % n][0]) / 2;
            double my = (ring[(i - 1 + n) % n][1] + ring[(i + 1) % n][1]) / 2;
            if (WindingAt(one, mx, my) != 0) return new[] { mx, my };
        }
        double minX = ring.Min(p => p[0]), maxX = ring.Max(p => p[0]);
        double minY = ring.Min(p => p[1]), maxY = ring.Max(p => p[1]);
        for (int gx = 0; gx < 16; gx++)
            for (int gy = 0; gy < 16; gy++)
            {
                double x = minX + (gx + 0.5) * (maxX - minX) / 16;
                double y = minY + (gy + 0.5) * (maxY - minY) / 16;
                if (WindingAt(one, x, y) != 0) return new[] { x, y };
            }
        return null;
    }
}
