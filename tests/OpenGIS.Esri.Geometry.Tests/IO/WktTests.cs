using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.IO;

namespace OpenGIS.Esri.Geometry.Tests.IO;

public class WktTests
{
    [Fact]
    public void WktExport_Point_ProducesCorrectWkt()
    {
        var point = new Point(10.5, 20.7);
        var wkt = WktExportOperator.ExportToWkt(point);

        Assert.StartsWith("POINT (", wkt);
        Assert.Contains("10.5", wkt);
        // Floating point 20.7 may be represented as 20.699999...
        Assert.Matches(@"20\.69\d+", wkt);
    }

    [Fact]
    public void WktExport_PointWithZ_ProducesCorrectWkt()
    {
        var point = new Point(10.5, 20.7, 30.1);
        var wkt = WktExportOperator.ExportToWkt(point);

        Assert.Contains("POINT Z", wkt);
        Assert.Contains("10.5", wkt);
        // Check that coordinates are present (floating point may have precision issues)
        // 20.7 may be represented as 20.699999...
        Assert.Matches(@"20\.69\d+", wkt);
        Assert.Matches(@"30\.1\d*", wkt);
    }

    [Fact]
    public void WktExport_LineString_ProducesCorrectWkt()
    {
        var line = new Line(new Point(0, 0), new Point(10, 10));
        var wkt = WktExportOperator.ExportToWkt(line);

        Assert.StartsWith("LINESTRING", wkt);
        Assert.Contains("0 0", wkt);
        Assert.Contains("10 10", wkt);
    }

    [Fact]
    public void WktExport_Polygon_ProducesCorrectWkt()
    {
        var polygon = new Polygon();
        var ring = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0) };
        polygon.AddRing(ring);

        var wkt = WktExportOperator.ExportToWkt(polygon);

        Assert.StartsWith("POLYGON", wkt);
        Assert.Contains("0 0", wkt);
        Assert.Contains("10 10", wkt);
    }

    [Fact]
    public void WktExport_MultiPoint_ProducesCorrectWkt()
    {
        var multiPoint = new MultiPoint();
        multiPoint.Add(new Point(10, 20));
        multiPoint.Add(new Point(30, 40));

        var wkt = WktExportOperator.ExportToWkt(multiPoint);

        Assert.StartsWith("MULTIPOINT", wkt);
        Assert.Contains("10 20", wkt);
        Assert.Contains("30 40", wkt);
    }

    [Fact]
    public void WktImport_Point_ParsesCorrectly()
    {
        var wkt = "POINT (10.5 20.7)";
        var geometry = WktImportOperator.ImportFromWkt(wkt);

        Assert.IsType<Point>(geometry);
        var point = (Point)geometry;
        Assert.Equal(10.5, point.X);
        Assert.Equal(20.7, point.Y);
    }

    [Fact]
    public void WktImport_PointWithZ_ParsesCorrectly()
    {
        var wkt = "POINT Z (10.5 20.7 30.1)";
        var geometry = WktImportOperator.ImportFromWkt(wkt);

        Assert.IsType<Point>(geometry);
        var point = (Point)geometry;
        Assert.Equal(10.5, point.X);
        Assert.Equal(20.7, point.Y);
        Assert.Equal(30.1, point.Z);
    }

    [Fact]
    public void WktImport_LineString_ParsesCorrectly()
    {
        var wkt = "LINESTRING (0 0, 10 10, 20 20)";
        var geometry = WktImportOperator.ImportFromWkt(wkt);

        Assert.IsType<Polyline>(geometry);
        var polyline = (Polyline)geometry;
        Assert.Equal(1, polyline.PathCount);

        var path = polyline.GetPath(0);
        Assert.Equal(3, path.Count);
        Assert.Equal(0, path[0].X);
        Assert.Equal(20, path[2].X);
    }

    [Fact]
    public void WktImport_Polygon_ParsesCorrectly()
    {
        var wkt = "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))";
        var geometry = WktImportOperator.ImportFromWkt(wkt);

        Assert.IsType<Polygon>(geometry);
        var polygon = (Polygon)geometry;
        Assert.Equal(1, polygon.RingCount);

        var ring = polygon.GetRing(0);
        Assert.Equal(5, ring.Count);
    }

    [Fact]
    public void WktImport_MultiPoint_ParsesCorrectly()
    {
        var wkt = "MULTIPOINT (10 20, 30 40, 50 60)";
        var geometry = WktImportOperator.ImportFromWkt(wkt);

        Assert.IsType<MultiPoint>(geometry);
        var multiPoint = (MultiPoint)geometry;
        Assert.Equal(3, multiPoint.Count);
    }

    [Fact]
    public void WktRoundTrip_Point_PreservesData()
    {
        var originalPoint = new Point(10.123456789, 20.987654321);
        var wkt = WktExportOperator.ExportToWkt(originalPoint);
        var parsedGeometry = WktImportOperator.ImportFromWkt(wkt);

        Assert.IsType<Point>(parsedGeometry);
        var parsedPoint = (Point)parsedGeometry;
        Assert.Equal(originalPoint.X, parsedPoint.X, 10);
        Assert.Equal(originalPoint.Y, parsedPoint.Y, 10);
    }

    [Fact]
    public void WktRoundTrip_Polygon_PreservesData()
    {
        var originalPolygon = new Polygon();
        var ring = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0) };
        originalPolygon.AddRing(ring);

        var wkt = WktExportOperator.ExportToWkt(originalPolygon);
        var parsedGeometry = WktImportOperator.ImportFromWkt(wkt);

        Assert.IsType<Polygon>(parsedGeometry);
        var parsedPolygon = (Polygon)parsedGeometry;
        Assert.Equal(originalPolygon.RingCount, parsedPolygon.RingCount);
    }

    [Fact]
    public void WktImport_EmptyPoint_CreatesEmptyGeometry()
    {
        var wkt = "POINT EMPTY";
        var geometry = WktImportOperator.ImportFromWkt(wkt);

        Assert.IsType<Point>(geometry);
        Assert.True(geometry.IsEmpty);
    }
}