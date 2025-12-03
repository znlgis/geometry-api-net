using System;
using System.Collections.Generic;
using System.Linq;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Operator for simplifying geometries using the Douglas-Peucker algorithm.
    /// </summary>
    public class SimplifyOperator : IGeometryOperator<Geometries.Geometry>
    {
        private const double EPSILON = 1e-10;
        private static readonly Lazy<SimplifyOperator> _instance = new Lazy<SimplifyOperator>(() => new SimplifyOperator());

        /// <summary>
        /// Gets the singleton instance of the simplify operator.
        /// </summary>
        public static SimplifyOperator Instance => _instance.Value;

        private SimplifyOperator()
        {
        }

        /// <inheritdoc/>
        public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
        {
            throw new NotImplementedException("Simplify operator requires a tolerance parameter. Use Execute(geometry, tolerance, spatialRef) instead.");
        }

        /// <summary>
        /// Simplifies a geometry using the Douglas-Peucker algorithm.
        /// </summary>
        /// <param name="geometry">The geometry to simplify.</param>
        /// <param name="tolerance">The simplification tolerance.</param>
        /// <param name="spatialRef">Optional spatial reference.</param>
        /// <returns>The simplified geometry.</returns>
        public Geometries.Geometry Execute(Geometries.Geometry geometry, double tolerance, SpatialReference.SpatialReference? spatialRef = null)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException(nameof(geometry));
            }

            if (geometry.IsEmpty)
            {
                return geometry;
            }

            if (tolerance <= 0)
            {
                throw new ArgumentException("Tolerance must be positive.", nameof(tolerance));
            }

            // Points don't need simplification
            if (geometry is Point)
            {
                return geometry;
            }

            // Simplify polyline
            if (geometry is Polyline polyline)
            {
                return SimplifyPolyline(polyline, tolerance);
            }

            // Simplify polygon
            if (geometry is Polygon polygon)
            {
                return SimplifyPolygon(polygon, tolerance);
            }

            // For other types, return as-is
            return geometry;
        }

        private Polyline SimplifyPolyline(Polyline polyline, double tolerance)
        {
            var simplified = new Polyline();
            foreach (var path in polyline.GetPaths())
            {
                var simplifiedPath = DouglasPeucker(path.ToList(), tolerance);
                if (simplifiedPath.Count >= 2)
                {
                    simplified.AddPath(simplifiedPath);
                }
            }
            return simplified;
        }

        private Polygon SimplifyPolygon(Polygon polygon, double tolerance)
        {
            var simplified = new Polygon();
            foreach (var ring in polygon.GetRings())
            {
                var simplifiedRing = DouglasPeucker(ring.ToList(), tolerance);
                // Ensure ring has at least 3 points plus closing point
                if (simplifiedRing.Count >= 3)
                {
                    // Ensure the ring is closed
                    if (!simplifiedRing[0].Equals(simplifiedRing[simplifiedRing.Count - 1]))
                    {
                        simplifiedRing.Add(simplifiedRing[0]);
                    }
                    simplified.AddRing(simplifiedRing);
                }
            }
            return simplified;
        }

        /// <summary>
        /// Douglas-Peucker line simplification algorithm.
        /// </summary>
        private List<Point> DouglasPeucker(List<Point> points, double tolerance)
        {
            if (points.Count < 3)
            {
                return points;
            }

            // Find the point with maximum distance from the line segment
            double maxDistance = 0;
            int maxIndex = 0;
            int end = points.Count - 1;

            for (int i = 1; i < end; i++)
            {
                double distance = PerpendicularDistance(points[i], points[0], points[end]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }

            // If max distance is greater than tolerance, recursively simplify
            if (maxDistance > tolerance)
            {
                // Recursive call
                var leftSegment = DouglasPeucker(points.GetRange(0, maxIndex + 1), tolerance);
                var rightSegment = DouglasPeucker(points.GetRange(maxIndex, end - maxIndex + 1), tolerance);

                // Combine results (removing duplicate point at maxIndex)
                var result = new List<Point>(leftSegment);
                result.AddRange(rightSegment.Skip(1));
                return result;
            }
            else
            {
                // Return only the endpoints
                return new List<Point> { points[0], points[end] };
            }
        }

        /// <summary>
        /// Calculate perpendicular distance from a point to a line segment.
        /// </summary>
        private double PerpendicularDistance(Point point, Point lineStart, Point lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;

            // Normalize
            double mag = Math.Sqrt(dx * dx + dy * dy);
            if (mag < EPSILON)
            {
                return point.Distance(lineStart);
            }

            double u = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (mag * mag);

            if (u < 0)
            {
                return point.Distance(lineStart);
            }
            else if (u > 1)
            {
                return point.Distance(lineEnd);
            }
            else
            {
                double ix = lineStart.X + u * dx;
                double iy = lineStart.Y + u * dy;
                return point.Distance(new Point(ix, iy));
            }
        }
    }
}
