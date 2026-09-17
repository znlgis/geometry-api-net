using System;
using System.Collections.Generic;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.IO;

namespace OpenGIS.Esri.Geometry.Tests.IO;

public class WkbTests
{
    [Fact]
    public void WkbExport_Point_ProducesCorrectWkb()
    {
        var point = new Point(10.5, 20.7);
        var wkb = WkbExportOperator.ExportToWkb(point);

        Assert.NotNull(wkb);
        Assert.True(wkb.Length > 0);

        // First byte is byte order (1 = little endian)
        Assert.Equal(1, wkb[0]);
    }

    [Fact]
    public void WkbImport_Point_ParsesCorrectly()
    {
        var originalPoint = new Point(10.5, 20.7);
        var wkb = WkbExportOperator.ExportToWkb(originalPoint);

        var geometry = WkbImportOperator.ImportFromWkb(wkb);

        Assert.IsType<Point>(geometry);
        var point = (Point)geometry;
        Assert.Equal(10.5, point.X, 10);
        Assert.Equal(20.7, point.Y, 10);
    }

    [Fact]
    public void WkbRoundTrip_Point_PreservesData()
    {
        var originalPoint = new Point(10.123456789, 20.987654321);

        var wkb = WkbExportOperator.ExportToWkb(originalPoint);
        var parsedGeometry = WkbImportOperator.ImportFromWkb(wkb);

        Assert.IsType<Point>(parsedGeometry);
        var parsedPoint = (Point)parsedGeometry;
        Assert.Equal(originalPoint.X, parsedPoint.X, 10);
        Assert.Equal(originalPoint.Y, parsedPoint.Y, 10);
    }

    [Fact]
    public void WkbRoundTrip_LineString_PreservesData()
    {
        var polyline = new Polyline();
        var path = new[] { new Point(0, 0), new Point(10, 10), new Point(20, 20) };
        polyline.AddPath(path);

        var wkb = WkbExportOperator.ExportToWkb(polyline);
        var parsedGeometry = WkbImportOperator.ImportFromWkb(wkb);

        Assert.IsType<Polyline>(parsedGeometry);
        var parsedPolyline = (Polyline)parsedGeometry;
        Assert.Equal(1, parsedPolyline.PathCount);

        var parsedPath = parsedPolyline.GetPath(0);
        Assert.Equal(3, parsedPath.Count);
        Assert.Equal(0, parsedPath[0].X);
        Assert.Equal(20, parsedPath[2].X);
    }

    [Fact]
    public void WkbRoundTrip_Polygon_PreservesData()
    {
        var polygon = new Polygon();
        var ring = new[]
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        };
        polygon.AddRing(ring);

        var wkb = WkbExportOperator.ExportToWkb(polygon);
        var parsedGeometry = WkbImportOperator.ImportFromWkb(wkb);

        Assert.IsType<Polygon>(parsedGeometry);
        var parsedPolygon = (Polygon)parsedGeometry;
        Assert.Equal(1, parsedPolygon.RingCount);

        var parsedRing = parsedPolygon.GetRing(0);
        Assert.Equal(5, parsedRing.Count);
    }

    [Fact]
    public void WkbRoundTrip_MultiPoint_PreservesData()
    {
        var multiPoint = new MultiPoint();
        multiPoint.Add(new Point(10, 20));
        multiPoint.Add(new Point(30, 40));
        multiPoint.Add(new Point(50, 60));

        var wkb = WkbExportOperator.ExportToWkb(multiPoint);
        var parsedGeometry = WkbImportOperator.ImportFromWkb(wkb);

        Assert.IsType<MultiPoint>(parsedGeometry);
        var parsedMultiPoint = (MultiPoint)parsedGeometry;
        Assert.Equal(3, parsedMultiPoint.Count);
    }

    [Fact]
    public void WkbRoundTrip_MultiLineString_PreservesData()
    {
        var polyline = new Polyline();
        polyline.AddPath(new[] { new Point(0, 0), new Point(10, 10) });
        polyline.AddPath(new[] { new Point(20, 20), new Point(30, 30) });

        var wkb = WkbExportOperator.ExportToWkb(polyline);
        var parsedGeometry = WkbImportOperator.ImportFromWkb(wkb);

        Assert.IsType<Polyline>(parsedGeometry);
        var parsedPolyline = (Polyline)parsedGeometry;
        Assert.Equal(2, parsedPolyline.PathCount);
    }

    [Fact]
    public void WkbExport_BigEndian_ProducesCorrectByteOrder()
    {
        var point = new Point(10, 20);
        var wkb = WkbExportOperator.ExportToWkb(point, true);

        // First byte should be 0 for big endian
        Assert.Equal(0, wkb[0]);
    }

    [Fact]
    public void WkbRoundTrip_BigEndian_PreservesData()
    {
        var originalPoint = new Point(10.5, 20.7);

        var wkb = WkbExportOperator.ExportToWkb(originalPoint, true);
        var parsedGeometry = WkbImportOperator.ImportFromWkb(wkb);

        Assert.IsType<Point>(parsedGeometry);
        var parsedPoint = (Point)parsedGeometry;
        Assert.Equal(originalPoint.X, parsedPoint.X, 10);
        Assert.Equal(originalPoint.Y, parsedPoint.Y, 10);
    }

    [Fact]
    public void WkbRoundTrip_Envelope_ConvertsToPolygon()
    {
        var envelope = new Envelope(0, 0, 10, 10);

        var wkb = WkbExportOperator.ExportToWkb(envelope);
        var parsedGeometry = WkbImportOperator.ImportFromWkb(wkb);

        // Envelope should be exported as Polygon
        Assert.IsType<Polygon>(parsedGeometry);
        var polygon = (Polygon)parsedGeometry;
        Assert.Equal(1, polygon.RingCount);

        var ring = polygon.GetRing(0);
        Assert.Equal(5, ring.Count); // 4 corners + closing point
    }

    [Fact]
    public void WkbExport_LittleEndian_ByteOrderMatchesData()
    {
        var point = new Point(1.0, 2.0);
        var wkb = WkbExportOperator.ExportToWkb(point, false);

        // Byte order marker is 1 (little-endian)
        Assert.Equal(1, wkb[0]);

        // Geometry type (int32 = 1 for Point) should be in little-endian: 01 00 00 00
        Assert.Equal(1, wkb[1]);
        Assert.Equal(0, wkb[2]);
        Assert.Equal(0, wkb[3]);
        Assert.Equal(0, wkb[4]);
    }

    [Fact]
    public void WkbExport_BigEndian_ByteOrderMatchesData()
    {
        var point = new Point(1.0, 2.0);
        var wkb = WkbExportOperator.ExportToWkb(point, true);

        // Byte order marker is 0 (big-endian)
        Assert.Equal(0, wkb[0]);

        // Geometry type (int32 = 1 for Point) should be in big-endian: 00 00 00 01
        Assert.Equal(0, wkb[1]);
        Assert.Equal(0, wkb[2]);
        Assert.Equal(0, wkb[3]);
        Assert.Equal(1, wkb[4]);
    }

    [Fact]
    public void WkbImport_TruncatedData_ThrowsFormatException()
    {
        var point = new Point(10.5, 20.7);
        var wkb = WkbExportOperator.ExportToWkb(point);

        // Truncate the WKB payload so a coordinate read runs past the end of the stream.
        var truncated = new byte[wkb.Length - 4];
        Array.Copy(wkb, truncated, truncated.Length);

        Assert.Throws<FormatException>(() => WkbImportOperator.ImportFromWkb(truncated));
    }

    [Fact]
    public void WkbImport_OnlyByteOrderByte_ThrowsFormatException()
    {
        // A single byte (byte order) with no geometry type/payload following.
        var wkb = new byte[] { 1 };

        Assert.Throws<FormatException>(() => WkbImportOperator.ImportFromWkb(wkb));
    }

    [Fact]
    public void WkbImport_InvalidByteOrderMarker_ThrowsFormatException()
    {
        // Byte-order marker must be 0 (big-endian) or 1 (little-endian); 2 is invalid.
        var wkb = new byte[] { 2, 0, 0, 0, 1 };

        Assert.Throws<FormatException>(() => WkbImportOperator.ImportFromWkb(wkb));
    }
    [Fact]
    public void WkbImport_EwkbPointZ_PreservesZ()
    {
        // PostGIS EWKB: little-endian POINT with Z flag (0x80000000)
        var wkb = new List<byte> { 1 };
        wkb.AddRange(BitConverter.GetBytes(0x80000001u)); // Z | POINT
        wkb.AddRange(BitConverter.GetBytes(1.0));
        wkb.AddRange(BitConverter.GetBytes(2.0));
        wkb.AddRange(BitConverter.GetBytes(3.0));
        var p = (Point)WkbImportOperator.ImportFromWkb(wkb.ToArray());
        Assert.Equal(1, p.X);
        Assert.Equal(3, p.Z);
    }

    [Fact]
    public void WkbImport_EwkbLinestringSridStripsSridAndKeepsZ()
    {
        // EWKB: Z|SRID|LINESTRING, SRID=4326, two XYZ points
        var wkb = new List<byte> { 1 };
        wkb.AddRange(BitConverter.GetBytes(0xA0000002u));
        wkb.AddRange(BitConverter.GetBytes(4326));
        wkb.AddRange(BitConverter.GetBytes(2));
        foreach (var (x, y, z) in new[] { (0.0, 0.0, 1.0), (5.0, 5.0, 2.0) })
        {
            wkb.AddRange(BitConverter.GetBytes(x));
            wkb.AddRange(BitConverter.GetBytes(y));
            wkb.AddRange(BitConverter.GetBytes(z));
        }
        var line = (Polyline)WkbImportOperator.ImportFromWkb(wkb.ToArray());
        Assert.Equal(2, line.GetPath(0).Count);
        Assert.Equal(1.0, line.GetPath(0)[0].Z);
        Assert.Equal(2.0, line.GetPath(0)[1].Z);
    }

    [Fact]
    public void WkbImport_IsoZmLinstring_Parses32BytePoints()
    {
        // ISO WKB type 3002 = LineStringZM: each point X,Y,Z,M
        var wkb = new List<byte> { 1 };
        wkb.AddRange(BitConverter.GetBytes(3002u));
        wkb.AddRange(BitConverter.GetBytes(1));
        wkb.AddRange(BitConverter.GetBytes(2.0)); // x
        wkb.AddRange(BitConverter.GetBytes(3.0)); // y
        wkb.AddRange(BitConverter.GetBytes(4.0)); // z
        wkb.AddRange(BitConverter.GetBytes(5.0)); // m (ignored)
        var line = (Polyline)WkbImportOperator.ImportFromWkb(wkb.ToArray());
        Assert.Equal(2.0, line.GetPath(0)[0].X);
        Assert.Equal(4.0, line.GetPath(0)[0].Z);
    }

    [Fact]
    public void WkbImport_MultiPolygonWkb_ImportsAllParts()
    {
        // 标准 WKB MULTIPOLYGON(type 6)：两个正方形部件
        static IEnumerable<byte> Ring(params (double x, double y)[] pts)
        {
            foreach (var b in BitConverter.GetBytes(pts.Length)) yield return b;
            foreach (var (x, y) in pts)
            {
                foreach (var b in BitConverter.GetBytes(x)) yield return b;
                foreach (var b in BitConverter.GetBytes(y)) yield return b;
            }
        }
        var wkb = new List<byte> { 1 };
        wkb.AddRange(BitConverter.GetBytes(6));
        wkb.AddRange(BitConverter.GetBytes(2)); // 两个 polygon
        foreach (var offset in new[] { 0.0, 2.0 })
        {
            wkb.Add(1); // 子几何字节序
            wkb.AddRange(BitConverter.GetBytes(3)); // POLYGON
            wkb.AddRange(BitConverter.GetBytes(1)); // 1 环
            foreach (var b in Ring((offset, 0), (offset + 1, 0), (offset + 1, 1), (offset, 1), (offset, 0)))
                wkb.Add(b);
        }
        var poly = (Polygon)WkbImportOperator.ImportFromWkb(wkb.ToArray());
        Assert.Equal(2, poly.RingCount);
        Assert.Equal(2.0, poly.CalculateArea2D(), 9);
    }
}
