using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Tests.Geometries;

public class LineTests
{
    [Fact]
    public void Line_DefaultConstructor_IsEmpty()
    {
        var line = new Line();
        Assert.True(line.IsEmpty);
    }

    [Fact]
    public void Line_Type_ReturnsLine()
    {
        var line = new Line(new Point(0, 0), new Point(10, 10));
        Assert.Equal(GeometryType.Line, line.Type);
    }

    [Fact]
    public void Line_Dimension_ReturnsOne()
    {
        var line = new Line(new Point(0, 0), new Point(10, 10));
        Assert.Equal(1, line.Dimension);
    }

    [Fact]
    public void Line_WithEndpoints_IsNotEmpty()
    {
        var line = new Line(new Point(0, 0), new Point(10, 10));
        Assert.False(line.IsEmpty);
    }

    [Fact]
    public void Line_Length_CalculatesCorrectly()
    {
        var line = new Line(new Point(0, 0), new Point(3, 4));
        Assert.Equal(5, line.Length, 10);
    }

    [Fact]
    public void Line_GetEnvelope_ReturnsCorrectBounds()
    {
        var line = new Line(new Point(5, 10), new Point(15, 20));
        var envelope = line.GetEnvelope();

        Assert.Equal(5, envelope.XMin);
        Assert.Equal(10, envelope.YMin);
        Assert.Equal(15, envelope.XMax);
        Assert.Equal(20, envelope.YMax);
    }

    [Fact]
    public void Line_WithSameStartEnd_HasZeroLength()
    {
        var line = new Line(new Point(10, 10), new Point(10, 10));
        Assert.Equal(0, line.Length, 10);
    }
}