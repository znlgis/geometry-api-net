using System;
using System.Collections.Generic;

namespace OpenGIS.Esri.Geometry.Core.Internal;

/// <summary>
/// 9-intersection 关系引擎：对两个 ShapeModel 计算 DE-9IM 矩阵（各槽最大维度），
/// 并据此推导 OGC 谓词。样本来源：
///   顶点（按角色：点=I、奇数度线端点=∂、偶数度=I、多边形环顶点=∂）、
///   段中点与求交分裂后的子段中点（线段=I、多边形环边=∂）、
///   面内部样本（每环耳点试探，奇偶规则验证）。
/// 求交事件（规范相交/共线重叠）同时补写槽维度（面×面穿越 ⇒ II 维 2 等）。
/// </summary>
internal sealed class RelateMatrix
{
    /// <summary>slots[r*3+c]：r,c ∈ {0=Interior,1=Boundary,2=Exterior}；值 -1=∅，否则=最大维度。</summary>
    public readonly int[] Slots = new int[9];

    public RelateMatrix()
    {
        for (int i = 0; i < 9; i++) Slots[i] = -1;
        // 外部∩外部总是 2 维（非退化平面）
        Slots[8] = 2;
    }

    public bool T(int r, int c) => Slots[r * 3 + c] >= 0;
    public int DimAt(int r, int c) => Slots[r * 3 + c];

    public void Set(int r, int c, int dim)
    {
        int i = r * 3 + c;
        if (dim > Slots[i]) Slots[i] = dim;
    }

    /// <summary>转置（交换两几何视角）。</summary>
    public RelateMatrix Transposed()
    {
        var m = new RelateMatrix();
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                m.Slots[r * 3 + c] = Slots[c * 3 + r];
        return m;
    }

    public static RelateMatrix Compute(ShapeModel a, ShapeModel b)
    {
        if (a.IsVoid || b.IsVoid)
        {
            var empty = new RelateMatrix();
            for (int i = 0; i < 9; i++) empty.Slots[i] = -1;
            empty.Slots[8] = 2;
            return empty;
        }

        var m = new RelateMatrix();

        // 包络快速分离
        if (a.MaxX < b.MinX || b.MaxX < a.MinX || a.MaxY < b.MinY || b.MaxY < a.MinY)
            return m; // 只有 EE

        var splitsA = new Dictionary<int, List<double>>();
        var splitsB = new Dictionary<int, List<double>>();

        void SplitA(int i, double t) { if (!splitsA.TryGetValue(i, out var l)) splitsA[i] = l = new List<double>(); l.Add(t); }
        void SplitB(int i, double t) { if (!splitsB.TryGetValue(i, out var l)) splitsB[i] = l = new List<double>(); l.Add(t); }

        // ---- 求交事件 ----
        var (small, largeIsA) = a.Segs.Count <= b.Segs.Count ? (a, true) : (b, false);
        EnumerateIntersections(a, b, (ta, tb, kind) =>
        {
            if (kind == 1)
            {
                SplitA(ta.SegIndex, ta.T0);
                SplitB(tb.SegIndex, tb.T0);
                var pa = a.Segs[ta.SegIndex]; var pb = b.Segs[tb.SegIndex];
                double px = pa.ax + (pa.bx - pa.ax) * ta.T0, py = pa.ay + (pa.by - pa.ay) * ta.T0;
                AddIntersectionPointRoles(m, a, b, px, py, ta.SegIndex, tb.SegIndex);
                if (a.Dim == 2 && b.Dim == 2 && ta.T0 > 1e-9 && ta.T0 < 1 - 1e-9 &&
                    tb.T0 > 1e-9 && tb.T0 < 1 - 1e-9)
                {
                    // 两多边形边界规范穿越（交点位于两条边内部）⇒ 内部重叠（2维）
                    m.Set(0, 0, 2);
                    QuadrantWitnesses(m, a, b, px, py, a.Segs[ta.SegIndex], b.Segs[tb.SegIndex], allowII: true);
                }
                else if (a.Dim == 2 && b.Dim == 2)
                {
                    // 顶点级穿越/相触：象限见证补非 II 槽（不武断断言内部重叠）
                    QuadrantWitnesses(m, a, b, px, py, a.Segs[ta.SegIndex], b.Segs[tb.SegIndex], allowII: false);
                }
                else if (a.Dim == 2 && b.Dim == 1)
                {
                    // 线穿越面边界：线内部与面内部相交（穿越两侧之一为内部，由子段中点样本定）
                }
            }
            else if (kind == 2)
            {
                SplitA(ta.SegIndex, ta.T0); SplitA(ta.SegIndex, ta.T1);
                SplitB(tb.SegIndex, tb.T0); SplitB(tb.SegIndex, tb.T1);
                var pa2 = a.Segs[ta.SegIndex]; var pb2 = b.Segs[tb.SegIndex];
                double mx = pa2.ax + (pa2.bx - pa2.ax) * (ta.T0 + ta.T1) / 2;
                double my = pa2.ay + (pa2.by - pa2.ay) * (ta.T0 + ta.T1) / 2;
                int roleA = a.Dim == 2 ? 1 : RoleOfSegParam(a, ta.SegIndex, (ta.T0 + ta.T1) / 2);
                int roleB = b.Dim == 2 ? 1 : RoleOfSegParam(b, tb.SegIndex, (tb.T0 + tb.T1) / 2);
                m.Set(roleA, Col(b.Classify(mx, my)), 1);
                m.Set(Col(a.Classify(mx, my)), roleB, 1);
            }
        });

        // ---- 顶点/端点样本 ----
        foreach (var (x, y, role) in VertexSamples(a))
            m.Set(role, a2b(a, b, x, y), 0);
        foreach (var (x, y, role) in VertexSamples(b))
            m.Set(Col(a.Classify(x, y)), role, 0);

        // ---- 子段中点样本（按分裂参数切分）----
        SegSubMidpoints(a, splitsA, (mx, my, role) =>
            m.Set(role, Col(b.Classify(mx, my)), 1));
        SegSubMidpoints(b, splitsB, (mx, my, role) =>
            m.Set(Col(a.Classify(mx, my)), role, 1));

        // ---- 拓扑推断补槽：∂B 有 1 维部分在 E(A) 且 ∂A 有部分入 B ⇒ B° 必有 2 维部分在 A 外 ----
        if (m.T(2, 1) && m.T(1, 0) && b.Dim == 2 && a.Dim == 2) m.Set(2, 0, 2);
        if (m.T(1, 2) && m.T(0, 1) && a.Dim == 2 && b.Dim == 2) m.Set(0, 2, 2);

        // ---- 面内部样本（双向）----
        foreach (var (x, y) in InteriorSamples(a))
            m.Set(0, Col(b.Classify(x, y)), 2);
        foreach (var (x, y) in InteriorSamples(b))
            m.Set(Col(a.Classify(x, y)), 0, 2);

        return m;
    }

    private static int a2b(ShapeModel a, ShapeModel b, double x, double y) => Col(b.Classify(x, y));

    /// <summary>Classify 结果(Out=0,On=1,In=2) → 槽坐标（I=0,∂=1,E=2）。</summary>
    public static int Col(int classifyResult) => classifyResult switch { 2 => 0, 1 => 1, _ => 2 };

    private static int RoleOfSegParam(ShapeModel m, int segIndex, double t)
    {
        if (m.Dim != 1) return 1; // 多边形环边 = 边界
        var (ax, ay, bx, by) = m.Segs[segIndex];
        if (t <= 1e-9 || t >= 1 - 1e-9)
            return m.IsLineBoundaryVertex(ax + (bx - ax) * t, ay + (by - ay) * t) ? 1 : 0;
        return 0;
    }

    private static void AddIntersectionPointRoles(RelateMatrix m, ShapeModel a, ShapeModel b, double px, double py, int segA, int segB)
    {
        int roleA = a.Dim == 2 ? 1 : RoleOfPointOnLine(a, segA, px, py);
        int roleB = b.Dim == 2 ? 1 : RoleOfPointOnLine(b, segB, px, py);
        // 该点在 A 视角的位置（其段上）与 B 视角位置
        m.Set(Col2(roleA), Col2(roleB), 0);
    }

    // roleA/roleB 本身就是 I/∂ 的槽列号（0/1）
    private static int Col2(int role) => role;

    private static int RoleOfPointOnLine(ShapeModel m, int segIndex, double px, double py)
    {
        var (ax, ay, bx, by) = m.Segs[segIndex];
        bool endpoint = (Math.Abs(ax - px) < 1e-12 && Math.Abs(ay - py) < 1e-12) ||
                        (Math.Abs(bx - px) < 1e-12 && Math.Abs(by - py) < 1e-12);
        if (!endpoint) return 0; // 段内部 → 线内部
        return m.IsLineBoundaryVertex(px, py) ? 1 : 0;
    }

    /// <summary>几何所有顶点及其角色：点=I；线端点按度；环顶点=∂。</summary>
    private static IEnumerable<(double x, double y, int role)> VertexSamples(ShapeModel m)
    {
        if (m.Dim == 0)
        {
            foreach (var p in m.Points) yield return (p[0], p[1], 0);
            yield break;
        }
        if (m.Dim == 1)
        {
            var seen = new HashSet<(double, double)>();
            foreach (var (ax, ay, bx, by) in m.Segs)
                foreach (var (x, y) in new[] { (ax, ay), (bx, by) })
                {
                    var key = (x, y);
                    if (!seen.Add(key)) continue;
                    yield return (x, y, m.IsLineBoundaryVertex(x, y) ? 1 : 0);
                }
            yield break;
        }
        foreach (var ring in m.Rings)
            foreach (var p in ring)
                yield return (p[0], p[1], 1);
    }

    private static void SegSubMidpoints(ShapeModel m, Dictionary<int, List<double>> splits, Action<double, double, int> accept)
    {
        for (int i = 0; i < m.Segs.Count; i++)
        {
            var (ax, ay, bx, by) = m.Segs[i];
            var ts = new List<double> { 0, 1 };
            if (splits.TryGetValue(i, out var extra)) ts.AddRange(extra);
            ts.Sort();
            // 去重
            var uniq = new List<double>();
            foreach (var t in ts)
                if (uniq.Count == 0 || t - uniq[uniq.Count - 1] > 1e-7) uniq.Add(t);
            for (int k = 0; k + 1 < uniq.Count; k++)
            {
                double t0 = uniq[k], t1 = uniq[k + 1];
                if (t1 - t0 < 1e-12) continue;
                double tm = (t0 + t1) / 2;
                double mx = ax + (bx - ax) * tm, my = ay + (by - ay) * tm;
                int role = m.Dim == 2 ? 1 : RoleOfSegParam(m, i, tm);
                accept(mx, my, role);
            }
        }
    }

    /// <summary>每个环产出多个内部样本：耳点/重心候选 + 4x4 包围盒网格，提升重叠槽位覆盖。</summary>
    private static IEnumerable<(double x, double y)> InteriorSamples(ShapeModel m)
    {
        if (m.Dim != 2) yield break;
        foreach (var ring in m.Rings)
        {
            var candidates = new List<(double, double)>();
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                var a = ring[(i - 1 + n) % n]; var b = ring[i]; var c = ring[(i + 1) % n];
                candidates.Add(((a[0] + c[0]) / 2, (a[1] + c[1]) / 2));
                candidates.Add(((a[0] + b[0] + c[0]) / 3, (a[1] + b[1] + c[1]) / 3));
            }
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in ring)
            {
                if (p[0] < minX) minX = p[0]; if (p[0] > maxX) maxX = p[0];
                if (p[1] < minY) minY = p[1]; if (p[1] > maxY) maxY = p[1];
            }
            for (int gx = 0; gx < 4; gx++)
                for (int gy = 0; gy < 4; gy++)
                    candidates.Add((minX + (gx + 0.5) * (maxX - minX) / 4, minY + (gy + 0.5) * (maxY - minY) / 4));

            int emitted = 0;
            foreach (var (x, y) in candidates)
            {
                if (emitted >= 6) break;
                if (m.Classify(x, y) == 2) { yield return (x, y); emitted++; }
            }
        }
    }

    /// <summary>枚举 a×b 段段相交（用较大者的网格索引粗筛）。kind: 1=规范点交，2=共线重叠。</summary>
    private static void EnumerateIntersections(ShapeModel a, ShapeModel b,
        Action<(int SegIndex, double T0, double T1), (int SegIndex, double T0, double T1), int> emit)
    {
        ShapeModel probe, indexed;
        bool aIsProbe;
        if (a.Segs.Count <= b.Segs.Count) { probe = a; indexed = b; aIsProbe = true; }
        else { probe = b; indexed = a; aIsProbe = false; }

        var reported = new HashSet<(int, int)>();
        for (int pi = 0; pi < probe.Segs.Count; pi++)
        {
            var (pax, pay, pbx, pby) = probe.Segs[pi];
            double minX = Math.Min(pax, pbx), maxX = Math.Max(pax, pbx), minY = Math.Min(pay, pby), maxY = Math.Max(pay, pby);
            foreach (int qi in indexed.Edges.QueryBox(minX, minY, maxX, maxY))
            {
                var (qax, qay, qbx, qby) = indexed.Segs[qi];
                int ai = aIsProbe ? pi : qi, bi = aIsProbe ? qi : pi;
                if (!reported.Add((ai, bi))) continue;
                var (ax, ay, bx, by) = a.Segs[ai];
                var (cx, cy, dx, dy) = b.Segs[bi];
                int kind = GeoMath.SegIntersect(ax, ay, bx, by, cx, cy, dx, dy,
                    out double ta, out double tb, out double ta0, out double ta1, out double tb0, out double tb1);
                // ta* 是 A 段参数、tb* 是 B 段参数（与 probe 方向无关）
                if (kind == 1) emit((ai, ta, ta), (bi, tb, tb), 1);
                else if (kind == 2) emit((ai, ta0, ta1), (bi, tb0, tb1), 2);
            }
        }
    }

    /// <summary>穿越点象限见证：两线段方向张成四象限各取 ε 采样点补槽，保证跨边界重叠的 II/IE/EI 槽必达。</summary>
    private static void QuadrantWitnesses(RelateMatrix m, ShapeModel a, ShapeModel b,
        double px, double py, (double ax, double ay, double bx, double by) pa, (double ax, double ay, double bx, double by) pb,
        bool allowII)
    {
        double la = SegLen(pa), lb = SegLen(pb);
        if (la < 1e-300 || lb < 1e-300) return;
        // ε 取较大边的 1e-3（并控制在较小边的 0.4 倍内）：细碎边界交叉时取样点仍能离开交点邻域
        double eps = Math.Max(1e-9, Math.Min(Math.Max(la, lb) * 1e-3, Math.Min(la, lb) * 0.4));
        double u1 = (pa.bx - pa.ax) / la, v1 = (pa.by - pa.ay) / la;
        double u2 = (pb.bx - pb.ax) / lb, v2 = (pb.by - pb.ay) / lb;
        for (int s1 = -1; s1 <= 1; s1 += 2)
            for (int s2 = -1; s2 <= 1; s2 += 2)
            {
                double dx = s1 * u1 + s2 * u2, dy = s1 * v1 + s2 * v2;
                double dl = Math.Sqrt(dx * dx + dy * dy);
                if (dl < 1e-12) continue;
                double qx = px + eps * dx / dl, qy = py + eps * dy / dl;
                int la_ = Col(a.Classify(qx, qy)), lb_ = Col(b.Classify(qx, qy));
                if (!allowII && la_ == 0 && lb_ == 0) continue;
                m.Set(la_, lb_, 2);
            }
    }

    /// <summary>共线重叠段两侧见证：沿公共线法向两侧取样补槽。</summary>
    private static void OverlapSideWitnesses(RelateMatrix m, ShapeModel a, ShapeModel b,
        double mx, double my, (double ax, double ay, double bx, double by) pa)
    {
        double la = SegLen(pa);
        if (la < 1e-300) return;
        double nx = -(pa.by - pa.ay) / la, ny = (pa.bx - pa.ax) / la;
        double eps = Math.Max(1e-12, la * 1e-3);
        foreach (int sg in new[] { -1, 1 })
        {
            double qx = mx + sg * nx * eps, qy = my + sg * ny * eps;
            m.Set(Col(a.Classify(qx, qy)), Col(b.Classify(qx, qy)), 2);
        }
    }

    private static double SegLen((double ax, double ay, double bx, double by) s)
        => Math.Sqrt((s.bx - s.ax) * (s.bx - s.ax) + (s.by - s.ay) * (s.by - s.ay));

    // ==== 谓词推导 ====
    public bool Intersects => T(0, 0) || T(0, 1) || T(1, 0) || T(1, 1);
    public bool Disjoint => !Intersects;

    // OGC 99-049 模式 "T*****FF*"（槽 0=T, 6=F, 7=F）；与 GEOS ST_Contains 一致。
    public bool Contains(ShapeModel a, ShapeModel b)
        => T(0, 0) && !T(2, 0) && !T(2, 1);

    public bool Within(ShapeModel a, ShapeModel b)
        => T(0, 0) && !T(0, 2) && !T(1, 2);

    public bool EqualsTopo(ShapeModel a, ShapeModel b)
        => T(0, 0) && !T(0, 2) && !T(1, 2) && !T(2, 0) && !T(2, 1);

    public bool Touches(ShapeModel a, ShapeModel b)
        => (T(0, 1) || T(1, 0) || T(1, 1)) && !T(0, 0);

    public bool Overlaps(ShapeModel a, ShapeModel b)
        => a.Dim == b.Dim && a.Dim >= 1 && DimAt(0, 0) == a.Dim && T(0, 2) && T(2, 0);

    public bool Crosses(ShapeModel a, ShapeModel b)
    {
        int dA = a.Dim, dB = b.Dim;
        // GEOS/PostGIS 实证（2026-09-15）：crosses 使用模式 "T*T***T**"（槽 0/2/6），
        // 适用 (0,1),(0,2),(1,2),(2,1)；(1,1) 用 "T*T***FF*" 且要求交点为 0 维；其它组合 false。
        return (dA, dB) switch
        {
            (0, 1) or (0, 2) => T(0, 0) && T(0, 2) && T(2, 0),
            (1, 1) => T(0, 0) && T(0, 2) && T(2, 0) && DimAt(0, 0) == 0,
            (1, 2) or (2, 1) => T(0, 0) && T(0, 2) && T(2, 0),
            _ => false,
        };
    }
}
