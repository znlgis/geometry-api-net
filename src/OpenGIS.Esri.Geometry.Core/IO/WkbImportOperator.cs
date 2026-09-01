using System;
using System.Collections.Generic;
using System.IO;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.IO;

/// <summary>
///     Imports geometries from Well-Known Binary (WKB) format.
/// </summary>
public static class WkbImportOperator
{
    private const byte WKB_POINT = 1;
    private const byte WKB_LINESTRING = 2;
    private const byte WKB_POLYGON = 3;
    private const byte WKB_MULTIPOINT = 4;
    private const byte WKB_MULTILINESTRING = 5;

    /// <summary>
    ///     从 WKB 格式导入几何对象.
    /// </summary>
    /// <param name="wkb">The WKB byte array to parse.</param>
    /// <returns>The parsed geometry.</returns>
    public static Geometries.Geometry ImportFromWkb(byte[] wkb)
    {
        if (wkb == null || wkb.Length == 0)
            throw new ArgumentException("WKB data cannot be null or empty.", nameof(wkb));

        using (var stream = new MemoryStream(wkb))
        using (var reader = new BinaryReader(stream))
        {
            return ReadGeometry(reader);
        }
    }

    private static Geometries.Geometry ReadGeometry(BinaryReader reader)
    {
        // Read byte order
        if (reader.BaseStream.Position >= reader.BaseStream.Length)
            throw new FormatException("Unexpected end of WKB stream while reading byte order.");

        var byteOrder = reader.ReadByte();
        if (byteOrder != 0 && byteOrder != 1)
            throw new FormatException($"Invalid WKB byte order marker: {byteOrder}. Expected 0 (big-endian) or 1 (little-endian).");

        var bigEndian = byteOrder == 0;

        // Read geometry type
        var geometryType = ReadInt32(reader, bigEndian);

        return geometryType switch
        {
            WKB_POINT => ReadPoint(reader, bigEndian),
            WKB_LINESTRING => ReadLineString(reader, bigEndian),
            WKB_POLYGON => ReadPolygon(reader, bigEndian),
            WKB_MULTIPOINT => ReadMultiPoint(reader, bigEndian),
            WKB_MULTILINESTRING => ReadMultiLineString(reader, bigEndian),
            _ => throw new FormatException($"Unsupported WKB geometry type: {geometryType}")
        };
    }

    private static Point ReadPoint(BinaryReader reader, bool bigEndian)
    {
        var x = ReadDouble(reader, bigEndian);
        var y = ReadDouble(reader, bigEndian);
        return new Point(x, y);
    }

    private static Polyline ReadLineString(BinaryReader reader, bool bigEndian)
    {
        var numPoints = ReadCount(reader, bigEndian, bytesPerElement: 16);
        var points = new List<Point>(numPoints);

        for (var i = 0; i < numPoints; i++)
        {
            var x = ReadDouble(reader, bigEndian);
            var y = ReadDouble(reader, bigEndian);
            points.Add(new Point(x, y));
        }

        var polyline = new Polyline();
        polyline.AddPath(points);
        return polyline;
    }

    private static Polygon ReadPolygon(BinaryReader reader, bool bigEndian)
    {
        var numRings = ReadCount(reader, bigEndian, bytesPerElement: 4);
        var polygon = new Polygon();

        for (var i = 0; i < numRings; i++)
        {
            var numPoints = ReadCount(reader, bigEndian, bytesPerElement: 16);
            var ring = new List<Point>(numPoints);

            for (var j = 0; j < numPoints; j++)
            {
                var x = ReadDouble(reader, bigEndian);
                var y = ReadDouble(reader, bigEndian);
                ring.Add(new Point(x, y));
            }

            polygon.AddRing(ring);
        }

        return polygon;
    }

    private static MultiPoint ReadMultiPoint(BinaryReader reader, bool bigEndian)
    {
        var numPoints = ReadCount(reader, bigEndian, bytesPerElement: 21);
        var multiPoint = new MultiPoint();

        for (var i = 0; i < numPoints; i++)
        {
            // Each point has its own byte order and type
            var pointByteOrder = reader.ReadByte();
            if (pointByteOrder != 0 && pointByteOrder != 1)
                throw new FormatException($"Invalid WKB byte order marker: {pointByteOrder}. Expected 0 (big-endian) or 1 (little-endian).");
            var pointBigEndian = pointByteOrder == 0;
            var pointType = ReadInt32(reader, pointBigEndian);

            if (pointType != WKB_POINT)
                throw new FormatException($"Expected point type in multipoint, got {pointType}");

            var x = ReadDouble(reader, pointBigEndian);
            var y = ReadDouble(reader, pointBigEndian);
            multiPoint.Add(new Point(x, y));
        }

        return multiPoint;
    }

    private static Polyline ReadMultiLineString(BinaryReader reader, bool bigEndian)
    {
        var numLineStrings = ReadCount(reader, bigEndian, bytesPerElement: 4);
        var polyline = new Polyline();

        for (var i = 0; i < numLineStrings; i++)
        {
            // Each linestring has its own byte order and type
            var lsByteOrder = reader.ReadByte();
            if (lsByteOrder != 0 && lsByteOrder != 1)
                throw new FormatException($"Invalid WKB byte order marker: {lsByteOrder}. Expected 0 (big-endian) or 1 (little-endian).");
            var lsBigEndian = lsByteOrder == 0;
            var lsType = ReadInt32(reader, lsBigEndian);

            if (lsType != WKB_LINESTRING)
                throw new FormatException($"Expected linestring type in multilinestring, got {lsType}");

            var numPoints = ReadCount(reader, lsBigEndian, bytesPerElement: 16);
            var points = new List<Point>(numPoints);

            for (var j = 0; j < numPoints; j++)
            {
                var x = ReadDouble(reader, lsBigEndian);
                var y = ReadDouble(reader, lsBigEndian);
                points.Add(new Point(x, y));
            }

            polyline.AddPath(points);
        }

        return polyline;
    }

    private static int ReadCount(BinaryReader reader, bool bigEndian, int bytesPerElement)
    {
        var count = ReadInt32(reader, bigEndian);
        if (count < 0)
            throw new FormatException($"Invalid negative element count in WKB stream: {count}.");

        var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (count > remaining / bytesPerElement)
            throw new FormatException(
                $"WKB element count {count} exceeds the remaining stream length ({remaining} bytes).");

        return count;
    }

    private static int ReadInt32(BinaryReader reader, bool bigEndian)
    {
        var bytes = reader.ReadBytes(4);
        if (bytes.Length != 4)
            throw new FormatException("Unexpected end of WKB stream while reading a 32-bit integer.");
        if (ShouldReverseBytes(bigEndian)) Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static double ReadDouble(BinaryReader reader, bool bigEndian)
    {
        var bytes = reader.ReadBytes(8);
        if (bytes.Length != 8)
            throw new FormatException("Unexpected end of WKB stream while reading a double.");
        if (ShouldReverseBytes(bigEndian)) Array.Reverse(bytes);
        return BitConverter.ToDouble(bytes, 0);
    }

    private static bool ShouldReverseBytes(bool bigEndian)
    {
        return bigEndian == BitConverter.IsLittleEndian;
    }
}