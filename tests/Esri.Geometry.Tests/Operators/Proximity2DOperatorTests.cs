using Esri.Geometry.Core;
using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators;

public class Proximity2DOperatorTests
{
  [Fact]
  public void TestGetNearestCoordinateFromPoint()
  {
    var point = new Point(10, 20);
    var inputPoint = new Point(15, 25);

    var result = Proximity2DOperator.Instance.GetNearestCoordinate(point, inputPoint);

    Assert.False(result.IsEmpty);
    Assert.Equal(10, result.Coordinate.X);
    Assert.Equal(20, result.Coordinate.Y);
    Assert.Equal(0, result.VertexIndex);
  }

  [Fact]
  public void TestGetNearestVertexFromMultiPoint()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(0, 0));
    multiPoint.Add(new Point(10, 10));
    multiPoint.Add(new Point(20, 20));

    var inputPoint = new Point(11, 11);

    var result = Proximity2DOperator.Instance.GetNearestVertex(multiPoint, inputPoint);

    Assert.False(result.IsEmpty);
    Assert.Equal(1, result.VertexIndex);
    Assert.Equal(10, result.Coordinate.X);
    Assert.Equal(10, result.Coordinate.Y);
  }

  [Fact]
  public void TestGetNearestCoordinateFromEnvelope()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    var inputPoint = new Point(15, 5);

    var result = Proximity2DOperator.Instance.GetNearestCoordinate(envelope, inputPoint);

    Assert.False(result.IsEmpty);
    Assert.Equal(10, result.Coordinate.X);
    Assert.Equal(5, result.Coordinate.Y);
  }

  [Fact]
  public void TestGetNearestCoordinateFromEnvelopeInterior()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    var inputPoint = new Point(5, 5);

    var result = Proximity2DOperator.Instance.GetNearestCoordinate(envelope, inputPoint, true);

    Assert.False(result.IsEmpty);
    Assert.Equal(5, result.Coordinate.X);
    Assert.Equal(5, result.Coordinate.Y);
    Assert.Equal(0, result.Distance);
  }

  [Fact]
  public void TestGetNearestCoordinateFromPolyline()
  {
    var polyline = new Polyline();
    polyline.AddPath(new[]
    {
      new Point(0, 0),
      new Point(10, 0),
      new Point(10, 10)
    });

    var inputPoint = new Point(5, 5);

    var result = Proximity2DOperator.Instance.GetNearestCoordinate(polyline, inputPoint);

    Assert.False(result.IsEmpty);
    // Should be closest to the horizontal segment from (0,0) to (10,0)
    Assert.True(result.Distance > 0);
  }

  [Fact]
  public void TestGetNearestVertexFromPolyline()
  {
    var polyline = new Polyline();
    polyline.AddPath(new[]
    {
      new Point(0, 0),
      new Point(10, 0),
      new Point(10, 10)
    });

    var inputPoint = new Point(9, 1);

    var result = Proximity2DOperator.Instance.GetNearestVertex(polyline, inputPoint);

    Assert.False(result.IsEmpty);
    Assert.Equal(1, result.VertexIndex);
    Assert.Equal(10, result.Coordinate.X);
    Assert.Equal(0, result.Coordinate.Y);
  }

  [Fact]
  public void TestGetNearestVertices()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(0, 0));
    multiPoint.Add(new Point(10, 10));
    multiPoint.Add(new Point(20, 20));
    multiPoint.Add(new Point(30, 30));

    var inputPoint = new Point(15, 15);

    var results = Proximity2DOperator.Instance.GetNearestVertices(multiPoint, inputPoint, 10.0);

    // Should find the two closest points within radius 10
    Assert.NotEmpty(results);
    Assert.True(results.Length <= 2);
    Assert.All(results, r => Assert.True(r.Distance <= 10.0));
  }

  [Fact]
  public void TestGetNearestVerticesMaxCount()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(0, 0));
    multiPoint.Add(new Point(1, 1));
    multiPoint.Add(new Point(2, 2));
    multiPoint.Add(new Point(3, 3));

    var inputPoint = new Point(0, 0);

    var results = Proximity2DOperator.Instance.GetNearestVertices(multiPoint, inputPoint, 100.0, 2);

    Assert.Equal(2, results.Length);
  }

  [Fact]
  public void TestGetNearestCoordinateFromEmptyGeometry()
  {
    var multiPoint = new MultiPoint();
    var inputPoint = new Point(5, 5);

    var result = Proximity2DOperator.Instance.GetNearestCoordinate(multiPoint, inputPoint);

    Assert.True(result.IsEmpty);
  }

  [Fact]
  public void TestGetNearestCoordinateFromPolygonExterior()
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

    var inputPoint = new Point(15, 5);

    var result = Proximity2DOperator.Instance.GetNearestCoordinate(polygon, inputPoint, false);

    Assert.False(result.IsEmpty);
    Assert.True(result.Distance > 0);
  }

  [Fact]
  public void TestGetNearestCoordinateFromPolygonInterior()
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

    var inputPoint = new Point(5, 5);

    var result = Proximity2DOperator.Instance.GetNearestCoordinate(polygon, inputPoint, true);

    Assert.False(result.IsEmpty);
    Assert.Equal(5, result.Coordinate.X);
    Assert.Equal(5, result.Coordinate.Y);
    Assert.Equal(0, result.Distance);
  }

  [Fact]
  public void TestGetNearestCoordinateUsingGeometryEngine()
  {
    var point = new Point(10, 20);
    var inputPoint = new Point(15, 25);

    var result = GeometryEngine.GetNearestCoordinate(point, inputPoint);

    Assert.False(result.IsEmpty);
    Assert.Equal(10, result.Coordinate.X);
    Assert.Equal(20, result.Coordinate.Y);
  }

  [Fact]
  public void TestGetNearestVertexUsingGeometryEngine()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(0, 0));
    multiPoint.Add(new Point(10, 10));

    var inputPoint = new Point(11, 11);

    var result = GeometryEngine.GetNearestVertex(multiPoint, inputPoint);

    Assert.False(result.IsEmpty);
    Assert.Equal(1, result.VertexIndex);
  }

  [Fact]
  public void TestGetNearestVerticesUsingGeometryEngine()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(0, 0));
    multiPoint.Add(new Point(10, 10));
    multiPoint.Add(new Point(20, 20));

    var inputPoint = new Point(15, 15);

    var results = GeometryEngine.GetNearestVertices(multiPoint, inputPoint, 10.0);

    Assert.NotEmpty(results);
  }
}