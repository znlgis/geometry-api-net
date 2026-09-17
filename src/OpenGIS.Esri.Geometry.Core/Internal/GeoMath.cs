using System;
using System.Collections.Generic;

namespace OpenGIS.Esri.Geometry.Core.Internal;

using static OpenGIS.Esri.Geometry.Core.Internal.Nums;

/// <summary>2D 平面几何基础数学原语（谓词引擎/裁剪器共用）。</summary>
internal static class GeoMath
{
    public const double Eps = 1e-10;

    public static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;

    /// <summary>有向面积 ×2（鞋带公式，环不要求闭合重复首点）。</summary>
    public static double SignedArea2X(IReadOnlyList<double[]> ring)
    {
        double s = 0;
        int n = ring.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
            s += ring[j][0] * ring[i][1] - ring[i][0] * ring[j][1];
        return s;
    }

    /// <summary>射线法（奇偶规则）：点是否在环内部（不含边界）。</summary>
    public static bool PointInRing(double x, double y, IReadOnlyList<double[]> ring)
    {
        bool inside = false;
        int n = ring.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = ring[i][0], yi = ring[i][1], xj = ring[j][0], yj = ring[j][1];
            if ((yi > y) != (yj > y) &&
                x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>点是否落在线段上（含端点），带尺度相对容差。</summary>
    public static bool PointOnSegment(double px, double py, double ax, double ay, double bx, double by)
    {
        double dx = bx - ax, dy = by - ay;
        double len = Math.Sqrt(dx * dx + dy * dy);
        double tol = Eps * Math.Max(1.0, Math.Max(Math.Abs(px), Math.Max(Math.Abs(py), Math.Max(Math.Abs(ax), Math.Max(Math.Abs(ay), Math.Max(Math.Abs(bx), Math.Abs(by)))))));
        double cross = Math.Abs((px - ax) * dy - (py - ay) * dx);
        if (cross > tol * Math.Max(len, 1e-30)) return false;
        double dot = (px - ax) * (px - bx) + (py - ay) * (py - by);
        return dot <= tol * tol;
    }

    /// <summary>点-多边形分类：0=Out 1=On 2=In。rings 全部闭合环，奇偶规则。</summary>
    public static int ClassifyPointInRings(double x, double y, IReadOnlyList<List<double[]>> rings)
    {
        bool on = false;
        int crossings = 0;
        foreach (var ring in rings)
        {
            int n = ring.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = ring[i][0], yi = ring[i][1], xj = ring[j][0], yj = ring[j][1];
                if (PointOnSegment(x, y, xj, yj, xi, yi)) on = true;
                // 统一射线交叉计数（半开区间规则）
                if ((yi > y) != (yj > y))
                {
                    double xInt = (xj - xi) * (y - yi) / (yj - yi) + xi;
                    if (x < xInt) crossings ^= 1;
                }
            }
            if (on) return 1;
        }
        return crossings == 1 ? 2 : 0;
    }

    /// <summary>单环分类：0=Out 1=On 2=In。</summary>
    public static int ClassifyPointInRing(double x, double y, IReadOnlyList<double[]> ring)
    {
        int n = ring.Count;
        int crossings = 0;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = ring[i][0], yi = ring[i][1], xj = ring[j][0], yj = ring[j][1];
            if (PointOnSegment(x, y, xj, yj, xi, yi)) return 1;
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) crossings ^= 1;
        }
        return crossings == 1 ? 2 : 0;
    }

    /// <summary>
    /// 线段相交分类：返回交点参数 (tA,tB)。
    /// 输出 kind：0=不相交，1=规范相交（单点），2=共线重叠（重叠段 [t0,t1]×[u0,u1]）。
    /// </summary>
    public static int SegIntersect(
        double ax0, double ay0, double ax1, double ay1,
        double bx0, double by0, double bx1, double by1,
        out double ta, out double tb,
        out double ta0, out double ta1, out double tb0, out double tb1)
    {
        ta = tb = ta0 = ta1 = tb0 = tb1 = 0;
        double rx = ax1 - ax0, ry = ay1 - ay0;
        double sx = bx1 - bx0, sy = by1 - by0;
        double ql = Cross(rx, ry, sx, sy);
        double qx = bx0 - ax0, qy = by0 - ay0;

        double scale = Math.Max(Math.Max(Math.Abs(rx), Math.Abs(ry)), Math.Max(Math.Abs(sx), Math.Abs(sy)));
        scale = Math.Max(1.0, scale);
        double eps = Eps * scale;

        if (Math.Abs(ql) > eps)
        {
            double t = Cross(qx, qy, sx, sy) / ql;
            double u = Cross(qx, qy, rx, ry) / ql;
            // 闭区间允许端点接触（带容差）
            double pad = Eps * 10;
            if (t < -pad || t > 1 + pad || u < -pad || u > 1 + pad) return 0;
            ta = ClampD(t, 0, 1);
            tb = ClampD(u, 0, 1);
            return 1;
        }

        // 平行：共线？
        if (Math.Abs(Cross(qx, qy, rx, ry)) > eps * Math.Max(1.0, Math.Abs(rx) + Math.Abs(ry))) return 0;

        // 投影到更长轴
        double rr = rx * rx + ry * ry;
        if (rr < 1e-300) return 0; // A 退化
        double t0 = (qx * rx + qy * ry) / rr;
        double t1 = t0 + (sx * rx + sy * ry) / rr;
        double lo = Math.Min(t0, t1), hi = Math.Max(t0, t1);
        double overlapLo = Math.Max(0, lo), overlapHi = Math.Min(1, hi);
        if (overlapHi - overlapLo <= Eps * 4)
        {
            // 端点级接触
            if (overlapLo > 1 + Eps || overlapHi < -Eps) return 0;
            ta = ClampD(overlapLo, 0, 1);
            double ss = sx * sx + sy * sy;
            tb = ss < 1e-300 ? 0 : ClampD(((ax0 + ta * rx - bx0) * sx + (ay0 + ta * ry - by0) * sy) / ss, 0, 1);
            return 1;
        }
        ta0 = overlapLo; ta1 = overlapHi;
        double rr2 = sx * sx + sy * sy;
        if (rr2 < 1e-300) { tb0 = tb1 = 0; }
        else
        {
            tb0 = ((ax0 + ta0 * rx - bx0) * sx + (ay0 + ta0 * ry - by0) * sy) / rr2;
            tb1 = ((ax0 + ta1 * rx - bx0) * sx + (ay0 + ta1 * ry - by0) * sy) / rr2;
            if (tb0 > tb1) (tb0, tb1) = (tb1, tb0);
        }
        return 2;
    }

    public static double PtSegDist(double px, double py, double ax, double ay, double bx, double by)
    {
        double dx = bx - ax, dy = by - ay;
        double l2 = dx * dx + dy * dy;
        double t = l2 == 0 ? 0 : ClampD(((px - ax) * dx + (py - ay) * dy) / l2, 0, 1);
        double ex = ax + t * dx - px, ey = ay + t * dy - py;
        return Math.Sqrt(ex * ex + ey * ey);
    }

    public static double SegSegDist(
        double ax, double ay, double bx, double by,
        double cx, double cy, double dx, double dy)
    {
        if (PtSegDist(ax, ay, cx, cy, dx, dy) == 0 || PtSegDist(bx, by, cx, cy, dx, dy) == 0 ||
            PtSegDist(cx, cy, ax, ay, bx, by) == 0 || PtSegDist(dx, dy, ax, ay, bx, by) == 0)
            return 0;
        double d1 = LineLineDist(ax, ay, bx, by, cx, cy, dx, dy);
        return Math.Min(Math.Min(
            Math.Min(PtSegDist(ax, ay, cx, cy, dx, dy), PtSegDist(bx, by, cx, cy, dx, dy)),
            Math.Min(PtSegDist(cx, cy, ax, ay, bx, by), PtSegDist(dx, dy, ax, ay, bx, by))), d1);
    }

    /// <summary>两直线（非平行）间最短距离：沿公垂线；2D 中若无限直线相交则 0，否则平行距离。</summary>
    private static double LineLineDist(
        double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
    {
        double rx = bx - ax, ry = by - ay, sx = dx - cx, sy = dy - cy;
        double ql = Cross(rx, ry, sx, sy);
        if (Math.Abs(ql) <= Eps * Math.Max(1, Math.Abs(rx) + Math.Abs(sy)))
            return PtSegDist(ax, ay, cx, cy, dx, dy) > PtSegDist(cx, cy, ax, ay, bx, by)
                ? PtSegDist(cx, cy, ax, ay, bx, by) : PtSegDist(ax, ay, cx, cy, dx, dy);
        // 无限直线相交；段不相交时最近必在端点 → 端点距离已覆盖
        return double.MaxValue;
    }
}
