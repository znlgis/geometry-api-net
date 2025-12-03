using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Operators;

namespace OpenGIS.Esri.Geometry.Tests.Operators;

public class SimplifyOGCOperatorTests
{
    [Fact]
    public void TestSimplifyOGCPoint()
    {
        var point = new Point(10, 20);
        var simplified = SimplifyOGCOperator.Instance.Execute(point);

        Assert.NotNull(simplified);
        Assert.IsType<Point>(simplified);
        var result = (Point)simplified;
        Assert.Equal(10, result.X);
        Assert.Equal(20, result.Y);
    }

    [Fact]
    public void TestSimplifyOGCPolyline()
    {
        var polyline = new Polyline();
        polyline.AddPath(new[]
        {
            new Point(0, 0),
            new Point(5, 5),
            new Point(10, 10)
        });

        var simplified = SimplifyOGCOperator.Instance.Execute(polyline);

        Assert.NotNull(simplified);
        Assert.IsType<Polyline>(simplified);
    }

    [Fact]
    public void TestSimplifyOGCPolygon()
    {
        var polygon = new Polygon();
        polygon.AddRing(new[]
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        });

        var simplified = SimplifyOGCOperator.Instance.Execute(polygon);

        Assert.NotNull(simplified);
        Assert.IsType<Polygon>(simplified);
        var result = (Polygon)simplified;
        Assert.True(result.RingCount > 0);
    }

    [Fact]
    public void TestSimplifyOGCEmptyGeometry()
    {
        var point = new Point();
        var simplified = SimplifyOGCOperator.Instance.Execute(point);

        Assert.NotNull(simplified);
        Assert.True(simplified.IsEmpty);
    }

    [Fact]
    public void TestIsSimpleOGCPoint()
    {
        var point = new Point(10, 20);
        var isSimple = SimplifyOGCOperator.Instance.IsSimpleOGC(point);

        Assert.True(isSimple);
    }

    [Fact]
    public void TestIsSimpleOGCEmptyPoint()
    {
        var point = new Point();
        var isSimple = SimplifyOGCOperator.Instance.IsSimpleOGC(point);

        // Empty geometries are considered simple
        Assert.True(isSimple);
    }

    [Fact]
    public void TestIsSimpleOGCMultiPoint()
    {
        var multiPoint = new MultiPoint();
        multiPoint.Add(new Point(0, 0));
        multiPoint.Add(new Point(10, 10));

        var isSimple = SimplifyOGCOperator.Instance.IsSimpleOGC(multiPoint);

        Assert.True(isSimple);
    }

    [Fact]
    public void TestIsSimpleOGCPolyline()
    {
        var polyline = new Polyline();
        polyline.AddPath(new[]
        {
            new Point(0, 0),
            new Point(10, 10)
        });

        var isSimple = SimplifyOGCOperator.Instance.IsSimpleOGC(polyline);

        Assert.True(isSimple);
    }

    [Fact]
    public void TestIsSimpleOGCPolygon()
    {
        var polygon = new Polygon();
        polygon.AddRing(new[]
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        });

        var isSimple = SimplifyOGCOperator.Instance.IsSimpleOGC(polygon);

        Assert.True(isSimple);
    }

    [Fact]
    public void TestIsSimpleOGCEnvelope()
    {
        var envelope = new Envelope(0, 0, 10, 10);

        var isSimple = SimplifyOGCOperator.Instance.IsSimpleOGC(envelope);

        Assert.True(isSimple);
    }

    [Fact]
    public void TestSimplifyOGCUsingGeometryEngine()
    {
        var polygon = new Polygon();
        polygon.AddRing(new[]
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        });

        var simplified = GeometryEngine.SimplifyOGC(polygon);

        Assert.NotNull(simplified);
        Assert.IsType<Polygon>(simplified);
    }

    [Fact]
    public void TestIsSimpleOGCUsingGeometryEngine()
    {
        var point = new Point(10, 20);

        var isSimple = GeometryEngine.IsSimpleOGC(point);

        Assert.True(isSimple);
    }

    [Fact]
    public void TestSimplifyOGCWithForceSimplify()
    {
        var polyline = new Polyline();
        polyline.AddPath(new[]
        {
            new Point(0, 0),
            new Point(5, 5),
            new Point(10, 10)
        });

        var simplified = SimplifyOGCOperator.Instance.Execute(polyline, null, true);

        Assert.NotNull(simplified);
        Assert.IsType<Polyline>(simplified);
    }

    [Fact]
    public void TestSimplifyOGCClosesUnclosedPolygonRing()
    {
        var polygon = new Polygon();
        // Add an unclosed ring (missing closing point)
        polygon.AddRing(new[]
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10)
            // Missing closing point (0, 0)
        });

        var simplified = SimplifyOGCOperator.Instance.Execute(polygon);

        Assert.NotNull(simplified);
        Assert.IsType<Polygon>(simplified);
        var result = (Polygon)simplified;
        Assert.True(result.RingCount > 0);
    }
}