using System;
using System.Globalization;
using System.Text;
using System.Linq;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.IO;

/// <summary>
///     Exports geometries to Well-Known Text (WKT) format.
/// </summary>
public static class WktExportOperator
{
  /// <summary>
  ///     将几何对象导出为 WKT 格式.
  /// </summary>
  /// <param name="geometry">要导出的几何对象.</param>
  /// <returns>几何对象的 WKT 表示.</returns>
  public static string ExportToWkt(Geometries.Geometry geometry)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));

        if (geometry.IsEmpty) return $"{GetGeometryTypeName(geometry)} EMPTY";

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
            return
                $"POINT Z ({FormatCoordinate(point.X)} {FormatCoordinate(point.Y)} {FormatCoordinate(point.Z.Value)})";
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
        var sb = new StringBuilder(256); // Pre-allocate reasonable capacity
        if (polyline.PathCount == 1)
        {
            var path = polyline.GetPath(0);
            sb.Append("LINESTRING (");
            for (var i = 0; i < path.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                AppendPoint(sb, path[i]);
            }

            sb.Append(')');
            return sb.ToString();
        }

        sb.Append("MULTILINESTRING (");
        for (var pathIdx = 0; pathIdx < polyline.PathCount; pathIdx++)
        {
            if (pathIdx > 0) sb.Append(", ");
            sb.Append('(');
            var path = polyline.GetPath(pathIdx);
            for (var i = 0; i < path.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                AppendPoint(sb, path[i]);
            }

            sb.Append(')');
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static string ExportPolygon(Polygon polygon)
    {
        var parts = RingNesting.Group(polygon.GetRings()
            .Select(ring => ring.Select(pt => new[] { pt.X, pt.Y })));

        var sb = new StringBuilder(256);
        if (parts.Count <= 1)
        {
            sb.Append("POLYGON (");
            AppendPartRings(sb, polygon);
            sb.Append(')');
            return sb.ToString();
        }

        sb.Append("MULTIPOLYGON (");
        for (var pi = 0; pi < parts.Count; pi++)
        {
            if (pi > 0) sb.Append(", ");
            sb.Append('(');
            var part = parts[pi];
            AppendRingCoords(sb, part.Shell);
            foreach (var hole in part.Holes)
            {
                sb.Append(", ");
                AppendRingCoords(sb, hole);
            }

            sb.Append(')');
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static void AppendRingCoords(StringBuilder sb, System.Collections.Generic.IReadOnlyList<double[]> ring)
    {
        sb.Append('(');
        for (var i = 0; i < ring.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(FormatCoordinate(ring[i][0])).Append(' ').Append(FormatCoordinate(ring[i][1]));
        }

        // 闭合
        sb.Append(", ").Append(FormatCoordinate(ring[0][0])).Append(' ').Append(FormatCoordinate(ring[0][1]));
        sb.Append(')');
    }

    private static void AppendPartRings(StringBuilder sb, Polygon polygon)
    {
        for (var ringIdx = 0; ringIdx < polygon.RingCount; ringIdx++)
        {
            if (ringIdx > 0) sb.Append(", ");
            sb.Append('(');
            var ring = polygon.GetRing(ringIdx);
            for (var i = 0; i < ring.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                AppendPoint(sb, ring[i]);
            }

            sb.Append(')');
        }
    }

    private static string ExportMultiPoint(MultiPoint multiPoint)
    {
        var sb = new StringBuilder(128); // Pre-allocate reasonable capacity
        sb.Append("MULTIPOINT (");
        var points = multiPoint.GetPoints();
        var index = 0;
        foreach (var point in points)
        {
            if (index > 0) sb.Append(", ");
            AppendPoint(sb, point);
            index++;
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static string ExportEnvelope(Envelope envelope)
    {
        var sb = new StringBuilder(200);
        sb.Append("POLYGON ((");
        // 标准五点闭合环
        var pts = new[] { new[] { envelope.XMin, envelope.YMin }, new[] { envelope.XMax, envelope.YMin }, new[] { envelope.XMax, envelope.YMax }, new[] { envelope.XMin, envelope.YMax }, new[] { envelope.XMin, envelope.YMin } };
        for (var i = 0; i < pts.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            AppendCoordinate(sb, pts[i][0], pts[i][1]);
        }

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

    private static void AppendCoordinate(StringBuilder sb, double x, double y)
    {
        sb.Append(FormatCoordinate(x));
        sb.Append(' ');
        sb.Append(FormatCoordinate(y));
    }

    private static string FormatCoordinate(double value)
    {
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }
}