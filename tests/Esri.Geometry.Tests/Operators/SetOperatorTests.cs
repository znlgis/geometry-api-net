using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators;

public class SetOperatorTests
{
  [Fact]
  public void UnionOperator_TwoPoints_CreatesMultiPoint()
  {
    var p1 = new Point(0, 0);
    var p2 = new Point(10, 10);

    var result = UnionOperator.Instance.Execute(p1, p2);

    Assert.IsType<MultiPoint>(result);
    var mp = (MultiPoint)result;
    Assert.Equal(2, mp.Count);
  }

  [Fact]
  public void UnionOperator_SamePoints_ReturnsSinglePoint()
  {
    var p1 = new Point(5, 5);
    var p2 = new Point(5, 5);

    var result = UnionOperator.Instance.Execute(p1, p2);

    Assert.IsType<Point>(result);
    var point = (Point)result;
    Assert.Equal(5, point.X);
    Assert.Equal(5, point.Y);
  }

  [Fact]
  public void UnionOperator_TwoEnvelopes_CreatesUnionEnvelope()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(5, 5, 15, 15);

    var result = UnionOperator.Instance.Execute(env1, env2);

    Assert.IsType<Envelope>(result);
    var envelope = (Envelope)result;
    Assert.Equal(0, envelope.XMin);
    Assert.Equal(0, envelope.YMin);
    Assert.Equal(15, envelope.XMax);
    Assert.Equal(15, envelope.YMax);
  }

  [Fact]
  public void UnionOperator_PointAndMultiPoint_CreatesLargerMultiPoint()
  {
    var point = new Point(0, 0);
    var mp = new MultiPoint();
    mp.Add(new Point(5, 5));
    mp.Add(new Point(10, 10));

    var result = UnionOperator.Instance.Execute(point, mp);

    Assert.IsType<MultiPoint>(result);
    var multiPoint = (MultiPoint)result;
    Assert.Equal(3, multiPoint.Count);
  }

  [Fact]
  public void IntersectionOperator_TwoSamePoints_ReturnsPoint()
  {
    var p1 = new Point(5, 5);
    var p2 = new Point(5, 5);

    var result = IntersectionOperator.Instance.Execute(p1, p2);

    Assert.IsType<Point>(result);
    var point = (Point)result;
    Assert.Equal(5, point.X);
    Assert.Equal(5, point.Y);
  }

  [Fact]
  public void IntersectionOperator_TwoDifferentPoints_ReturnsEmpty()
  {
    var p1 = new Point(0, 0);
    var p2 = new Point(10, 10);

    var result = IntersectionOperator.Instance.Execute(p1, p2);

    Assert.True(result.IsEmpty);
  }

  [Fact]
  public void IntersectionOperator_PointInEnvelope_ReturnsPoint()
  {
    var point = new Point(5, 5);
    var envelope = new Envelope(0, 0, 10, 10);

    var result = IntersectionOperator.Instance.Execute(point, envelope);

    Assert.IsType<Point>(result);
    var resultPoint = (Point)result;
    Assert.Equal(5, resultPoint.X);
    Assert.Equal(5, resultPoint.Y);
  }

  [Fact]
  public void IntersectionOperator_PointOutsideEnvelope_ReturnsEmpty()
  {
    var point = new Point(15, 15);
    var envelope = new Envelope(0, 0, 10, 10);

    var result = IntersectionOperator.Instance.Execute(point, envelope);

    Assert.True(result.IsEmpty);
  }

  [Fact]
  public void IntersectionOperator_TwoEnvelopes_ReturnsIntersection()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(5, 5, 15, 15);

    var result = IntersectionOperator.Instance.Execute(env1, env2);

    Assert.IsType<Envelope>(result);
    var envelope = (Envelope)result;
    Assert.Equal(5, envelope.XMin);
    Assert.Equal(5, envelope.YMin);
    Assert.Equal(10, envelope.XMax);
    Assert.Equal(10, envelope.YMax);
  }

  [Fact]
  public void IntersectionOperator_NonIntersectingEnvelopes_ReturnsEmpty()
  {
    var env1 = new Envelope(0, 0, 5, 5);
    var env2 = new Envelope(10, 10, 15, 15);

    var result = IntersectionOperator.Instance.Execute(env1, env2);

    Assert.True(result.IsEmpty);
  }

  [Fact]
  public void IntersectionOperator_MultiPointWithEnvelope_ReturnsPointsInside()
  {
    var mp = new MultiPoint();
    mp.Add(new Point(2, 2)); // inside
    mp.Add(new Point(5, 5)); // inside
    mp.Add(new Point(12, 12)); // outside

    var envelope = new Envelope(0, 0, 10, 10);

    var result = IntersectionOperator.Instance.Execute(mp, envelope);

    Assert.IsType<MultiPoint>(result);
    var multiPoint = (MultiPoint)result;
    Assert.Equal(2, multiPoint.Count);
  }

  [Fact]
  public void DifferenceOperator_TwoSamePoints_ReturnsEmpty()
  {
    var p1 = new Point(5, 5);
    var p2 = new Point(5, 5);

    var result = DifferenceOperator.Instance.Execute(p1, p2);

    Assert.True(result.IsEmpty);
  }

  [Fact]
  public void DifferenceOperator_TwoDifferentPoints_ReturnsFirstPoint()
  {
    var p1 = new Point(0, 0);
    var p2 = new Point(10, 10);

    var result = DifferenceOperator.Instance.Execute(p1, p2);

    Assert.IsType<Point>(result);
    var point = (Point)result;
    Assert.Equal(0, point.X);
    Assert.Equal(0, point.Y);
  }

  [Fact]
  public void DifferenceOperator_PointInEnvelope_ReturnsEmpty()
  {
    var point = new Point(5, 5);
    var envelope = new Envelope(0, 0, 10, 10);

    var result = DifferenceOperator.Instance.Execute(point, envelope);

    Assert.True(result.IsEmpty);
  }

  [Fact]
  public void DifferenceOperator_PointOutsideEnvelope_ReturnsPoint()
  {
    var point = new Point(15, 15);
    var envelope = new Envelope(0, 0, 10, 10);

    var result = DifferenceOperator.Instance.Execute(point, envelope);

    Assert.IsType<Point>(result);
    var resultPoint = (Point)result;
    Assert.Equal(15, resultPoint.X);
    Assert.Equal(15, resultPoint.Y);
  }

  [Fact]
  public void DifferenceOperator_MultiPointWithPoint_RemovesPoint()
  {
    var mp = new MultiPoint();
    mp.Add(new Point(0, 0));
    mp.Add(new Point(5, 5));
    mp.Add(new Point(10, 10));

    var point = new Point(5, 5);

    var result = DifferenceOperator.Instance.Execute(mp, point);

    Assert.IsType<MultiPoint>(result);
    var multiPoint = (MultiPoint)result;
    Assert.Equal(2, multiPoint.Count);
  }

  [Fact]
  public void DifferenceOperator_MultiPointWithEnvelope_ReturnsPointsOutside()
  {
    var mp = new MultiPoint();
    mp.Add(new Point(2, 2)); // inside - removed
    mp.Add(new Point(5, 5)); // inside - removed
    mp.Add(new Point(12, 12)); // outside - kept

    var envelope = new Envelope(0, 0, 10, 10);

    var result = DifferenceOperator.Instance.Execute(mp, envelope);

    Assert.IsType<Point>(result); // Single point left
    var point = (Point)result;
    Assert.Equal(12, point.X);
    Assert.Equal(12, point.Y);
  }

  [Fact]
  public void DifferenceOperator_NonIntersectingEnvelopes_ReturnsFirstEnvelope()
  {
    var env1 = new Envelope(0, 0, 5, 5);
    var env2 = new Envelope(10, 10, 15, 15);

    var result = DifferenceOperator.Instance.Execute(env1, env2);

    Assert.IsType<Envelope>(result);
    var envelope = (Envelope)result;
    Assert.Equal(0, envelope.XMin);
    Assert.Equal(0, envelope.YMin);
    Assert.Equal(5, envelope.XMax);
    Assert.Equal(5, envelope.YMax);
  }
}