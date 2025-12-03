using System;
using System.Globalization;
using System.Text;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.IO
{
    /// <summary>
    /// Operator for exporting geometries to GeoJSON format.
    /// </summary>
    public class GeoJsonExportOperator : Operators.IGeometryOperator<string>
    {
        private static readonly GeoJsonExportOperator _instance = new GeoJsonExportOperator();

        /// <summary>
        /// Gets the singleton instance of the GeoJSON export operator.
        /// </summary>
        public static GeoJsonExportOperator Instance => _instance;

        private GeoJsonExportOperator() { }

        /// <summary>
        /// Exports a geometry to GeoJSON format.
        /// </summary>
        /// <param name="geometry">The geometry to export.</param>
        /// <param name="spatialReference">Optional spatial reference (not used in basic GeoJSON).</param>
        /// <returns>GeoJSON string representation of the geometry.</returns>
        public string Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialReference = null)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            return ExportToGeoJson(geometry);
        }

        /// <summary>
        /// Exports a geometry to GeoJSON format.
        /// </summary>
        /// <param name="geometry">The geometry to export.</param>
        /// <returns>GeoJSON string representation.</returns>
        public static string ExportToGeoJson(Geometries.Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            if (geometry.IsEmpty)
                return $"{{\"type\":\"GeometryCollection\",\"geometries\":[]}}";

            switch (geometry.Type)
            {
                case GeometryType.Point:
                    return ExportPoint((Point)geometry);
                case GeometryType.MultiPoint:
                    return ExportMultiPoint((MultiPoint)geometry);
                case GeometryType.Line:
                    return ExportLineString((Line)geometry);
                case GeometryType.Polyline:
                    return ExportPolyline((Polyline)geometry);
                case GeometryType.Polygon:
                    return ExportPolygon((Polygon)geometry);
                case GeometryType.Envelope:
                    return ExportEnvelope((Envelope)geometry);
                default:
                    throw new ArgumentException($"Unsupported geometry type: {geometry.Type}");
            }
        }

        private static string ExportPoint(Point point)
        {
            var sb = new StringBuilder();
            sb.Append("{\"type\":\"Point\",\"coordinates\":");
            AppendCoordinate(sb, point);
            sb.Append("}");
            return sb.ToString();
        }

        private static string ExportMultiPoint(MultiPoint multiPoint)
        {
            var sb = new StringBuilder();
            sb.Append("{\"type\":\"MultiPoint\",\"coordinates\":[");
            
            for (int i = 0; i < multiPoint.Count; i++)
            {
                if (i > 0) sb.Append(",");
                AppendCoordinate(sb, multiPoint.GetPoint(i));
            }
            
            sb.Append("]}");
            return sb.ToString();
        }

        private static string ExportLineString(Line line)
        {
            var sb = new StringBuilder();
            sb.Append("{\"type\":\"LineString\",\"coordinates\":[");
            AppendCoordinate(sb, line.Start);
            sb.Append(",");
            AppendCoordinate(sb, line.End);
            sb.Append("]}");
            return sb.ToString();
        }

        private static string ExportPolyline(Polyline polyline)
        {
            if (polyline.PathCount == 1)
            {
                // Single path - export as LineString
                var sb = new StringBuilder();
                sb.Append("{\"type\":\"LineString\",\"coordinates\":[");
                var path = polyline.GetPath(0);
                for (int i = 0; i < path.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    AppendCoordinate(sb, path[i]);
                }
                sb.Append("]}");
                return sb.ToString();
            }
            else
            {
                // Multiple paths - export as MultiLineString
                var sb = new StringBuilder();
                sb.Append("{\"type\":\"MultiLineString\",\"coordinates\":[");
                for (int pathIdx = 0; pathIdx < polyline.PathCount; pathIdx++)
                {
                    if (pathIdx > 0) sb.Append(",");
                    sb.Append("[");
                    var path = polyline.GetPath(pathIdx);
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (i > 0) sb.Append(",");
                        AppendCoordinate(sb, path[i]);
                    }
                    sb.Append("]");
                }
                sb.Append("]}");
                return sb.ToString();
            }
        }

        private static string ExportPolygon(Polygon polygon)
        {
            var sb = new StringBuilder();
            sb.Append("{\"type\":\"Polygon\",\"coordinates\":[");
            
            for (int ringIdx = 0; ringIdx < polygon.RingCount; ringIdx++)
            {
                if (ringIdx > 0) sb.Append(",");
                sb.Append("[");
                var ring = polygon.GetRing(ringIdx);
                for (int i = 0; i < ring.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    AppendCoordinate(sb, ring[i]);
                }
                sb.Append("]");
            }
            
            sb.Append("]}");
            return sb.ToString();
        }

        private static string ExportEnvelope(Envelope envelope)
        {
            // Export envelope as a Polygon (rectangular polygon)
            var sb = new StringBuilder();
            sb.Append("{\"type\":\"Polygon\",\"coordinates\":[[");
            
            // Bottom-left
            sb.Append($"[{FormatCoord(envelope.XMin)},{FormatCoord(envelope.YMin)}],");
            // Bottom-right
            sb.Append($"[{FormatCoord(envelope.XMax)},{FormatCoord(envelope.YMin)}],");
            // Top-right
            sb.Append($"[{FormatCoord(envelope.XMax)},{FormatCoord(envelope.YMax)}],");
            // Top-left
            sb.Append($"[{FormatCoord(envelope.XMin)},{FormatCoord(envelope.YMax)}],");
            // Close ring (back to bottom-left)
            sb.Append($"[{FormatCoord(envelope.XMin)},{FormatCoord(envelope.YMin)}]");
            
            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendCoordinate(StringBuilder sb, Point point)
        {
            sb.Append("[");
            sb.Append(FormatCoord(point.X));
            sb.Append(",");
            sb.Append(FormatCoord(point.Y));
            if (point.Z.HasValue && !double.IsNaN(point.Z.Value))
            {
                sb.Append(",");
                sb.Append(FormatCoord(point.Z.Value));
            }
            sb.Append("]");
        }

        private static string FormatCoord(double value)
        {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }
    }
}
