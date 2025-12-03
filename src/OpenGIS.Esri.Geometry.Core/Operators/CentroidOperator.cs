using System;
using System.Linq;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于计算几何对象质心（质量中心）的操作符.
/// </summary>
public class CentroidOperator : IGeometryOperator<Point>
{
    private const double EPSILON = 1e-10;
    private static readonly Lazy<CentroidOperator> _instance = new(() => new CentroidOperator());

    private CentroidOperator()
    {
    }

    /// <summary>
    ///     获取 CentroidOperator 的单例实例.
    /// </summary>
    public static CentroidOperator Instance => _instance.Value;

    /// <inheritdoc />
    public Point Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));

        if (geometry.IsEmpty) return new Point();

        // Point centroid is itself
        if (geometry is Point point) return new Point(point.X, point.Y);

        // MultiPoint centroid is average of all points
        if (geometry is MultiPoint multiPoint) return CalculateMultiPointCentroid(multiPoint);

        // Envelope centroid is its center
        if (geometry is Envelope envelope) return envelope.Center;

        // Line centroid is midpoint
        if (geometry is Line line)
            return new Point(
                (line.Start.X + line.End.X) / 2,
                (line.Start.Y + line.End.Y) / 2
            );

        // Polyline centroid is weighted average by segment length
        if (geometry is Polyline polyline) return CalculatePolylineCentroid(polyline);

        // Polygon centroid is area-weighted centroid
        if (geometry is Polygon polygon) return CalculatePolygonCentroid(polygon);

        throw new NotSupportedException($"Centroid calculation for {geometry.Type} is not yet implemented.");
    }

    private Point CalculateMultiPointCentroid(MultiPoint multiPoint)
    {
        var points = multiPoint.GetPoints().ToList();
        if (points.Count == 0) return new Point();

        double sumX = 0;
        double sumY = 0;
        foreach (var point in points)
        {
            sumX += point.X;
            sumY += point.Y;
        }

        return new Point(sumX / points.Count, sumY / points.Count);
    }

    private Point CalculatePolylineCentroid(Polyline polyline)
    {
        double totalLength = 0;
        double weightedX = 0;
        double weightedY = 0;

        foreach (var path in polyline.GetPaths())
            for (var i = 0; i < path.Count - 1; i++)
            {
                var p1 = path[i];
                var p2 = path[i + 1];
                var segmentLength = p1.Distance(p2);

                if (segmentLength > 0)
                {
                    var midX = (p1.X + p2.X) / 2;
                    var midY = (p1.Y + p2.Y) / 2;

                    weightedX += midX * segmentLength;
                    weightedY += midY * segmentLength;
                    totalLength += segmentLength;
                }
            }

        if (totalLength == 0) return new Point();

        return new Point(weightedX / totalLength, weightedY / totalLength);
    }

    private Point CalculatePolygonCentroid(Polygon polygon)
    {
        // Using the formula for polygon centroid based on vertices
        // This is a simplified implementation for the exterior ring only

        if (polygon.RingCount == 0) return new Point();

        var ring = polygon.GetRing(0);
        if (ring.Count < 3) return new Point();

        double area = 0;
        double cx = 0;
        double cy = 0;

        for (var i = 0; i < ring.Count - 1; i++)
        {
            var x0 = ring[i].X;
            var y0 = ring[i].Y;
            var x1 = ring[i + 1].X;
            var y1 = ring[i + 1].Y;

            var cross = x0 * y1 - x1 * y0;
            area += cross;
            cx += (x0 + x1) * cross;
            cy += (y0 + y1) * cross;
        }

        area /= 2.0;

        if (Math.Abs(area) < EPSILON)
        {
            // Degenerate polygon, return average of vertices
            double sumX = 0, sumY = 0;
            foreach (var p in ring)
            {
                sumX += p.X;
                sumY += p.Y;
            }

            return new Point(sumX / ring.Count, sumY / ring.Count);
        }

        cx /= 6.0 * area;
        cy /= 6.0 * area;

        return new Point(cx, cy);
    }
}