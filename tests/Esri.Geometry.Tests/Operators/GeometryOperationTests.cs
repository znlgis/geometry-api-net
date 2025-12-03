using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators;

public class GeometryOperationTests
{
  [Fact]
  public void BufferOperator_Point_CreatesSquareBuffer()
  {
    var point = new Point(10, 20);
    var buffer = BufferOperator.Instance.Execute(point, 5);

    Assert.NotNull(buffer);
    Assert.False(buffer.IsEmpty);
    Assert.Equal(1, buffer.RingCount);

    var envelope = buffer.GetEnvelope();
    Assert.Equal(5, envelope.XMin);
    Assert.Equal(15, envelope.YMin);
    Assert.Equal(15, envelope.XMax);
    Assert.Equal(25, envelope.YMax);
  }

  [Fact]
  public void BufferOperator_Envelope_ExpandsCorrectly()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    var buffer = BufferOperator.Instance.Execute(envelope, 5);

    Assert.NotNull(buffer);
    var resultEnv = buffer.GetEnvelope();
    Assert.Equal(-5, resultEnv.XMin);
    Assert.Equal(-5, resultEnv.YMin);
    Assert.Equal(15, resultEnv.XMax);
    Assert.Equal(15, resultEnv.YMax);
  }

  [Fact]
  public void ConvexHullOperator_SinglePoint_ReturnsPoint()
  {
    var point = new Point(10, 20);
    var hull = ConvexHullOperator.Instance.Execute(point);

    Assert.IsType<Point>(hull);
    var resultPoint = (Point)hull;
    Assert.Equal(10, resultPoint.X);
    Assert.Equal(20, resultPoint.Y);
  }

  [Fact]
  public void ConvexHullOperator_TwoPoints_ReturnsLine()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(0, 0));
    multiPoint.Add(new Point(10, 10));

    var hull = ConvexHullOperator.Instance.Execute(multiPoint);

    Assert.IsType<Line>(hull);
  }

  [Fact]
  public void ConvexHullOperator_MultiplePoints_ReturnsPolygon()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(0, 0));
    multiPoint.Add(new Point(10, 0));
    multiPoint.Add(new Point(5, 10));
    multiPoint.Add(new Point(5, 5)); // Interior point

    var hull = ConvexHullOperator.Instance.Execute(multiPoint);

    Assert.IsType<Polygon>(hull);
    var polygon = (Polygon)hull;
    Assert.Equal(1, polygon.RingCount);

    // The hull should only contain the exterior points (triangle)
    var ring = polygon.GetRing(0);
    Assert.Equal(4, ring.Count); // 3 vertices + closing point
  }

  [Fact]
  public void ConvexHullOperator_Envelope_ReturnsRectangle()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    var hull = ConvexHullOperator.Instance.Execute(envelope);

    Assert.IsType<Polygon>(hull);
    var polygon = (Polygon)hull;
    Assert.Equal(1, polygon.RingCount);
  }

  [Fact]
  public void AreaOperator_Polygon_CalculatesArea()
  {
    var polygon = new Polygon();
    var ring = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0) };
    polygon.AddRing(ring);

    var area = AreaOperator.Instance.Execute(polygon);
    Assert.Equal(100, area, 2);
  }

  [Fact]
  public void AreaOperator_Envelope_CalculatesArea()
  {
    var envelope = new Envelope(0, 0, 10, 10);

    var area = AreaOperator.Instance.Execute(envelope);
    Assert.Equal(100, area);
  }

  [Fact]
  public void AreaOperator_Point_ReturnsZero()
  {
    var point = new Point(10, 20);

    var area = AreaOperator.Instance.Execute(point);
    Assert.Equal(0, area);
  }

  [Fact]
  public void LengthOperator_Polyline_CalculatesLength()
  {
    var polyline = new Polyline();
    var path = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10) };
    polyline.AddPath(path);

    var length = LengthOperator.Instance.Execute(polyline);
    Assert.Equal(20, length, 2);
  }

  [Fact]
  public void LengthOperator_Line_CalculatesLength()
  {
    var line = new Line(new Point(0, 0), new Point(3, 4));

    var length = LengthOperator.Instance.Execute(line);
    Assert.Equal(5, length, 2);
  }

  [Fact]
  public void LengthOperator_Envelope_CalculatesPerimeter()
  {
    var envelope = new Envelope(0, 0, 10, 10);

    var length = LengthOperator.Instance.Execute(envelope);
    Assert.Equal(40, length);
  }
}