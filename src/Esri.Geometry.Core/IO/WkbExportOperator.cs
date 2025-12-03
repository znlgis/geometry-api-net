using System;
using System.Collections.Generic;
using System.IO;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.IO;

/// <summary>
///   Exports geometries to Well-Known Binary (WKB) format.
/// </summary>
public static class WkbExportOperator
{
  private const byte WKB_POINT = 1;
  private const byte WKB_LINESTRING = 2;
  private const byte WKB_POLYGON = 3;
  private const byte WKB_MULTIPOINT = 4;
  private const byte WKB_MULTILINESTRING = 5;

  /// <summary>
  ///   将几何对象导出为 WKB 格式.
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
    WriteInt32(writer, WKB_POLYGON, bigEndian);
    WriteInt32(writer, polygon.RingCount, bigEndian);

    for (var i = 0; i < polygon.RingCount; i++)
    {
      var ring = polygon.GetRing(i);
      WriteInt32(writer, ring.Count, bigEndian);
      foreach (var point in ring)
      {
        WriteDouble(writer, point.X, bigEndian);
        WriteDouble(writer, point.Y, bigEndian);
      }
    }
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
    return bigEndian != BitConverter.IsLittleEndian;
  }
}