using System;
using System.Collections.Generic;
using System.Linq;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Operator for computing the convex hull of a geometry.
    /// </summary>
    public class ConvexHullOperator : IGeometryOperator<Geometries.Geometry>
    {
        private static readonly Lazy<ConvexHullOperator> _instance = new Lazy<ConvexHullOperator>(() => new ConvexHullOperator());

        /// <summary>
        /// Gets the singleton instance of the convex hull operator.
        /// </summary>
        public static ConvexHullOperator Instance => _instance.Value;

        private ConvexHullOperator()
        {
        }

        /// <inheritdoc/>
        public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException(nameof(geometry));
            }

            if (geometry.IsEmpty)
            {
                return new Polygon();
            }

            var points = ExtractPoints(geometry);
            if (points.Count == 0)
            {
                return new Polygon();
            }

            if (points.Count == 1)
            {
                return points[0];
            }

            if (points.Count == 2)
            {
                return new Line(points[0], points[1]);
            }

            // Compute convex hull using Graham scan algorithm
            var hull = GrahamScan(points);
            
            if (hull.Count < 3)
            {
                if (hull.Count == 1)
                    return hull[0];
                if (hull.Count == 2)
                    return new Line(hull[0], hull[1]);
                return new Polygon();
            }

            var polygon = new Polygon();
            // Ensure the ring is closed
            var ring = new List<Point>(hull);
            if (!ring[0].Equals(ring[ring.Count - 1]))
            {
                ring.Add(ring[0]);
            }
            polygon.AddRing(ring);
            return polygon;
        }

        private List<Point> ExtractPoints(Geometries.Geometry geometry)
        {
            var points = new List<Point>();

            if (geometry is Point point && !point.IsEmpty)
            {
                points.Add(point);
            }
            else if (geometry is MultiPoint multiPoint)
            {
                points.AddRange(multiPoint.GetPoints());
            }
            else if (geometry is Envelope envelope && !envelope.IsEmpty)
            {
                points.Add(new Point(envelope.XMin, envelope.YMin));
                points.Add(new Point(envelope.XMax, envelope.YMin));
                points.Add(new Point(envelope.XMax, envelope.YMax));
                points.Add(new Point(envelope.XMin, envelope.YMax));
            }
            else if (geometry is Polyline polyline)
            {
                foreach (var path in polyline.GetPaths())
                {
                    points.AddRange(path);
                }
            }
            else if (geometry is Polygon polygon)
            {
                foreach (var ring in polygon.GetRings())
                {
                    points.AddRange(ring);
                }
            }

            return points;
        }

        private List<Point> GrahamScan(List<Point> points)
        {
            if (points.Count < 3)
            {
                return points;
            }

            // Find the point with the lowest Y coordinate (and lowest X if tie)
            var lowestPoint = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();

            // Sort points by polar angle with respect to the lowest point
            var sortedPoints = points
                .Where(p => p != lowestPoint)
                .OrderBy(p => Math.Atan2(p.Y - lowestPoint.Y, p.X - lowestPoint.X))
                .ThenBy(p => p.Distance(lowestPoint))
                .ToList();

            // Build the convex hull
            var hull = new List<Point> { lowestPoint };

            foreach (var point in sortedPoints)
            {
                while (hull.Count > 1 && !IsLeftTurn(hull[hull.Count - 2], hull[hull.Count - 1], point))
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }

            return hull;
        }

        private bool IsLeftTurn(Point p1, Point p2, Point p3)
        {
            // Cross product to determine if we make a left turn
            double cross = (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
            return cross > 0;
        }
    }
}
