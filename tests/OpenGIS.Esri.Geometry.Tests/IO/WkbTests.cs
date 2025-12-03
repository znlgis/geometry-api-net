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
}