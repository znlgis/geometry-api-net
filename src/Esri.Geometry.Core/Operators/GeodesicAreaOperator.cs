using Esri.Geometry.Core.Geometries;
using System;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Calculates geodesic area on WGS84 ellipsoid using spherical excess formula.
    /// For accurate area calculations on Earth's surface.
    /// </summary>
    public class GeodesicAreaOperator : IGeometryOperator<double>
    {
        private static readonly Lazy<GeodesicAreaOperator> _instance = new Lazy<GeodesicAreaOperator>(() => new GeodesicAreaOperator());

        /// <summary>
        /// Gets the singleton instance of the GeodesicAreaOperator.
        /// </summary>
        public static GeodesicAreaOperator Instance => _instance.Value;

        private GeodesicAreaOperator() { }

        // WGS84 ellipsoid parameters
        private const double WGS84_SEMI_MAJOR_AXIS = 6378137.0; // meters
        private const double WGS84_FLATTENING = 1.0 / 298.257223563;
        private const double WGS84_SEMI_MINOR_AXIS = WGS84_SEMI_MAJOR_AXIS * (1.0 - WGS84_FLATTENING);

        /// <inheritdoc/>
        public double Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
        {
            if (geometry == null || geometry.IsEmpty)
                return 0.0;

            switch (geometry)
            {
                case Polygon polygon:
                    return CalculatePolygonGeodesicArea(polygon);
                case Envelope envelope:
                    return CalculateEnvelopeGeodesicArea(envelope);
                default:
                    // Points, lines, etc. have zero area
                    return 0.0;
            }
        }

        private double CalculatePolygonGeodesicArea(Polygon polygon)
        {
            double totalArea = 0.0;

            // Calculate area of outer ring
            if (polygon.RingCount > 0)
            {
                totalArea = CalculateRingGeodesicArea(polygon.GetRing(0));

                // Subtract areas of holes
                for (int i = 1; i < polygon.RingCount; i++)
                {
                    totalArea -= CalculateRingGeodesicArea(polygon.GetRing(i));
                }
            }

            return Math.Abs(totalArea);
        }

        private double CalculateRingGeodesicArea(System.Collections.Generic.IReadOnlyList<Point> ring)
        {
            if (ring.Count < 3)
                return 0.0;

            // Use spherical excess formula for geodesic area
            // This is a simplified approach using spherical approximation
            double area = 0.0;
            int n = ring.Count;

            for (int i = 0; i < n - 1; i++)
            {
                Point p1 = ring[i];
                Point p2 = ring[i + 1];

                double lon1 = p1.X * Math.PI / 180.0; // Convert to radians
                double lat1 = p1.Y * Math.PI / 180.0;
                double lon2 = p2.X * Math.PI / 180.0;
                double lat2 = p2.Y * Math.PI / 180.0;

                // Calculate area contribution using spherical approximation
                area += (lon2 - lon1) * (2.0 + Math.Sin(lat1) + Math.Sin(lat2));
            }

            // Convert to square meters using mean radius
            double meanRadius = (WGS84_SEMI_MAJOR_AXIS + WGS84_SEMI_MINOR_AXIS) / 2.0;
            area = Math.Abs(area * meanRadius * meanRadius / 2.0);

            return area;
        }

        private double CalculateEnvelopeGeodesicArea(Envelope envelope)
        {
            // Convert envelope to polygon and calculate
            var ring = new Point[]
            {
                new Point(envelope.XMin, envelope.YMin),
                new Point(envelope.XMax, envelope.YMin),
                new Point(envelope.XMax, envelope.YMax),
                new Point(envelope.XMin, envelope.YMax),
                new Point(envelope.XMin, envelope.YMin)
            };

            return CalculateRingGeodesicArea(ring);
        }
    }
}
