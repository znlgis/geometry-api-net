using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;
using System;
using System.Text;
using System.Text.Json;

namespace Esri.Geometry.Core.IO
{
    /// <summary>
    /// Exports geometries to Esri JSON format.
    /// Esri JSON is similar to GeoJSON but with Esri-specific extensions.
    /// </summary>
    public class EsriJsonExportOperator : IGeometryOperator<string>
    {
        private static readonly Lazy<EsriJsonExportOperator> _instance = new Lazy<EsriJsonExportOperator>(() => new EsriJsonExportOperator());

        /// <summary>
        /// Gets the singleton instance of the EsriJsonExportOperator.
        /// </summary>
        public static EsriJsonExportOperator Instance => _instance.Value;

        private EsriJsonExportOperator() { }

        /// <inheritdoc/>
        public string Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            if (geometry.IsEmpty)
            {
                return "{}";
            }

            switch (geometry)
            {
                case Point point:
                    return ExportPoint(point);
                case MultiPoint multiPoint:
                    return ExportMultiPoint(multiPoint);
                case Polyline polyline:
                    return ExportPolyline(polyline);
                case Polygon polygon:
                    return ExportPolygon(polygon);
                case Envelope envelope:
                    return ExportEnvelope(envelope);
                default:
                    throw new ArgumentException($"Unsupported geometry type: {geometry.GetType().Name}");
            }
        }

        private string ExportPoint(Point point)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"x\":{FormatNumber(point.X)},");
            sb.Append($"\"y\":{FormatNumber(point.Y)}");

            if (point.Z.HasValue)
            {
                sb.Append($",\"z\":{FormatNumber(point.Z.Value)}");
            }
            if (point.M.HasValue)
            {
                sb.Append($",\"m\":{FormatNumber(point.M.Value)}");
            }

            sb.Append("}");
            return sb.ToString();
        }

        private string ExportMultiPoint(MultiPoint multiPoint)
        {
            var sb = new StringBuilder();
            sb.Append("{\"points\":[");

            for (int i = 0; i < multiPoint.Count; i++)
            {
                if (i > 0) sb.Append(",");

                Point point = multiPoint.GetPoint(i);
                sb.Append($"[{FormatNumber(point.X)},{FormatNumber(point.Y)}");

                if (point.Z.HasValue)
                {
                    sb.Append($",{FormatNumber(point.Z.Value)}");
                }

                sb.Append("]");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private string ExportPolyline(Polyline polyline)
        {
            var sb = new StringBuilder();
            sb.Append("{\"paths\":[");

            for (int i = 0; i < polyline.PathCount; i++)
            {
                if (i > 0) sb.Append(",");

                var path = polyline.GetPath(i);
                sb.Append("[");

                for (int j = 0; j < path.Count; j++)
                {
                    if (j > 0) sb.Append(",");

                    Point point = path[j];
                    sb.Append($"[{FormatNumber(point.X)},{FormatNumber(point.Y)}");

                    if (point.Z.HasValue)
                    {
                        sb.Append($",{FormatNumber(point.Z.Value)}");
                    }

                    sb.Append("]");
                }

                sb.Append("]");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private string ExportPolygon(Polygon polygon)
        {
            var sb = new StringBuilder();
            sb.Append("{\"rings\":[");

            for (int i = 0; i < polygon.RingCount; i++)
            {
                if (i > 0) sb.Append(",");

                var ring = polygon.GetRing(i);
                sb.Append("[");

                for (int j = 0; j < ring.Count; j++)
                {
                    if (j > 0) sb.Append(",");

                    Point point = ring[j];
                    sb.Append($"[{FormatNumber(point.X)},{FormatNumber(point.Y)}");

                    if (point.Z.HasValue)
                    {
                        sb.Append($",{FormatNumber(point.Z.Value)}");
                    }

                    sb.Append("]");
                }

                sb.Append("]");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private string ExportEnvelope(Envelope envelope)
        {
            // Export as xmin, ymin, xmax, ymax
            return $"{{\"xmin\":{FormatNumber(envelope.XMin)},\"ymin\":{FormatNumber(envelope.YMin)}," +
                   $"\"xmax\":{FormatNumber(envelope.XMax)},\"ymax\":{FormatNumber(envelope.YMax)}}}";
        }

        private string FormatNumber(double value)
        {
            return value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
