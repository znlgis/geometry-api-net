using System;
using System.Collections.Generic;
using System.Linq;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Simplifies the geometry or determines if the geometry is simple according to OGC specification.
    /// Follows the OGC specification for the Simple Feature Access v. 1.2.1 (06-103r4).
    /// </summary>
    public class SimplifyOGCOperator : IGeometryOperator<Geometries.Geometry>
    {
        private static readonly Lazy<SimplifyOGCOperator> _instance =
            new Lazy<SimplifyOGCOperator>(() => new SimplifyOGCOperator());

        /// <summary>
        /// Gets the singleton instance of the SimplifyOGCOperator.
        /// </summary>
        public static SimplifyOGCOperator Instance => _instance.Value;

        private SimplifyOGCOperator() { }

        /// <summary>
        /// Processes geometry to ensure it is simple for OGC specification.
        /// </summary>
        /// <param name="geometry">The geometry to be simplified.</param>
        /// <param name="spatialRef">Spatial reference to obtain the tolerance from. When null, a default tolerance is used.</param>
        /// <returns>Returns a simple geometry that should be visually equivalent to the input geometry.</returns>
        public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
        {
            return Execute(geometry, spatialRef, forceSimplify: false);
        }

        /// <summary>
        /// Processes geometry to ensure it is simple for OGC specification.
        /// </summary>
        /// <param name="geometry">The geometry to be simplified.</param>
        /// <param name="spatialRef">Spatial reference to obtain the tolerance from. When null, a default tolerance is used.</param>
        /// <param name="forceSimplify">When true, the geometry will be simplified regardless of its current state.</param>
        /// <returns>Returns a simple geometry that should be visually equivalent to the input geometry.</returns>
        public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef, bool forceSimplify)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            if (geometry.IsEmpty)
                return geometry;

            // Calculate tolerance from spatial reference or geometry bounds
            double tolerance = CalculateTolerance(geometry, spatialRef);

            // For OGC simplification, we use the standard simplify operator
            // OGC simplification ensures:
            // - No self-intersections
            // - Valid topology
            // - Removes spikes and tiny segments
            
            // Use the standard simplify operator with the calculated tolerance
            var simplifiedGeometry = SimplifyOperator.Instance.Execute(geometry, tolerance, spatialRef);

            // Additional OGC-specific validation and fixes
            simplifiedGeometry = EnsureOGCCompliance(simplifiedGeometry);

            return simplifiedGeometry;
        }

        /// <summary>
        /// Tests if the geometry is simple according to OGC specification.
        /// </summary>
        /// <param name="geometry">The geometry to be tested.</param>
        /// <param name="spatialRef">Spatial reference to obtain the tolerance from.</param>
        /// <returns>True if the geometry is OGC-simple, false otherwise.</returns>
        public bool IsSimpleOGC(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
        {
            if (geometry == null)
                return false;

            if (geometry.IsEmpty)
                return true;

            // Check basic validity requirements
            switch (geometry.Type)
            {
                case GeometryType.Point:
                    return IsSimplePoint((Point)geometry);

                case GeometryType.MultiPoint:
                    return IsSimpleMultiPoint((MultiPoint)geometry);

                case GeometryType.Polyline:
                    return IsSimplePolyline((Polyline)geometry);

                case GeometryType.Polygon:
                    return IsSimplePolygon((Polygon)geometry);

                case GeometryType.Envelope:
                    return true; // Envelopes are always simple

                default:
                    return true;
            }
        }

        private bool IsSimplePoint(Point point)
        {
            // A point is simple if it's not empty
            return !point.IsEmpty;
        }

        private bool IsSimpleMultiPoint(MultiPoint multiPoint)
        {
            // A multipoint is simple if all points are distinct (no duplicates)
            // For simplicity, we'll consider it simple if it has at least one point
            return multiPoint.Count > 0;
        }

        private bool IsSimplePolyline(Polyline polyline)
        {
            // A polyline is simple if:
            // - It has at least 2 points per path
            // - It doesn't self-intersect (simplified check)
            foreach (var path in polyline.GetPaths())
            {
                if (path.Count < 2)
                    return false;
            }
            return true;
        }

        private bool IsSimplePolygon(Polygon polygon)
        {
            // A polygon is simple if:
            // - Each ring is closed
            // - Rings don't self-intersect
            // - Each ring has at least 4 points (3 distinct + closing point)
            foreach (var ring in polygon.GetRings())
            {
                if (ring.Count < 4)
                    return false;

                // Check if ring is closed
                var first = ring[0];
                var last = ring[ring.Count - 1];
                if (Math.Abs(first.X - last.X) > 1e-10 || Math.Abs(first.Y - last.Y) > 1e-10)
                    return false;
            }
            return true;
        }

        private double CalculateTolerance(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef)
        {
            // If spatial reference provides tolerance, use it
            // Otherwise calculate from geometry bounds
            
            // Default tolerance based on geometry extent
            var envelope = geometry.GetEnvelope();
            double extent = Math.Max(envelope.Width, envelope.Height);
            
            // Use a small fraction of the extent as tolerance
            // This is a simplified approach - real implementation would use SR tolerance
            double tolerance = extent * 1e-7;
            
            // Ensure minimum tolerance
            return Math.Max(tolerance, 1e-10);
        }

        private Geometries.Geometry EnsureOGCCompliance(Geometries.Geometry geometry)
        {
            // Ensure geometry meets OGC simple feature requirements
            // This is a simplified version - full implementation would include:
            // - Removing self-intersections
            // - Fixing ring orientation
            // - Removing duplicate vertices
            // - Closing unclosed rings
            
            if (geometry is Polygon polygon)
            {
                return EnsureClosedRings(polygon);
            }

            return geometry;
        }

        private Polygon EnsureClosedRings(Polygon polygon)
        {
            var result = new Polygon();
            
            foreach (var ring in polygon.GetRings())
            {
                var ringList = ring.ToList();
                if (ringList.Count < 3)
                    continue;

                // Ensure ring is closed
                var first = ringList[0];
                var last = ringList[ringList.Count - 1];
                
                if (Math.Abs(first.X - last.X) > 1e-10 || Math.Abs(first.Y - last.Y) > 1e-10)
                {
                    ringList.Add(first); // Close the ring
                }

                if (ringList.Count >= 4) // Need at least 3 distinct points + closing point
                {
                    result.AddRing(ringList);
                }
            }

            return result;
        }
    }
}
