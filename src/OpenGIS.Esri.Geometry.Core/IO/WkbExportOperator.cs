using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.IO;

/// <summary>
///     Exports geometries to Well-Known Binary (WKB) format.
/// </summary>
public static class WkbExportOperator
{
    private const byte WKB_POINT = 1;
    private const byte WKB_LINESTRING = 2;
    private const byte WKB_POLYGON = 3;
    private const byte WKB_MULTIPOINT = 4;
    private const byte WKB_MULTILINESTRING = 5;
    private const byte WKB_MULTIPOLYGON = 6;

    /// <summary>
    ///     将几何对象导出为 WKB 格式.
    /// </summary>
    /// <param name="geometry">要导出的几何对象.</param>
    /// <param name="bigEndian">If true, uses big-endian byte order; otherwise little-endian (default).</param>
    /// <returns>The WKB representation as a byte array.</returns>
    public static byte[] ExportToWkb(Geometries.Geometry geometry, bool bigEndian = false)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            WriteGeometry(writer, geometry, bigEndian);
            return stream.ToArray();
        }
    }

    private static void WriteGeometry(BinaryWriter writer, Geometries.Geometry geometry, bool bigEndian)
    {
        // Write byte order marker
        writer.Write(bigEndian ? (byte)0 : (byte)1);

        if (geometry is Point point)
        {
            WritePoint(writer, point, bigEndian);
        }
        else if (geometry is Line line)
        {
            WriteLineString(writer, new[] { line.Start, line.End }, bigEndian);
        }
        else if (geometry is Polyline polyline)
        {
            if (polyline.PathCount == 1)
                WriteLineString(writer, polyline.GetPath(0), bigEndian);
            else
                WriteMultiLineString(writer, polyline, bigEndian);
        }
        else if (geometry is Polygon polygon)
        {
            WritePolygon(writer, polygon, bigEndian);
        }
        else if (geometry is MultiPoint multiPoint)
        {
            WriteMultiPoint(writer, multiPoint, bigEndian);
        }
        else if (geometry is Envelope envelope)
        {
            // Convert envelope to polygon
            var poly = new Polygon();
            var ring = new List<Point>
            {
                new(envelope.XMin, envelope.YMin),
                new(envelope.XMax, envelope.YMin),
                new(envelope.XMax, envelope.YMax),
                new(envelope.XMin, envelope.YMax),
                new(envelope.XMin, envelope.YMin)
            };
            poly.AddRing(ring);
            WritePolygon(writer, poly, bigEndian);
        }
        else
        {
            throw new NotSupportedException($"WKB export for {geometry.Type} is not supported.");
        }
    }

    private static void WritePoint(BinaryWriter writer, Point point, bool bigEndian)
    {
        WriteInt32(writer, WKB_POINT, bigEndian);
        WriteDouble(writer, point.X, bigEndian);
        WriteDouble(writer, point.Y, bigEndian);
    }

    private static void WriteLineString(BinaryWriter writer, IReadOnlyList<Point> points, bool bigEndian)
    {
        WriteInt32(writer, WKB_LINESTRING, bigEndian);
        WriteInt32(writer, points.Count, bigEndian);
        foreach (var point in points)
        {
            WriteDouble(writer, point.X, bigEndian);
            WriteDouble(writer, point.Y, bigEndian);
        }
    }

    private static void WritePolygon(BinaryWriter writer, Polygon polygon, bool bigEndian)
    {
        var parts = Internal.RingNesting.Group(polygon.GetRings()
            .Select(r => r.Select(pt => new[] { pt.X, pt.Y }).ToList()));
        if (parts.Count > 1)
        {
            WriteInt32(writer, WKB_MULTIPOLYGON, bigEndian);
            WriteInt32(writer, parts.Count, bigEndian);
            foreach (var part in parts)
            {
                writer.Write(bigEndian ? (byte)0 : (byte)1);
                WriteInt32(writer, WKB_POLYGON, bigEndian);
                var ringsOfPart = new System.Collections.Generic.List<System.Collections.Generic.List<double[]>> { part.Shell };
                ringsOfPart.AddRange(part.Holes);
                WriteInt32(writer, ringsOfPart.Count, bigEndian);
                foreach (var ring in ringsOfPart)
                {
                    var closed = NormalizeClosed(ring);
                    if (closed.Count < 2) continue;
                    WriteInt32(writer, closed.Count, bigEndian);
                    foreach (var p in closed)
                    {
                        WriteDouble(writer, p[0], bigEndian);
                        WriteDouble(writer, p[1], bigEndian);
                    }
                }
            }
            return;
        }

        WriteInt32(writer, WKB_POLYGON, bigEndian);
        WriteInt32(writer, polygon.RingCount, bigEndian);

        for (var i = 0; i < polygon.RingCount; i++)
        {
            var closed = NormalizeClosed(polygon.GetRing(i).Select(ppt => new[] { ppt.X, ppt.Y }).ToList());
            WriteInt32(writer, closed.Count, bigEndian);
            foreach (var point in closed)
            {
                WriteDouble(writer, point[0], bigEndian);
                WriteDouble(writer, point[1], bigEndian);
            }
        }
    }

    /// <summary>环闭合幂等归一：尾部所有与首点重复的点删除后，恰好补一个闭合点。</summary>
    private static System.Collections.Generic.List<double[]> NormalizeClosed(System.Collections.Generic.IEnumerable<double[]> ring)
    {
        var list = ring.ToList();
        while (list.Count > 1 && list[list.Count - 1][0] == list[0][0] && list[list.Count - 1][1] == list[0][1])
            list.RemoveAt(list.Count - 1);
        if (list.Count > 1) list.Add(list[0]);
        return list;
    }

    private static void WriteMultiPoint(BinaryWriter writer, MultiPoint multiPoint, bool bigEndian)
    {
        WriteInt32(writer, WKB_MULTIPOINT, bigEndian);
        WriteInt32(writer, multiPoint.Count, bigEndian);

        foreach (var point in multiPoint.GetPoints())
        {
            writer.Write(bigEndian ? (byte)0 : (byte)1); // Byte order for each point
            WriteInt32(writer, WKB_POINT, bigEndian);
            WriteDouble(writer, point.X, bigEndian);
            WriteDouble(writer, point.Y, bigEndian);
        }
    }

    private static void WriteMultiLineString(BinaryWriter writer, Polyline polyline, bool bigEndian)
    {
        WriteInt32(writer, WKB_MULTILINESTRING, bigEndian);
        WriteInt32(writer, polyline.PathCount, bigEndian);

        for (var i = 0; i < polyline.PathCount; i++)
        {
            writer.Write(bigEndian ? (byte)0 : (byte)1); // Byte order for each linestring
            var path = polyline.GetPath(i);
            WriteLineString(writer, path, bigEndian);
        }
    }

    private static void WriteInt32(BinaryWriter writer, int value, bool bigEndian)
    {
        var bytes = BitConverter.GetBytes(value);
        if (ShouldReverseBytes(bigEndian)) Array.Reverse(bytes);
        writer.Write(bytes);
    }

    private static void WriteDouble(BinaryWriter writer, double value, bool bigEndian)
    {
        var bytes = BitConverter.GetBytes(value);
        if (ShouldReverseBytes(bigEndian)) Array.Reverse(bytes);
        writer.Write(bytes);
    }

    private static bool ShouldReverseBytes(bool bigEndian)
    {
        return bigEndian == BitConverter.IsLittleEndian;
    }
}