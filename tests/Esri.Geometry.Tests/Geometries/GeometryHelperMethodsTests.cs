using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Tests.Geometries;

public class GeometryHelperMethodsTests
{
  [Fact]
  public void TestCalculateArea2DForPolygon()
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

    var area = polygon.CalculateArea2D();
    Assert.Equal(100.0, area);
  }

  [Fact]
  public void TestCalculateArea2DForEnvelope()
  {
    var envelope = new Envelope(0, 0, 10, 20);

    var area = envelope.CalculateArea2D();
    Assert.Equal(200.0, area);
  }

  [Fact]
  public void TestCalculateArea2DForPoint()
  {
    var point = new Point(10, 20);

    var area = point.CalculateArea2D();
    Assert.Equal(0.0, area);
  }

  [Fact]
  public void TestCalculateLength2DForPolyline()
  {
    var polyline = new Polyline();
    polyline.AddPath(new[]
    {
      new Point(0, 0),
      new Point(3, 0),
      new Point(3, 4)
    });

    var length = polyline.CalculateLength2D();
    Assert.Equal(7.0, length);
  }

  [Fact]
  public void TestCalculateLength2DForPolygon()
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

    var length = polygon.CalculateLength2D();
    Assert.Equal(40.0, length);
  }

  [Fact]
  public void TestCalculateLength2DForLine()
  {
    var line = new Line(new Point(0, 0), new Point(3, 4));

    var length = line.CalculateLength2D();
    Assert.Equal(5.0, length);
  }

  [Fact]
  public void TestCalculateLength2DForEnvelope()
  {
    var envelope = new Envelope(0, 0, 10, 20);

    var length = envelope.CalculateLength2D();
    Assert.Equal(60.0, length); // 2 * (10 + 20)
  }

  [Fact]
  public void TestCopyPoint()
  {
    var point = new Point(10, 20, 30);
    var copy = point.Copy() as Point;

    Assert.NotNull(copy);
    Assert.NotSame(point, copy);
    Assert.Equal(point.X, copy.X);
    Assert.Equal(point.Y, copy.Y);
    Assert.Equal(point.Z, copy.Z);
  }

  [Fact]
  public void TestCopyEnvelope()
  {
    var envelope = new Envelope(0, 0, 10, 20);
    var copy = envelope.Copy() as Envelope;

    Assert.NotNull(copy);
    Assert.NotSame(envelope, copy);
    Assert.Equal(envelope.XMin, copy.XMin);
    Assert.Equal(envelope.YMin, copy.YMin);
    Assert.Equal(envelope.XMax, copy.XMax);
    Assert.Equal(envelope.YMax, copy.YMax);
  }

  [Fact]
  public void TestCopyMultiPoint()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(0, 0));
    multiPoint.Add(new Point(10, 10));

    var copy = multiPoint.Copy() as MultiPoint;

    Assert.NotNull(copy);
    Assert.NotSame(multiPoint, copy);
    Assert.Equal(multiPoint.Count, copy.Count);
  }

  [Fact]
  public void TestCopyPolyline()
  {
    var polyline = new Polyline();
    polyline.AddPath(new[]
    {
      new Point(0, 0),
      new Point(10, 10)
    });

    var copy = polyline.Copy() as Polyline;

    Assert.NotNull(copy);
    Assert.NotSame(polyline, copy);
    Assert.Equal(polyline.PathCount, copy.PathCount);
  }

  [Fact]
  public void TestCopyPolygon()
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

    var copy = polygon.Copy() as Polygon;

    Assert.NotNull(copy);
    Assert.NotSame(polygon, copy);
    Assert.Equal(polygon.RingCount, copy.RingCount);
  }

  [Fact]
  public void TestIsValid()
  {
    var point = new Point(10, 20);
    Assert.True(point.IsValid());

    var emptyPoint = new Point();
    Assert.False(emptyPoint.IsValid());
  }

  [Fact]
  public void TestIsPoint()
  {
    var point = new Point(10, 20);
    Assert.True(point.IsPoint);

    var multiPoint = new MultiPoint();
    Assert.True(multiPoint.IsPoint);

    var polyline = new Polyline();
    Assert.False(polyline.IsPoint);
  }

  [Fact]
  public void TestIsLinear()
  {
    var line = new Line(new Point(0, 0), new Point(10, 10));
    Assert.True(line.IsLinear);

    var polyline = new Polyline();
    Assert.True(polyline.IsLinear);

    var point = new Point(10, 20);
    Assert.False(point.IsLinear);
  }

  [Fact]
  public void TestIsArea()
  {
    var polygon = new Polygon();
    Assert.True(polygon.IsArea);

    var envelope = new Envelope(0, 0, 10, 10);
    Assert.True(envelope.IsArea);

    var point = new Point(10, 20);
    Assert.False(point.IsArea);
  }

  [Fact]
  public void TestCopyWithZAndM()
  {
    var point = new Point(10, 20, 30);
    point.M = 40;

    var copy = point.Copy() as Point;

    Assert.NotNull(copy);
    Assert.Equal(point.X, copy.X);
    Assert.Equal(point.Y, copy.Y);
    Assert.Equal(point.Z, copy.Z);
    Assert.Equal(point.M, copy.M);
  }
}