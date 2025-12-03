using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators;

public class SpatialRelationshipOperatorTests
{
  [Fact]
  public void EqualsOperator_SamePoints_ReturnsTrue()
  {
    var point1 = new Point(10, 20);
    var point2 = new Point(10, 20);

    var equals = EqualsOperator.Instance.Execute(point1, point2);
    Assert.True(equals);
  }

  [Fact]
  public void EqualsOperator_DifferentPoints_ReturnsFalse()
  {
    var point1 = new Point(10, 20);
    var point2 = new Point(10, 21);

    var equals = EqualsOperator.Instance.Execute(point1, point2);
    Assert.False(equals);
  }

  [Fact]
  public void EqualsOperator_SameEnvelopes_ReturnsTrue()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(0, 0, 10, 10);

    var equals = EqualsOperator.Instance.Execute(env1, env2);
    Assert.True(equals);
  }

  [Fact]
  public void DisjointOperator_NonIntersectingEnvelopes_ReturnsTrue()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(20, 20, 30, 30);

    var disjoint = DisjointOperator.Instance.Execute(env1, env2);
    Assert.True(disjoint);
  }

  [Fact]
  public void DisjointOperator_IntersectingEnvelopes_ReturnsFalse()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(5, 5, 15, 15);

    var disjoint = DisjointOperator.Instance.Execute(env1, env2);
    Assert.False(disjoint);
  }

  [Fact]
  public void WithinOperator_PointInEnvelope_ReturnsTrue()
  {
    var point = new Point(5, 5);
    var envelope = new Envelope(0, 0, 10, 10);

    var within = WithinOperator.Instance.Execute(point, envelope);
    Assert.True(within);
  }

  [Fact]
  public void WithinOperator_PointOutsideEnvelope_ReturnsFalse()
  {
    var point = new Point(15, 15);
    var envelope = new Envelope(0, 0, 10, 10);

    var within = WithinOperator.Instance.Execute(point, envelope);
    Assert.False(within);
  }

  [Fact]
  public void WithinOperator_EnvelopeWithinEnvelope_ReturnsTrue()
  {
    var env1 = new Envelope(2, 2, 8, 8);
    var env2 = new Envelope(0, 0, 10, 10);

    var within = WithinOperator.Instance.Execute(env1, env2);
    Assert.True(within);
  }

  [Fact]
  public void TouchesOperator_PointOnEnvelopeBoundary_ReturnsTrue()
  {
    var point = new Point(0, 5);
    var envelope = new Envelope(0, 0, 10, 10);

    var touches = TouchesOperator.Instance.Execute(point, envelope);
    Assert.True(touches);
  }

  [Fact]
  public void TouchesOperator_PointInsideEnvelope_ReturnsFalse()
  {
    var point = new Point(5, 5);
    var envelope = new Envelope(0, 0, 10, 10);

    var touches = TouchesOperator.Instance.Execute(point, envelope);
    Assert.False(touches);
  }

  [Fact]
  public void TouchesOperator_EnvelopesTouchingAtEdge_ReturnsTrue()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(10, 0, 20, 10);

    var touches = TouchesOperator.Instance.Execute(env1, env2);
    Assert.True(touches);
  }

  [Fact]
  public void OverlapsOperator_OverlappingEnvelopes_ReturnsTrue()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(5, 5, 15, 15);

    var overlaps = OverlapsOperator.Instance.Execute(env1, env2);
    Assert.True(overlaps);
  }

  [Fact]
  public void OverlapsOperator_OneEnvelopeContainsOther_ReturnsFalse()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(2, 2, 8, 8);

    var overlaps = OverlapsOperator.Instance.Execute(env1, env2);
    Assert.False(overlaps);
  }

  [Fact]
  public void OverlapsOperator_NonIntersectingEnvelopes_ReturnsFalse()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(20, 20, 30, 30);

    var overlaps = OverlapsOperator.Instance.Execute(env1, env2);
    Assert.False(overlaps);
  }
}