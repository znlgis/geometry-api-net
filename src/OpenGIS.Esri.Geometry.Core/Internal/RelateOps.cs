using System;
using System.Collections.Generic;

namespace OpenGIS.Esri.Geometry.Core.Internal
{
using OpenGIS.Esri.Geometry.Core.Geometries;
using Geometry = OpenGIS.Esri.Geometry.Core.Geometries.Geometry;



/// <summary>谓词/距离统一入口：基于 ShapeModel + RelateMatrix（DE-9IM）。</summary>
internal static class RelateOps
{
    public static bool IntersectsGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return false;
        return Matrix(a, b).Intersects;
    }

    public static bool DisjointGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return true;
        return Matrix(a, b).Disjoint;
    }

    public static bool ContainsGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return false;
        var ma = ShapeModel.Of(a); var mb = ShapeModel.Of(b);
        return RelateMatrix.Compute(ma, mb).Contains(ma, mb);
    }

    public static bool WithinGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return false;
        var ma = ShapeModel.Of(a); var mb = ShapeModel.Of(b);
        return RelateMatrix.Compute(ma, mb).Within(ma, mb);
    }

    public static bool CrossesGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return false;
        var ma = ShapeModel.Of(a); var mb = ShapeModel.Of(b);
        return RelateMatrix.Compute(ma, mb).Crosses(ma, mb);
    }

    public static bool TouchesGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return false;
        var ma = ShapeModel.Of(a); var mb = ShapeModel.Of(b);
        return RelateMatrix.Compute(ma, mb).Touches(ma, mb);
    }

    public static bool OverlapsGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return false;
        var ma = ShapeModel.Of(a); var mb = ShapeModel.Of(b);
        return RelateMatrix.Compute(ma, mb).Overlaps(ma, mb);
    }

    public static bool EqualsGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty && b.IsEmpty) return true;
        if (a.IsEmpty || b.IsEmpty) return false;
        var ma = ShapeModel.Of(a); var mb = ShapeModel.Of(b);
        return RelateMatrix.Compute(ma, mb).EqualsTopo(ma, mb);
    }

    public static RelateMatrix Matrix(Geometry a, Geometry b)
        => RelateMatrix.Compute(ShapeModel.Of(a), ShapeModel.Of(b));

    /// <summary>一次关系计算派生全部谓词（避免 9 次重复矩阵计算）。</summary>
    public static RelateMatrix RelateOnce(Geometry a, Geometry b, out ShapeModel ma, out ShapeModel mb)
    {
        ma = ShapeModel.Of(a);
        mb = ShapeModel.Of(b);
        return RelateMatrix.Compute(ma, mb);
    }

    /// <summary>平面最小距离：先经关系矩阵判定相交即 0，否则在点/段特征上取最小值。</summary>
    public static double DistanceGeom(Geometry a, Geometry b)
    {
        if (a.IsEmpty || b.IsEmpty) return double.NaN;

        var ma = ShapeModel.Of(a);
        var mb = ShapeModel.Of(b);
        if (RelateMatrix.Compute(ma, mb).Intersects) return 0;

        double best = double.MaxValue;
        var va = Features(ma);
        var vb = Features(mb);

        foreach (var fa in va)
            foreach (var fb in vb)
                best = Math.Min(best, FeatureDistance(fa, fb));
        return best == double.MaxValue ? 0 : best;
    }

    private readonly struct Feat
    {
        public readonly double Ax, Ay, Bx, By;
        public readonly bool IsSeg;
        public Feat(double x, double y) { Ax = x; Ay = y; Bx = x; By = y; IsSeg = false; }
        public Feat(double ax, double ay, double bx, double by) { Ax = ax; Ay = ay; Bx = bx; By = by; IsSeg = true; }
    }

    private static List<Feat> Features(ShapeModel m)
    {
        var list = new List<Feat>();
        if (m.Dim == 0)
        {
            foreach (var p in m.Points) list.Add(new Feat(p[0], p[1]));
            return list;
        }
        foreach (var (ax, ay, bx, by) in m.Segs) list.Add(new Feat(ax, ay, bx, by));
        if (m.Dim == 2)
        {
            // 多边形之间还需考虑“内部到内部”——若不相交则最近必在边界/顶点，环边已覆盖；
            // 但一个多边形完全包含另一个不相交情形已由 Intersects=0 排除。
        }
        else
        {
            foreach (var (ax, ay, _, _) in m.Segs) { }
        }
        // 孤立点/端点
        if (m.Dim == 1)
            foreach (var line in m.Lines)
                foreach (var p in line)
                    if (!ContainsSegEndpoint(list, p[0], p[1])) list.Add(new Feat(p[0], p[1]));
        return list;
    }

    private static bool ContainsSegEndpoint(List<Feat> list, double x, double y)
    {
        foreach (var f in list)
            if (f.IsSeg && ((f.Ax == x && f.Ay == y) || (f.Bx == x && f.By == y))) return true;
        return false;
    }

    private static double FeatureDistance(Feat a, Feat b)
    {
        if (!a.IsSeg && !b.IsSeg)
        {
            double dx = a.Ax - b.Ax, dy = a.Ay - b.Ay;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        if (a.IsSeg || b.IsSeg)
            return GeoMath.SegSegDist(a.Ax, a.Ay, a.Bx, a.By, b.Ax, b.Ay, b.Bx, b.By);
        return 0;
    }
}
}
