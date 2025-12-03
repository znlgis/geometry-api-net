using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators;

public class GeodesicAreaAndOffsetOperatorTests
{
  [Fact]
  public void TestGeodesicAreaOfPolygon()
  {
    // Create a small polygon (approximately 1 degree x 1 degree near equator)
    var polygon = new Polygon();
    polygon.AddRing(new List<Point>
    {
      new(0, 0),
      new(1, 0),
      new(1, 1),
      new(0, 1),
      new(0, 0)
    });

    var area = GeodesicAreaOperator.Instance.Execute(polygon);

    // Area should be approximately 12,364,000,000 square meters (1 degree x 1 degree at equator)
    Assert.True(area > 12_000_000_000 && area < 13_000_000_000,
      $"Expected area between 12-13 billion sq meters, got {area}");
  }

  [Fact]
  public void TestGeodesicAreaOfEnvelope()
  {
    // Create an envelope (1 degree x 1 degree)
    var envelope = new Envelope(0, 0, 1, 1);

    var area = GeodesicAreaOperator.Instance.Execute(envelope);

    // Should be similar to polygon test
    Assert.True(area > 12_000_000_000 && area < 13_000_000_000);
  }

  [Fact]
  public void TestGeodesicAreaOfEmptyGeometry()
  {
    var emptyPolygon = new Polygon();
    var area = GeodesicAreaOperator.Instance.Execute(emptyPolygon);
    Assert.Equal(0.0, area);
  }

  [Fact]
  public void TestGeodesicAreaOfPoint()
  {
    // Points have zero area
    var point = new Point(10, 20);
    var area = GeodesicAreaOperator.Instance.Execute(point);
    Assert.Equal(0.0, area);
  }

  [Fact]
  public void TestOffsetPolygonOutward()
  {
    var polygon = new Polygon();
    polygon.AddRing(new List<Point>
    {
      new(0, 0),
      new(10, 0),
      new(10, 10),
      new(0, 10),
      new(0, 0)
    });

    var offset = OffsetOperator.Instance.Execute(polygon, 1.0);

    Assert.NotNull(offset);
    Assert.IsType<Polygon>(offset);
    Assert.False(offset.IsEmpty);
  }

  [Fact]
  public void TestOffsetPolyline()
  {
    var polyline = new Polyline();
    polyline.AddPath(new List<Point>
    {
      new(0, 0),
      new(10, 0),
      new(10, 10)
    });

    var offset = OffsetOperator.Instance.Execute(polyline, 2.0);

    Assert.NotNull(offset);
    Assert.IsType<Polyline>(offset);
    Assert.False(offset.IsEmpty);
  }

  [Fact]
  public void TestOffsetEnvelope()
  {
    var envelope = new Envelope(0, 0, 10, 10);

    var offset = OffsetOperator.Instance.Execute(envelope, 5.0);

    Assert.NotNull(offset);
    var offsetEnv = Assert.IsType<Envelope>(offset);
    Assert.Equal(-5.0, offsetEnv.XMin);
    Assert.Equal(-5.0, offsetEnv.YMin);
    Assert.Equal(15.0, offsetEnv.XMax);
    Assert.Equal(15.0, offsetEnv.YMax);
  }

  [Fact]
  public void TestOffsetPoint()
  {
    // Point offset returns buffer (which is a Polygon)
    var point = new Point(5, 5);

    var offset = OffsetOperator.Instance.Execute(point, 3.0);

    Assert.NotNull(offset);
    Assert.IsType<Polygon>(offset); // Buffer returns polygon
  }

  [Fact]
  public void TestOffsetEmptyGeometry()
  {
    var emptyPolygon = new Polygon();

    var offset = OffsetOperator.Instance.Execute(emptyPolygon, 1.0);

    Assert.NotNull(offset);
    Assert.True(offset.IsEmpty);
  }
}