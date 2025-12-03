using System;
using System.Collections.Generic;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于计算几何对象边界的操作符.
///     边界根据 OGC Simple Features 规范定义.
/// </summary>
public class BoundaryOperator : IGeometryOperator<Geometries.Geometry>
{
    private static readonly Lazy<BoundaryOperator> _instance = new(() => new BoundaryOperator());

    private BoundaryOperator()
    {
    }

    /// <summary>
    ///     获取 BoundaryOperator 的单例实例.
    /// </summary>
    public static BoundaryOperator Instance => _instance.Value;

    /// <inheritdoc />
    public Geometries.Geometry Execute(Geometries.Geometry geometry,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));

        if (geometry.IsEmpty) return new MultiPoint(); // Empty geometry has empty boundary

        // Point has no boundary
        if (geometry is Point) return new MultiPoint();

        // MultiPoint has no boundary
        if (geometry is MultiPoint) return new MultiPoint();

        // Line boundary is its endpoints
        if (geometry is Line line)
        {
            var boundary = new MultiPoint();
            boundary.Add(new Point(line.Start.X, line.Start.Y));
            if (!line.Start.Equals(line.End)) boundary.Add(new Point(line.End.X, line.End.Y));
            return boundary;
        }

        // Polyline boundary is its endpoints (non-closed paths)
        if (geometry is Polyline polyline) return CalculatePolylineBoundary(polyline);

        // Polygon boundary is its rings (as polyline)
        if (geometry is Polygon polygon) return CalculatePolygonBoundary(polygon);

        // Envelope boundary is its perimeter
        if (geometry is Envelope envelope)
        {
            var boundaryPolyline = new Polyline();
            var path = new List<Point>
            {
                new(envelope.XMin, envelope.YMin),
                new(envelope.XMax, envelope.YMin),
                new(envelope.XMax, envelope.YMax),
                new(envelope.XMin, envelope.YMax),
                new(envelope.XMin, envelope.YMin)
            };
            boundaryPolyline.AddPath(path);
            return boundaryPolyline;
        }

        throw new NotSupportedException($"Boundary calculation for {geometry.Type} is not yet implemented.");
    }

    private Geometries.Geometry CalculatePolylineBoundary(Polyline polyline)
    {
        var endpoints = new MultiPoint();

        foreach (var path in polyline.GetPaths())
        {
            if (path.Count < 2)
                continue;

            var firstPoint = path[0];
            var lastPoint = path[path.Count - 1];

            // If the path is not closed, add both endpoints
            if (!firstPoint.Equals(lastPoint))
            {
                endpoints.Add(new Point(firstPoint.X, firstPoint.Y));
                endpoints.Add(new Point(lastPoint.X, lastPoint.Y));
            }
            // If the path is closed, it has no boundary points
        }

        return endpoints;
    }

    private Geometries.Geometry CalculatePolygonBoundary(Polygon polygon)
    {
        // The boundary of a polygon is the collection of its rings
        var boundary = new Polyline();

        foreach (var ring in polygon.GetRings())
            if (ring.Count >= 3)
            {
                var path = new List<Point>();
                foreach (var point in ring) path.Add(new Point(point.X, point.Y));

                // Ensure the path is closed
                if (!path[0].Equals(path[path.Count - 1])) path.Add(new Point(path[0].X, path[0].Y));

                boundary.AddPath(path);
            }

        return boundary;
    }
}