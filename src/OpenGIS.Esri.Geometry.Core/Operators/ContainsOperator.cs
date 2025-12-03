using System;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于测试一个几何对象是否包含另一个几何对象的操作符。
/// </summary>
public class ContainsOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<ContainsOperator> _instance = new(() => new ContainsOperator());

    private ContainsOperator()
    {
    }

    /// <summary>
    ///     获取 ContainsOperator 的单例实例。
    /// </summary>
    public static ContainsOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        // Simple implementation for envelope-point containment
        if (geometry1 is Envelope env && geometry2 is Point p) return env.Contains(p);

        // Point in Polygon test using ray casting algorithm
        if (geometry1 is Polygon poly && geometry2 is Point pt) return IsPointInPolygon(poly, pt);

        // For other geometry types, this would require more complex implementations
        throw new NotImplementedException(
            $"Contains test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
    }

    /// <summary>
    ///     使用光线投射算法测试点是否在多边形内部。
    /// </summary>
    private static bool IsPointInPolygon(Polygon polygon, Point point)
    {
        if (polygon.IsEmpty || polygon.RingCount == 0)
            return false;

        var inside = false;
        var x = point.X;
        var y = point.Y;

        // Test the first ring (exterior ring)
        var ring = polygon.GetRing(0);
        if (ring.Count < 3)
            return false;

        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            double xi = ring[i].X, yi = ring[i].Y;
            double xj = ring[j].X, yj = ring[j].Y;

            if (yi > y != yj > y &&
                x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                inside = !inside;
        }

        // If we have holes (subsequent rings), check if point is in any hole
        for (var ringIndex = 1; ringIndex < polygon.RingCount; ringIndex++)
        {
            var holeRing = polygon.GetRing(ringIndex);
            if (holeRing.Count < 3)
                continue;

            var inHole = false;
            for (int i = 0, j = holeRing.Count - 1; i < holeRing.Count; j = i++)
            {
                double xi = holeRing[i].X, yi = holeRing[i].Y;
                double xj = holeRing[j].X, yj = holeRing[j].Y;

                if (yi > y != yj > y &&
                    x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                    inHole = !inHole;
            }

            // If point is in a hole, it's not in the polygon
            if (inHole)
                return false;
        }

        return inside;
    }
}