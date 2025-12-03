using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators;

public class GeneralizeDensifyOperatorTests
{
  [Fact]
  public void GeneralizeOperator_PolylineWithManyPoints_RemovesVertices()
  {
    // Create a polyline with many closely spaced points
    var polyline = new Polyline();
    polyline.AddPath(new List<Point>
    {
      new(0, 0),
      new(1, 0.1),
      new(2, 0.2),
      new(3, 0.1),
      new(4, 0),
      new(5, 0)
    });

    var result = GeneralizeOperator.Instance.Execute(polyline, 0.5);

    Assert.IsType<Polyline>(result);
    var generalizedPolyline = (Polyline)result;
    Assert.Equal(1, generalizedPolyline.PathCount);

    // Should have fewer points than original (6 points originally)
    Assert.True(generalizedPolyline.GetPath(0).Count < 6);
  }

  [Fact]
  public void GeneralizeOperator_Polygon_PreservesShape()
  {
    // Create a polygon with many points
    var polygon = new Polygon();
    polygon.AddRing(new List<Point>
    {
      new(0, 0),
      new(1, 0),
      new(2, 0.1),
      new(3, 0),
      new(4, 0),
      new(4, 4),
      new(0, 4),
      new(0, 0)
    });

    var result = GeneralizeOperator.Instance.Execute(polygon, 0.5);

    Assert.IsType<Polygon>(result);
    var generalizedPolygon = (Polygon)result;
    Assert.Equal(1, generalizedPolygon.RingCount);

    // Should have fewer points (8 originally)
    Assert.True(generalizedPolygon.GetRing(0).Count < 8);

    // First and last points should still match (closed ring)
    var generalizedRing = generalizedPolygon.GetRing(0);
    Assert.Equal(generalizedRing[0].X, generalizedRing[generalizedRing.Count - 1].X);
    Assert.Equal(generalizedRing[0].Y, generalizedRing[generalizedRing.Count - 1].Y);
  }

  [Fact]
  public void GeneralizeOperator_MultiPoint_RemovesClosePoints()
  {
    var points = new List<Point>
    {
      new(0, 0),
      new(0.1, 0.1),
      new(5, 5),
      new(5.1, 5.1),
      new(10, 10)
    };
    var multiPoint = new MultiPoint(points);

    var result = GeneralizeOperator.Instance.Execute(multiPoint, 0.5);

    Assert.IsType<MultiPoint>(result);
    var generalizedMultiPoint = (MultiPoint)result;

    // Should have removed close points (5 originally)
    Assert.True(generalizedMultiPoint.Count < 5);
  }

  [Fact]
  public void GeneralizeOperator_Point_RemainsUnchanged()
  {
    var point = new Point(10, 20);

    var result = GeneralizeOperator.Instance.Execute(point, 1.0);

    Assert.IsType<Point>(result);
    var generalizedPoint = (Point)result;
    Assert.Equal(10, generalizedPoint.X);
    Assert.Equal(20, generalizedPoint.Y);
  }

  [Fact]
  public void DensifyOperator_Line_AddsIntermediatePoints()
  {
    var line = new Line(new Point(0, 0), new Point(10, 0));

    var result = DensifyOperator.Instance.Execute(line, 2.0);

    Assert.IsType<Polyline>(result);
    var densifiedPolyline = (Polyline)result;
    Assert.Equal(1, densifiedPolyline.PathCount);

    // Should have added points (10/2 = 5 segments = 6 points)
    Assert.True(densifiedPolyline.GetPath(0).Count >= 5);
  }

  [Fact]
  public void DensifyOperator_Polyline_AddsVertices()
  {
    var polyline = new Polyline();
    polyline.AddPath(new List<Point>
    {
      new(0, 0),
      new(10, 0),
      new(10, 10)
    });

    var result = DensifyOperator.Instance.Execute(polyline, 3.0);

    Assert.IsType<Polyline>(result);
    var densifiedPolyline = (Polyline)result;
    Assert.Equal(1, densifiedPolyline.PathCount);

    // Should have more points than original (3 originally)
    Assert.True(densifiedPolyline.GetPath(0).Count > 3);
  }

  [Fact]
  public void DensifyOperator_Polygon_MaintainsClosedRing()
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

    var result = DensifyOperator.Instance.Execute(polygon, 3.0);

    Assert.IsType<Polygon>(result);
    var densifiedPolygon = (Polygon)result;
    Assert.Equal(1, densifiedPolygon.RingCount);

    // Should have more points (5 originally)
    Assert.True(densifiedPolygon.GetRing(0).Count > 5);

    // Ring should still be closed
    var densifiedRing = densifiedPolygon.GetRing(0);
    Assert.Equal(densifiedRing[0].X, densifiedRing[densifiedRing.Count - 1].X);
    Assert.Equal(densifiedRing[0].Y, densifiedRing[densifiedRing.Count - 1].Y);
  }

  [Fact]
  public void DensifyOperator_Point_RemainsUnchanged()
  {
    var point = new Point(10, 20);

    var result = DensifyOperator.Instance.Execute(point, 1.0);

    Assert.IsType<Point>(result);
    Assert.Equal(10, ((Point)result).X);
    Assert.Equal(20, ((Point)result).Y);
  }

  [Fact]
  public void DensifyOperator_WithZCoordinates_PreservesZ()
  {
    var line = new Line(new Point(0, 0, 0), new Point(10, 0, 10));

    var result = DensifyOperator.Instance.Execute(line, 3.0);

    Assert.IsType<Polyline>(result);
    var densifiedPolyline = (Polyline)result;

    // Check that intermediate points have interpolated Z values
    var densifiedPath = densifiedPolyline.GetPath(0);
    Assert.True(densifiedPath.Count > 2);

    // Check middle point has intermediate Z value
    if (densifiedPath.Count > 2)
    {
      var middlePoint = densifiedPath[densifiedPath.Count / 2];
      Assert.True(middlePoint.Z.HasValue);
      Assert.True(middlePoint.Z.Value > 0 && middlePoint.Z.Value < 10);
    }
  }

  [Fact]
  public void GeneralizeAndDensify_RoundTrip_ProducesReasonableResult()
  {
    // Start with a simple polyline
    var polyline = new Polyline();
    polyline.AddPath(new List<Point>
    {
      new(0, 0),
      new(10, 0),
      new(20, 0)
    });

    // Densify it
    var densified = DensifyOperator.Instance.Execute(polyline, 2.0);
    var densifiedPolyline = (Polyline)densified;
    Assert.True(densifiedPolyline.GetPath(0).Count > 3);

    // Then generalize it back
    var generalized = GeneralizeOperator.Instance.Execute(densified, 0.1);
    var generalizedPolyline = (Polyline)generalized;

    // Should have reduced some points but maintained general structure
    Assert.True(generalizedPolyline.GetPath(0).Count >= 2);
  }
}