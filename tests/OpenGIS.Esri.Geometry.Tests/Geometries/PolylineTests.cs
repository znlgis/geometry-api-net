using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Tests.Geometries;

public class PolylineTests
{
    [Fact]
    public void Polyline_DefaultConstructor_IsEmpty()
    {
        var polyline = new Polyline();
        Assert.True(polyline.IsEmpty);
    }

    [Fact]
    public void Polyline_Type_ReturnsPolyline()
    {
        var polyline = new Polyline();
        Assert.Equal(GeometryType.Polyline, polyline.Type);
    }

    [Fact]
    public void Polyline_Dimension_ReturnsOne()
    {
        var polyline = new Polyline();
        Assert.Equal(1, polyline.Dimension);
    }

    [Fact]
    public void Polyline_AddPath_IncreasesPathCount()
    {
        var polyline = new Polyline();
        var path = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10) };
        polyline.AddPath(path);

        Assert.Equal(1, polyline.PathCount);
        Assert.False(polyline.IsEmpty);
    }

    [Fact]
    public void Polyline_Length_CalculatesCorrectly()
    {
        var polyline = new Polyline();
        var path = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10) };
        polyline.AddPath(path);

        // Length should be 10 + 10 = 20
        Assert.Equal(20, polyline.Length, 2);
    }

    [Fact]
    public void Polyline_MultiplePaths_CalculatesTotalLength()
    {
        var polyline = new Polyline();
        var path1 = new[] { new Point(0, 0), new Point(10, 0) };
        var path2 = new[] { new Point(20, 0), new Point(30, 0) };
        polyline.AddPath(path1);
        polyline.AddPath(path2);

        Assert.Equal(2, polyline.PathCount);
        Assert.Equal(20, polyline.Length, 2);
    }

    [Fact]
    public void Polyline_GetPath_ReturnsCorrectPath()
    {
        var polyline = new Polyline();
        var path = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10) };
        polyline.AddPath(path);

        var retrievedPath = polyline.GetPath(0);
        Assert.Equal(3, retrievedPath.Count);
        Assert.Equal(10, retrievedPath[1].X);
    }

    [Fact]
    public void Polyline_GetEnvelope_ReturnsCorrectBounds()
    {
        var polyline = new Polyline();
        var path = new[] { new Point(5, 5), new Point(15, 5), new Point(15, 15) };
        polyline.AddPath(path);

        var envelope = polyline.GetEnvelope();
        Assert.Equal(5, envelope.XMin);
        Assert.Equal(5, envelope.YMin);
        Assert.Equal(15, envelope.XMax);
        Assert.Equal(15, envelope.YMax);
    }
}