using System;
using System.Collections.Generic;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Operator for finding the nearest coordinates and vertices on geometries.
    /// Provides 2D proximity operations to find points closest to a given input point.
    /// </summary>
    public class Proximity2DOperator
    {
        private static readonly Lazy<Proximity2DOperator> _instance =
            new Lazy<Proximity2DOperator>(() => new Proximity2DOperator());

        /// <summary>
        /// Gets the singleton instance of the Proximity2DOperator.
        /// </summary>
        public static Proximity2DOperator Instance => _instance.Value;

        private Proximity2DOperator() { }

        /// <summary>
        /// Returns the nearest coordinate on the geometry to the given input point.
        /// </summary>
        /// <param name="geometry">The input geometry.</param>
        /// <param name="inputPoint">The query point.</param>
        /// <param name="testPolygonInterior">
        /// When true and geometry is a polygon, tests if the input point is inside the polygon.
        /// Points inside the polygon have zero distance. When false, only determines proximity to the boundary.
        /// </param>
        /// <returns>Result containing the nearest coordinate and distance information.</returns>
        public Proximity2DResult GetNearestCoordinate(Geometries.Geometry geometry, Point inputPoint, bool testPolygonInterior = false)
        {
            if (geometry == null || inputPoint == null)
                throw new ArgumentNullException();

            if (geometry.IsEmpty)
                return new Proximity2DResult();

            switch (geometry.Type)
            {
                case GeometryType.Point:
                    return GetNearestCoordinateFromPoint((Point)geometry, inputPoint);

                case GeometryType.MultiPoint:
                    return GetNearestVertexFromMultiPoint((MultiPoint)geometry, inputPoint);

                case GeometryType.Envelope:
                    return GetNearestCoordinateFromEnvelope((Envelope)geometry, inputPoint, testPolygonInterior);

                case GeometryType.Polyline:
                    return GetNearestCoordinateFromPolyline((Polyline)geometry, inputPoint);

                case GeometryType.Polygon:
                    return GetNearestCoordinateFromPolygon((Polygon)geometry, inputPoint, testPolygonInterior);

                default:
                    throw new ArgumentException($"Unsupported geometry type: {geometry.Type}");
            }
        }

        /// <summary>
        /// Returns the nearest vertex of the geometry to the given input point.
        /// </summary>
        /// <param name="geometry">The input geometry.</param>
        /// <param name="inputPoint">The query point.</param>
        /// <returns>Result containing the nearest vertex and distance information.</returns>
        public Proximity2DResult GetNearestVertex(Geometries.Geometry geometry, Point inputPoint)
        {
            if (geometry == null || inputPoint == null)
                throw new ArgumentNullException();

            if (geometry.IsEmpty)
                return new Proximity2DResult();

            switch (geometry.Type)
            {
                case GeometryType.Point:
                    return GetNearestCoordinateFromPoint((Point)geometry, inputPoint);

                case GeometryType.MultiPoint:
                    return GetNearestVertexFromMultiPoint((MultiPoint)geometry, inputPoint);

                case GeometryType.Envelope:
                    return GetNearestVertexFromEnvelope((Envelope)geometry, inputPoint);

                case GeometryType.Polyline:
                    return GetNearestVertexFromPolyline((Polyline)geometry, inputPoint);

                case GeometryType.Polygon:
                    return GetNearestVertexFromPolygon((Polygon)geometry, inputPoint);

                default:
                    throw new ArgumentException($"Unsupported geometry type: {geometry.Type}");
            }
        }

        /// <summary>
        /// Returns vertices of the geometry that are within the search radius of the given point.
        /// </summary>
        /// <param name="geometry">The input geometry.</param>
        /// <param name="inputPoint">The query point.</param>
        /// <param name="searchRadius">The maximum distance to the query point.</param>
        /// <param name="maxVertexCount">The maximum number of vertices to return.</param>
        /// <returns>Array of results sorted by distance, with the closest vertex first.</returns>
        public Proximity2DResult[] GetNearestVertices(Geometries.Geometry geometry, Point inputPoint, 
            double searchRadius, int maxVertexCount = int.MaxValue)
        {
            if (geometry == null || inputPoint == null)
                throw new ArgumentNullException();

            if (maxVertexCount < 0)
                throw new ArgumentException("maxVertexCount cannot be negative");

            if (geometry.IsEmpty)
                return new Proximity2DResult[0];

            var results = new List<Proximity2DResult>();
            double searchRadiusSq = searchRadius * searchRadius;

            switch (geometry.Type)
            {
                case GeometryType.Point:
                    {
                        var pt = (Point)geometry;
                        double distSq = DistanceSquared(pt, inputPoint);
                        if (distSq <= searchRadiusSq)
                            results.Add(new Proximity2DResult(pt, 0, Math.Sqrt(distSq)));
                        break;
                    }

                case GeometryType.MultiPoint:
                    {
                        var mp = (MultiPoint)geometry;
                        for (int i = 0; i < mp.Count; i++)
                        {
                            var pt = mp.GetPoint(i);
                            double distSq = DistanceSquared(pt, inputPoint);
                            if (distSq <= searchRadiusSq)
                                results.Add(new Proximity2DResult(pt, i, Math.Sqrt(distSq)));
                        }
                        break;
                    }

                case GeometryType.Envelope:
                    {
                        var env = (Envelope)geometry;
                        var corners = new[] {
                            new Point(env.XMin, env.YMin),
                            new Point(env.XMax, env.YMin),
                            new Point(env.XMax, env.YMax),
                            new Point(env.XMin, env.YMax)
                        };
                        for (int i = 0; i < corners.Length; i++)
                        {
                            double distSq = DistanceSquared(corners[i], inputPoint);
                            if (distSq <= searchRadiusSq)
                                results.Add(new Proximity2DResult(corners[i], i, Math.Sqrt(distSq)));
                        }
                        break;
                    }

                case GeometryType.Polyline:
                    {
                        var polyline = (Polyline)geometry;
                        int vertexIndex = 0;
                        for (int pathIndex = 0; pathIndex < polyline.PathCount; pathIndex++)
                        {
                            var path = polyline.GetPath(pathIndex);
                            foreach (var pt in path)
                            {
                                double distSq = DistanceSquared(pt, inputPoint);
                                if (distSq <= searchRadiusSq)
                                    results.Add(new Proximity2DResult(pt, vertexIndex, Math.Sqrt(distSq)));
                                vertexIndex++;
                            }
                        }
                        break;
                    }

                case GeometryType.Polygon:
                    {
                        var polygon = (Polygon)geometry;
                        int vertexIndex = 0;
                        for (int ringIndex = 0; ringIndex < polygon.RingCount; ringIndex++)
                        {
                            var ring = polygon.GetRing(ringIndex);
                            foreach (var pt in ring)
                            {
                                double distSq = DistanceSquared(pt, inputPoint);
                                if (distSq <= searchRadiusSq)
                                    results.Add(new Proximity2DResult(pt, vertexIndex, Math.Sqrt(distSq)));
                                vertexIndex++;
                            }
                        }
                        break;
                    }

                default:
                    throw new ArgumentException($"Unsupported geometry type: {geometry.Type}");
            }

            // Sort by distance
            results.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            // Return at most maxVertexCount results
            if (results.Count > maxVertexCount)
                results.RemoveRange(maxVertexCount, results.Count - maxVertexCount);

            return results.ToArray();
        }

        #region Private Helper Methods

        private Proximity2DResult GetNearestCoordinateFromPoint(Point geometry, Point inputPoint)
        {
            double distance = DistanceOperator.Instance.Execute(geometry, inputPoint);
            return new Proximity2DResult(geometry, 0, distance);
        }

        private Proximity2DResult GetNearestVertexFromMultiPoint(MultiPoint geometry, Point inputPoint)
        {
            if (geometry.Count == 0)
                return new Proximity2DResult();

            int closestIndex = 0;
            double minDistSq = DistanceSquared(geometry.GetPoint(0), inputPoint);

            for (int i = 1; i < geometry.Count; i++)
            {
                double distSq = DistanceSquared(geometry.GetPoint(i), inputPoint);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    closestIndex = i;
                }
            }

            return new Proximity2DResult(geometry.GetPoint(closestIndex), closestIndex, Math.Sqrt(minDistSq));
        }

        private Proximity2DResult GetNearestCoordinateFromEnvelope(Envelope envelope, Point inputPoint, bool testInterior)
        {
            // Test if point is inside envelope
            if (testInterior && envelope.Contains(inputPoint))
            {
                return new Proximity2DResult(inputPoint, 0, 0);
            }

            // Find closest point on envelope boundary
            double x = inputPoint.X;
            double y = inputPoint.Y;

            double clampedX = Math.Max(envelope.XMin, Math.Min(envelope.XMax, x));
            double clampedY = Math.Max(envelope.YMin, Math.Min(envelope.YMax, y));

            var closestPoint = new Point(clampedX, clampedY);
            double distance = DistanceOperator.Instance.Execute(closestPoint, inputPoint);

            return new Proximity2DResult(closestPoint, 0, distance);
        }

        private Proximity2DResult GetNearestVertexFromEnvelope(Envelope envelope, Point inputPoint)
        {
            var corners = new[] {
                new Point(envelope.XMin, envelope.YMin),
                new Point(envelope.XMax, envelope.YMin),
                new Point(envelope.XMax, envelope.YMax),
                new Point(envelope.XMin, envelope.YMax)
            };

            int closestIndex = 0;
            double minDistSq = DistanceSquared(corners[0], inputPoint);

            for (int i = 1; i < corners.Length; i++)
            {
                double distSq = DistanceSquared(corners[i], inputPoint);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    closestIndex = i;
                }
            }

            return new Proximity2DResult(corners[closestIndex], closestIndex, Math.Sqrt(minDistSq));
        }

        private Proximity2DResult GetNearestCoordinateFromPolyline(Polyline polyline, Point inputPoint)
        {
            Point? closestPoint = null;
            int closestVertexIndex = -1;
            double minDistSq = double.MaxValue;

            int vertexIndex = 0;
            for (int pathIndex = 0; pathIndex < polyline.PathCount; pathIndex++)
            {
                var path = polyline.GetPath(pathIndex);
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var p1 = path[i];
                    var p2 = path[i + 1];

                    var closestOnSegment = GetClosestPointOnSegment(p1, p2, inputPoint);
                    double distSq = DistanceSquared(closestOnSegment, inputPoint);

                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                        closestPoint = closestOnSegment;
                        closestVertexIndex = vertexIndex + i;
                    }
                }
                vertexIndex += path.Count;
            }

            if (closestPoint == null)
                return new Proximity2DResult();

            return new Proximity2DResult(closestPoint, closestVertexIndex, Math.Sqrt(minDistSq));
        }

        private Proximity2DResult GetNearestVertexFromPolyline(Polyline polyline, Point inputPoint)
        {
            Point? closestVertex = null;
            int closestIndex = -1;
            double minDistSq = double.MaxValue;

            int vertexIndex = 0;
            for (int pathIndex = 0; pathIndex < polyline.PathCount; pathIndex++)
            {
                var path = polyline.GetPath(pathIndex);
                foreach (var vertex in path)
                {
                    double distSq = DistanceSquared(vertex, inputPoint);
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                        closestVertex = vertex;
                        closestIndex = vertexIndex;
                    }
                    vertexIndex++;
                }
            }

            if (closestVertex == null)
                return new Proximity2DResult();

            return new Proximity2DResult(closestVertex, closestIndex, Math.Sqrt(minDistSq));
        }

        private Proximity2DResult GetNearestCoordinateFromPolygon(Polygon polygon, Point inputPoint, bool testInterior)
        {
            // Test if point is inside polygon
            if (testInterior && ContainsOperator.Instance.Execute(polygon, inputPoint))
            {
                return new Proximity2DResult(inputPoint, 0, 0);
            }

            // Find closest point on polygon boundary
            Point? closestPoint = null;
            int closestVertexIndex = -1;
            double minDistSq = double.MaxValue;

            int vertexIndex = 0;
            for (int ringIndex = 0; ringIndex < polygon.RingCount; ringIndex++)
            {
                var ring = polygon.GetRing(ringIndex);
                for (int i = 0; i < ring.Count - 1; i++)
                {
                    var p1 = ring[i];
                    var p2 = ring[i + 1];

                    var closestOnSegment = GetClosestPointOnSegment(p1, p2, inputPoint);
                    double distSq = DistanceSquared(closestOnSegment, inputPoint);

                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                        closestPoint = closestOnSegment;
                        closestVertexIndex = vertexIndex + i;
                    }
                }
                vertexIndex += ring.Count;
            }

            if (closestPoint == null)
                return new Proximity2DResult();

            return new Proximity2DResult(closestPoint, closestVertexIndex, Math.Sqrt(minDistSq));
        }

        private Proximity2DResult GetNearestVertexFromPolygon(Polygon polygon, Point inputPoint)
        {
            Point? closestVertex = null;
            int closestIndex = -1;
            double minDistSq = double.MaxValue;

            int vertexIndex = 0;
            for (int ringIndex = 0; ringIndex < polygon.RingCount; ringIndex++)
            {
                var ring = polygon.GetRing(ringIndex);
                foreach (var vertex in ring)
                {
                    double distSq = DistanceSquared(vertex, inputPoint);
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                        closestVertex = vertex;
                        closestIndex = vertexIndex;
                    }
                    vertexIndex++;
                }
            }

            if (closestVertex == null)
                return new Proximity2DResult();

            return new Proximity2DResult(closestVertex, closestIndex, Math.Sqrt(minDistSq));
        }

        private Point GetClosestPointOnSegment(Point p1, Point p2, Point point)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            if (dx == 0 && dy == 0)
                return p1;

            double t = ((point.X - p1.X) * dx + (point.Y - p1.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            return new Point(p1.X + t * dx, p1.Y + t * dy);
        }

        private double DistanceSquared(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return dx * dx + dy * dy;
        }

        #endregion
    }
}
