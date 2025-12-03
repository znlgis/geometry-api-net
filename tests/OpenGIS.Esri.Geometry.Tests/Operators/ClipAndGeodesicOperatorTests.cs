using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Operators;

namespace OpenGIS.Esri.Geometry.Tests.Operators;

public class ClipAndGeodesicOperatorTests
{
    [Fact]
    public void ClipOperator_PointInsideEnvelope_ReturnsPoint()
    {
        var point = new Point(5, 5);
        var clipEnv = new Envelope(0, 0, 10, 10);

        var result = ClipOperator.Instance.Execute(point, clipEnv);

        Assert.IsType<Point>(result);
        Assert.False(result.IsEmpty);
        var clippedPoint = (Point)result;
        Assert.Equal(5, clippedPoint.X);
        Assert.Equal(5, clippedPoint.Y);
    }

    [Fact]
    public void ClipOperator_PointOutsideEnvelope_ReturnsEmpty()
    {
        var point = new Point(15, 15);
        var clipEnv = new Envelope(0, 0, 10, 10);

        var result = ClipOperator.Instance.Execute(point, clipEnv);

        Assert.IsType<Point>(result);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void ClipOperator_MultiPoint_FiltersPoints()
    {
        var multiPoint = new MultiPoint();
        multiPoint.Add(new Point(5, 5)); // Inside
        multiPoint.Add(new Point(15, 15)); // Outside
        multiPoint.Add(new Point(7, 7)); // Inside

        var clipEnv = new Envelope(0, 0, 10, 10);
        var result = ClipOperator.Instance.Execute(multiPoint, clipEnv);

        Assert.IsType<MultiPoint>(result);
        var clippedMultiPoint = (MultiPoint)result;
        Assert.Equal(2, clippedMultiPoint.Count);
    }

    [Fact]
    public void ClipOperator_Envelope_ReturnsIntersection()
    {
        var envelope = new Envelope(5, 5, 15, 15);
        var clipEnv = new Envelope(0, 0, 10, 10);

        var result = ClipOperator.Instance.Execute(envelope, clipEnv);

        Assert.IsType<Envelope>(result);
        var clippedEnv = (Envelope)result;
        Assert.Equal(5, clippedEnv.XMin);
        Assert.Equal(5, clippedEnv.YMin);
        Assert.Equal(10, clippedEnv.XMax);
        Assert.Equal(10, clippedEnv.YMax);
    }

    [Fact]
    public void ClipOperator_Line_ClipsToEnvelope()
    {
        var line = new Line(new Point(-5, 5), new Point(15, 5));
        var clipEnv = new Envelope(0, 0, 10, 10);

        var result = ClipOperator.Instance.Execute(line, clipEnv);

        // Should return a line or polyline clipped to envelope
        Assert.True(result is Line || result is Polyline);
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void ClipOperator_LineCompletelyOutside_ReturnsEmpty()
    {
        var line = new Line(new Point(-5, -5), new Point(-1, -1));
        var clipEnv = new Envelope(0, 0, 10, 10);

        var result = ClipOperator.Instance.Execute(line, clipEnv);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void ClipOperator_Polyline_ClipsSegments()
    {
        var polyline = new Polyline();
        var path = new[]
        {
            new Point(-5, 5),
            new Point(5, 5),
            new Point(15, 5)
        };
        polyline.AddPath(path);

        var clipEnv = new Envelope(0, 0, 10, 10);
        var result = ClipOperator.Instance.Execute(polyline, clipEnv);

        Assert.IsType<Polyline>(result);
        var clippedPolyline = (Polyline)result;
        Assert.True(clippedPolyline.PathCount >= 1);
    }

    [Fact]
    public void GeodesicDistanceOperator_SamePoint_ReturnsZero()
    {
        var point1 = new Point(0, 0);
        var point2 = new Point(0, 0);

        var distance = GeodesicDistanceOperator.Instance.Execute(point1, point2);

        Assert.Equal(0, distance, 1);
    }

    [Fact]
    public void GeodesicDistanceOperator_NewYorkToLondon_ReturnsCorrectDistance()
    {
        // New York: 40.7128° N, 74.0060° W
        // London: 51.5074° N, 0.1278° W
        var newYork = new Point(-74.0060, 40.7128);
        var london = new Point(-0.1278, 51.5074);

        var distance = GeodesicDistanceOperator.Instance.Execute(newYork, london);

        // Expected distance is approximately 5,570 km
        Assert.True(distance > 5500000 && distance < 5600000, $"Distance was {distance}");
    }

    [Fact]
    public void GeodesicDistanceOperator_EquatorPoints_ReturnsCorrectDistance()
    {
        // Two points on the equator, 1 degree apart
        var point1 = new Point(0, 0);
        var point2 = new Point(1, 0);

        var distance = GeodesicDistanceOperator.Instance.Execute(point1, point2);

        // 1 degree at equator is approximately 111 km
        Assert.True(distance > 110000 && distance < 112000, $"Distance was {distance}");
    }

    [Fact]
    public void GeodesicDistanceOperator_PolarPoints_HandlesHighLatitudes()
    {
        // Points near the north pole
        var point1 = new Point(0, 89);
        var point2 = new Point(180, 89);

        var distance = GeodesicDistanceOperator.Instance.Execute(point1, point2);

        // Should be a reasonable distance (not throwing exception)
        Assert.True(distance >= 0);
        Assert.True(distance < 500000); // Less than 500 km
    }
}