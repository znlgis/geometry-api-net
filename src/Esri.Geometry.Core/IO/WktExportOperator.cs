using System;
using System.Globalization;
using System.Text;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.IO
{
    /// <summary>
    /// Exports geometries to Well-Known Text (WKT) format.
    /// </summary>
    public static class WktExportOperator
    {
        /// <summary>
        /// Exports a geometry to WKT format.
        /// </summary>
        /// <param name="geometry">The geometry to export.</param>
        /// <returns>The WKT representation of the geometry.</returns>
        public static string ExportToWkt(Geometries.Geometry geometry)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException(nameof(geometry));
            }

            if (geometry.IsEmpty)
            {
                return $"{GetGeometryTypeName(geometry)} EMPTY";
            }

            return geometry switch
            {
                Point point => ExportPoint(point),
                Line line => ExportLine(line),
                Polyline polyline => ExportPolyline(polyline),
                Polygon polygon => ExportPolygon(polygon),
                MultiPoint multiPoint => ExportMultiPoint(multiPoint),
                Envelope envelope => ExportEnvelope(envelope),
                _ => throw new NotSupportedException($"Geometry type {geometry.Type} is not supported for WKT export.")
            };
        }

        private static string GetGeometryTypeName(Geometries.Geometry geometry)
        {
            return geometry.Type switch
            {
                GeometryType.Point => "POINT",
                GeometryType.Line => "LINESTRING",
                GeometryType.Polyline => "LINESTRING",
                GeometryType.Polygon => "POLYGON",
                GeometryType.MultiPoint => "MULTIPOINT",
                GeometryType.Envelope => "POLYGON",
                _ => "GEOMETRY"
            };
        }

        private static string ExportPoint(Point point)
        {
            if (point.Z.HasValue)
            {
                return $"POINT Z ({FormatCoordinate(point.X)} {FormatCoordinate(point.Y)} {FormatCoordinate(point.Z.Value)})";
            }
            return $"POINT ({FormatCoordinate(point.X)} {FormatCoordinate(point.Y)})";
        }

        private static string ExportLine(Line line)
        {
            var sb = new StringBuilder();
            sb.Append("LINESTRING (");
            AppendPoint(sb, line.Start);
            sb.Append(", ");
            AppendPoint(sb, line.End);
            sb.Append(")");
            return sb.ToString();
        }

        private static string ExportPolyline(Polyline polyline)
        {
            if (polyline.PathCount == 1)
            {
                var path = polyline.GetPath(0);
                var sb = new StringBuilder();
                sb.Append("LINESTRING (");
                for (int i = 0; i < path.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    AppendPoint(sb, path[i]);
                }
                sb.Append(")");
                return sb.ToString();
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append("MULTILINESTRING (");
                for (int pathIdx = 0; pathIdx < polyline.PathCount; pathIdx++)
                {
                    if (pathIdx > 0) sb.Append(", ");
                    sb.Append("(");
                    var path = polyline.GetPath(pathIdx);
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        AppendPoint(sb, path[i]);
                    }
                    sb.Append(")");
                }
                sb.Append(")");
                return sb.ToString();
            }
        }

        private static string ExportPolygon(Polygon polygon)
        {
            var sb = new StringBuilder();
            sb.Append("POLYGON (");
            for (int ringIdx = 0; ringIdx < polygon.RingCount; ringIdx++)
            {
                if (ringIdx > 0) sb.Append(", ");
                sb.Append("(");
                var ring = polygon.GetRing(ringIdx);
                for (int i = 0; i < ring.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    AppendPoint(sb, ring[i]);
                }
                sb.Append(")");
            }
            sb.Append(")");
            return sb.ToString();
        }

        private static string ExportMultiPoint(MultiPoint multiPoint)
        {
            var sb = new StringBuilder();
            sb.Append("MULTIPOINT (");
            var points = multiPoint.GetPoints();
            int index = 0;
            foreach (var point in points)
            {
                if (index > 0) sb.Append(", ");
                AppendPoint(sb, point);
                index++;
            }
            sb.Append(")");
            return sb.ToString();
        }

        private static string ExportEnvelope(Envelope envelope)
        {
            var sb = new StringBuilder();
            sb.Append("POLYGON ((");
            sb.Append($"{FormatCoordinate(envelope.XMin)} {FormatCoordinate(envelope.YMin)}, ");
            sb.Append($"{FormatCoordinate(envelope.XMax)} {FormatCoordinate(envelope.YMin)}, ");
            sb.Append($"{FormatCoordinate(envelope.XMax)} {FormatCoordinate(envelope.YMax)}, ");
            sb.Append($"{FormatCoordinate(envelope.XMin)} {FormatCoordinate(envelope.YMax)}, ");
            sb.Append($"{FormatCoordinate(envelope.XMin)} {FormatCoordinate(envelope.YMin)}");
            sb.Append("))");
            return sb.ToString();
        }

        private static void AppendPoint(StringBuilder sb, Point point)
        {
            sb.Append(FormatCoordinate(point.X));
            sb.Append(' ');
            sb.Append(FormatCoordinate(point.Y));
            if (point.Z.HasValue)
            {
                sb.Append(' ');
                sb.Append(FormatCoordinate(point.Z.Value));
            }
        }

        private static string FormatCoordinate(double value)
        {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }
    }
}
