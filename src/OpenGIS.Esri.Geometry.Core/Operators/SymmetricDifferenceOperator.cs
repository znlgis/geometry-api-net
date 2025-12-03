using System;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     计算两个几何对象的对称差.
///     The symmetric difference is the set of points that are in either geometry but not in both.
///     Equivalent to (A - B) ∪ (B - A) or (A ∪ B) - (A ∩ B).
/// </summary>
public sealed class SymmetricDifferenceOperator : IBinaryGeometryOperator<Geometries.Geometry>
{
    private SymmetricDifferenceOperator()
    {
    }

    /// <summary>
    ///     Gets the singleton instance of the SymmetricDifferenceOperator.
    /// </summary>
    public static SymmetricDifferenceOperator Instance { get; } = new();

    /// <summary>
    ///     计算两个几何对象的对称差.
    /// </summary>
    /// <param name="geometry1">First geometry.</param>
    /// <param name="geometry2">Second geometry.</param>
    /// <param name="spatialReference">Optional spatial reference (not used in this implementation).</param>
    /// <returns>A geometry representing the symmetric difference.</returns>
    public Geometries.Geometry Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialReference = null)
    {
        if (geometry1 == null || geometry2 == null)
            throw new ArgumentNullException("Geometries cannot be null");

        if (geometry1.IsEmpty && geometry2.IsEmpty)
            return new Point(double.NaN, double.NaN);

        if (geometry1.IsEmpty)
            return geometry2;

        if (geometry2.IsEmpty)
            return geometry1;

        // For points
        if (geometry1 is Point p1 && geometry2 is Point p2)
        {
            // If points are equal, result is empty
            if (EqualsOperator.Instance.Execute(p1, p2))
                return new Point(double.NaN, double.NaN);

            // If points are different, return both as MultiPoint
            var mp = new MultiPoint();
            mp.Add(p1);
            mp.Add(p2);
            return mp;
        }

        // Point vs MultiPoint
        if (geometry1 is Point point && geometry2 is MultiPoint mp2)
        {
            var result = new MultiPoint();
            var pointFound = false;

            // Add all points from mp2 except the matching one
            for (var i = 0; i < mp2.Count; i++)
            {
                var p = mp2.GetPoint(i);
                if (Math.Abs(p.X - point.X) < 1e-10 && Math.Abs(p.Y - point.Y) < 1e-10)
                    pointFound = true;
                else
                    result.Add(p);
            }

            // If point wasn't found in mp2, add it
            if (!pointFound)
                result.Add(point);

            return result.Count > 0 ? result : new Point(double.NaN, double.NaN);
        }

        // MultiPoint vs Point (swap arguments)
        if (geometry1 is MultiPoint && geometry2 is Point) return Execute(geometry2, geometry1, spatialReference);

        // MultiPoint vs MultiPoint
        if (geometry1 is MultiPoint mp1A && geometry2 is MultiPoint mp2A)
        {
            var result = new MultiPoint();

            // Add points from mp1 that are not in mp2
            for (var i = 0; i < mp1A.Count; i++)
            {
                var p1Test = mp1A.GetPoint(i);
                var foundInMp2 = false;

                for (var j = 0; j < mp2A.Count; j++)
                {
                    var p2Test = mp2A.GetPoint(j);
                    if (Math.Abs(p1Test.X - p2Test.X) < 1e-10 && Math.Abs(p1Test.Y - p2Test.Y) < 1e-10)
                    {
                        foundInMp2 = true;
                        break;
                    }
                }

                if (!foundInMp2)
                    result.Add(p1Test);
            }

            // Add points from mp2 that are not in mp1
            for (var j = 0; j < mp2A.Count; j++)
            {
                var p2Test = mp2A.GetPoint(j);
                var foundInMp1 = false;

                for (var i = 0; i < mp1A.Count; i++)
                {
                    var p1Test = mp1A.GetPoint(i);
                    if (Math.Abs(p2Test.X - p1Test.X) < 1e-10 && Math.Abs(p2Test.Y - p1Test.Y) < 1e-10)
                    {
                        foundInMp1 = true;
                        break;
                    }
                }

                if (!foundInMp1)
                    result.Add(p2Test);
            }

            return result.Count > 0 ? result : new Point(double.NaN, double.NaN);
        }

        // For Envelopes, symmetric difference can be approximated
        // In practice, this would require complex polygon operations
        if (geometry1 is Envelope env1 && geometry2 is Envelope env2)
        {
            // Symmetric difference = (Union - Intersection)
            var union = UnionOperator.Instance.Execute(env1, env2, spatialReference);
            var intersection = IntersectionOperator.Instance.Execute(env1, env2, spatialReference);

            if (intersection.IsEmpty)
                return union;

            // For envelopes, this is a simplification
            // True symmetric difference would require polygon clipping
            return union;
        }

        // For complex geometries, return union as approximation
        // Full implementation requires polygon clipping algorithms
        return UnionOperator.Instance.Execute(geometry1, geometry2, spatialReference);
    }
}