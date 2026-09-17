using System;
using System.Collections.Generic;
using System.IO;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.IO;

/// <summary>
///     Imports geometries from Well-Known Binary (WKB) and Extended WKB (EWKB) formats.
///     支持 PostGIS 高位标志（Z=0x80000000，M=0x40000000，SRID=0x20000000）与
///     ISO WKB 维度型别（*1000=Z、*2000=M、*3000=ZM）；Z 坐标保留到 Point，
///     M 与 SRID 读取后忽略（SRID 属空间参考层职责）。
/// </summary>
public static class WkbImportOperator
{
    private const int WKB_POINT = 1;
    private const int WKB_LINESTRING = 2;
    private const int WKB_POLYGON = 3;
    private const int WKB_MULTIPOINT = 4;
    private const int WKB_MULTILINESTRING = 5;
    private const int WKB_MULTIPOLYGON = 6;

    private const uint EWKB_Z = 0x80000000;
    private const uint EWKB_M = 0x40000000;
    private const uint EWKB_SRID = 0x20000000;

    /// <summary>坐标维度描述。</summary>
    private sealed class Dims
    {
        public bool HasZ;
        public bool HasM;
    }

    /// <summary>
    ///     从 WKB/EWKB 格式导入几何对象.
    /// </summary>
    /// <param name="wkb">The WKB (or EWKB) byte array to parse.</param>
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
        if (reader.BaseStream.Position >= reader.BaseStream.Length)
            throw new FormatException("Unexpected end of WKB stream while reading byte order.");

        var byteOrder = reader.ReadByte();
        if (byteOrder != 0 && byteOrder != 1)
            throw new FormatException($"Invalid WKB byte order marker: {byteOrder}. Expected 0 (big-endian) or 1 (little-endian).");

        var bigEndian = byteOrder == 0;
        var raw = ReadUInt32(reader, bigEndian);
        return ReadBody(reader, bigEndian, raw);
    }

    /// <summary>解析已读取的类型字（含 EWKB/ISO 维度与 SRID 标志）之后的几何主体。</summary>
    private static Geometries.Geometry ReadBody(BinaryReader reader, bool bigEndian, uint raw)
    {
        var dims = new Dims();
        var baseType = (int)(raw & 0x0FFFFFFF);

        // PostGIS EWKB 高位标志
        if ((raw & EWKB_Z) != 0) dims.HasZ = true;
        if ((raw & EWKB_M) != 0) dims.HasM = true;
        if ((raw & EWKB_SRID) != 0) _ = ReadInt32(reader, bigEndian); // SRID：读取并忽略

        // ISO WKB 维度型别：1000+T=Z、2000+T=M、3000+T=ZM
        if (baseType is >= 1000 and <= 3999)
        {
            var family = baseType / 1000;
            baseType %= 1000;
            if (family is 1 or 3) dims.HasZ = true;
            if (family is 2 or 3) dims.HasM = true;
        }

        return baseType switch
        {
            WKB_POINT => ReadPoint(reader, bigEndian, dims),
            WKB_LINESTRING => ReadLineString(reader, bigEndian, dims),
            WKB_POLYGON => ReadPolygon(reader, bigEndian, dims),
            WKB_MULTIPOINT => ReadMultiPoint(reader, bigEndian),
            WKB_MULTILINESTRING => ReadMultiLineString(reader, bigEndian),
            WKB_MULTIPOLYGON => ReadMultiPolygon(reader, bigEndian),
            _ => throw new FormatException($"Unsupported WKB geometry type: {raw}")
        };
    }

    private static Point ReadPoint(BinaryReader reader, bool bigEndian, Dims dims)
    {
        var x = ReadDouble(reader, bigEndian);
        var y = ReadDouble(reader, bigEndian);
        double? z = null;
        if (dims.HasZ) z = ReadDouble(reader, bigEndian);
        if (dims.HasM) _ = ReadDouble(reader, bigEndian); // M 值读取后忽略
        return z.HasValue ? new Point(x, y, z.Value) : new Point(x, y);
    }

    private static Polyline ReadLineString(BinaryReader reader, bool bigEndian, Dims dims)
    {
        var numPoints = ReadCount(reader, bigEndian);
        var points = new List<Point>(numPoints);

        for (var i = 0; i < numPoints; i++)
            points.Add(ReadPoint(reader, bigEndian, dims));

        var polyline = new Polyline();
        polyline.AddPath(points);
        return polyline;
    }

    private static Polygon ReadPolygon(BinaryReader reader, bool bigEndian, Dims dims)
    {
        var numRings = ReadCount(reader, bigEndian);
        var polygon = new Polygon();

        for (var i = 0; i < numRings; i++)
        {
            var numPoints = ReadCount(reader, bigEndian);
            var ring = new List<Point>(numPoints);

            for (var j = 0; j < numPoints; j++)
                ring.Add(ReadPoint(reader, bigEndian, dims));

            polygon.AddRing(ring);
        }

        return polygon;
    }

    private static Polygon ReadMultiPolygon(BinaryReader reader, bool bigEndian)
    {
        var numPolygons = ReadCount(reader, bigEndian);
        var polygon = new Polygon();

        for (var i = 0; i < numPolygons; i++)
        {
            var inner = ReadSubGeometry(reader);
            if (inner is not Polygon sub)
                throw new FormatException($"Expected POLYGON inside MULTIPOLYGON, got {inner.Type}.");
            foreach (var ring in sub.GetRings())
                polygon.AddRing(ring);
        }

        return polygon;
    }

    private static MultiPoint ReadMultiPoint(BinaryReader reader, bool bigEndian)
    {
        var numPoints = ReadCount(reader, bigEndian);
        var multiPoint = new MultiPoint();

        for (var i = 0; i < numPoints; i++)
        {
            var p = ReadSubGeometry(reader);
            if (p is not Point pt)
                throw new FormatException($"Expected POINT inside MULTIPOINT, got {p.Type}.");
            multiPoint.Add(pt);
        }

        return multiPoint;
    }

    private static Polyline ReadMultiLineString(BinaryReader reader, bool bigEndian)
    {
        var numLines = ReadCount(reader, bigEndian);
        var polyline = new Polyline();

        for (var i = 0; i < numLines; i++)
        {
            var sub = ReadSubGeometry(reader);
            if (sub is not Polyline line)
                throw new FormatException($"Expected LINESTRING inside MULTILINESTRING, got {sub.Type}.");
            foreach (var path in line.GetPaths())
                polyline.AddPath(path);
        }

        return polyline;
    }

    /// <summary>multi 子几何：自带完整（EWKB）字节序与类型头。</summary>
    private static Geometries.Geometry ReadSubGeometry(BinaryReader reader)
    {
        var byteOrder = reader.ReadByte();
        if (byteOrder != 0 && byteOrder != 1)
            throw new FormatException($"Invalid WKB byte order marker: {byteOrder}. Expected 0 (big-endian) or 1 (little-endian).");
        var bigEndian = byteOrder == 0;
        var raw = ReadUInt32(reader, bigEndian);
        return ReadBody(reader, bigEndian, raw);
    }

    private static int ReadCount(BinaryReader reader, bool bigEndian)
    {
        var count = ReadInt32(reader, bigEndian);
        var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (count < 0 || (long)count * 8 > remaining)
            throw new FormatException($"WKB component count {count} exceeds remaining stream length.");
        return count;
    }

    private static int ReadInt32(BinaryReader reader, bool bigEndian)
    {
        var bytes = reader.ReadBytes(4);
        if (bytes.Length != 4) throw new FormatException("Unexpected end of WKB stream while reading int32.");
        if (bigEndian == BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static uint ReadUInt32(BinaryReader reader, bool bigEndian)
    {
        var bytes = reader.ReadBytes(4);
        if (bytes.Length != 4) throw new FormatException("Unexpected end of WKB stream while reading uint32.");
        if (bigEndian == BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static double ReadDouble(BinaryReader reader, bool bigEndian)
    {
        var bytes = reader.ReadBytes(8);
        if (bytes.Length != 8) throw new FormatException("Unexpected end of WKB stream while reading double.");
        if (bigEndian == BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToDouble(bytes, 0);
    }
}
