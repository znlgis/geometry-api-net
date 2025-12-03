using System;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于计算两个几何对象交集的操作符.
///     返回表示两个几何对象中所有点的几何对象.
///     Implements intersection for basic geometry types.
/// </summary>
public sealed class IntersectionOperator : IBinaryGeometryOperator<Geometries.Geometry>
{
    private static readonly Lazy<IntersectionOperator> _instance = new(() => new IntersectionOperator());

    private IntersectionOperator()
    {
    }

    /// <summary>
    ///     Gets the singleton instance of the IntersectionOperator.
    /// </summary>
    public static IntersectionOperator Instance => _instance.Value;

    /// <summary>
    ///     计算两个几何对象的交集.
    /// </summary>
    /// <param name="geometry1">First geometry</param>
    /// <param name="geometry2">Second geometry</param>
    /// <param name="spatialReference">Optional spatial reference (not used in this implementation)</param>
    /// <returns>Intersection of the two geometries, or empty geometry if they don't intersect</returns>
    public Geometries.Geometry Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialReference = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        // If either geometry is empty, return empty
        if (geometry1.IsEmpty || geometry2.IsEmpty)
            return new Point(double.NaN, double.NaN);

        // Point intersection with Point
        if (geometry1 is Point p1 && geometry2 is Point p2)
        {
            if (EqualsOperator.Instance.Execute(p1, p2))
                return p1;
            return new Point(double.NaN, double.NaN);
        }

        // Point intersection with Envelope
        if (geometry1 is Point point && geometry2 is Envelope envelope)
        {
            if (ContainsOperator.Instance.Execute(envelope, point))
                return point;
            return new Point(double.NaN, double.NaN);
        }

        if (geometry1 is Envelope envelope1 && geometry2 is Point point2)
        {
            if (ContainsOperator.Instance.Execute(envelope1, point2))
                return point2;
            return new Point(double.NaN, double.NaN);
        }

        // Point intersection with MultiPoint
        if (geometry1 is Point pt && geometry2 is MultiPoint mp)
        {
            foreach (var p in mp.GetPoints())
                if (EqualsOperator.Instance.Execute(pt, p))
                    return pt;
            return new Point(double.NaN, double.NaN);
        }

        if (geometry1 is MultiPoint mp1 && geometry2 is Point pt2)
        {
            foreach (var p in mp1.GetPoints())
                if (EqualsOperator.Instance.Execute(p, pt2))
                    return pt2;
            return new Point(double.NaN, double.NaN);
        }

        // Envelope intersection with Envelope
        if (geometry1 is Envelope env1 && geometry2 is Envelope env2)
        {
            var xMin = Math.Max(env1.XMin, env2.XMin);
            var yMin = Math.Max(env1.YMin, env2.YMin);
            var xMax = Math.Min(env1.XMax, env2.XMax);
            var yMax = Math.Min(env1.YMax, env2.YMax);

            if (xMin <= xMax && yMin <= yMax)
                return new Envelope(xMin, yMin, xMax, yMax);

            return new Point(double.NaN, double.NaN);
        }

        // MultiPoint intersection with MultiPoint
        if (geometry1 is MultiPoint multiPoint1 && geometry2 is MultiPoint multiPoint2)
        {
            var result = new MultiPoint();
            foreach (var p1_pt in multiPoint1.GetPoints())
            foreach (var p2_pt in multiPoint2.GetPoints())
                if (EqualsOperator.Instance.Execute(p1_pt, p2_pt))
                {
                    result.Add(p1_pt);
                    break;
                }

            if (result.Count > 0)
                return result;

            return new Point(double.NaN, double.NaN);
        }

        // MultiPoint intersection with Envelope
        if (geometry1 is MultiPoint multiPoint && geometry2 is Envelope env)
        {
            var result = new MultiPoint();
            foreach (var p in multiPoint.GetPoints())
                if (ContainsOperator.Instance.Execute(env, p))
                    result.Add(p);

            if (result.Count > 0)
                return result;

            return new Point(double.NaN, double.NaN);
        }

        if (geometry1 is Envelope env3 && geometry2 is MultiPoint multiPoint3)
        {
            var result = new MultiPoint();
            foreach (var p in multiPoint3.GetPoints())
                if (ContainsOperator.Instance.Execute(env3, p))
                    result.Add(p);

            if (result.Count > 0)
                return result;

            return new Point(double.NaN, double.NaN);
        }

        // For complex geometries, check if they intersect and return envelope intersection
        if (IntersectsOperator.Instance.Execute(geometry1, geometry2))
        {
            var env1_temp = geometry1.GetEnvelope();
            var env2_temp = geometry2.GetEnvelope();

            var xMin = Math.Max(env1_temp.XMin, env2_temp.XMin);
            var yMin = Math.Max(env1_temp.YMin, env2_temp.YMin);
            var xMax = Math.Min(env1_temp.XMax, env2_temp.XMax);
            var yMax = Math.Min(env1_temp.YMax, env2_temp.YMax);

            if (xMin <= xMax && yMin <= yMax)
                return new Envelope(xMin, yMin, xMax, yMax);
        }

        return new Point(double.NaN, double.NaN);
    }
}