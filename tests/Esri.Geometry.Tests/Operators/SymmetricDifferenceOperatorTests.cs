using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators;

public class SymmetricDifferenceOperatorTests
{
  [Fact]
  public void TestSymmetricDifference_TwoEqualPoints_ReturnsEmpty()
  {
    var p1 = new Point(10, 20);
    var p2 = new Point(10, 20);

    var result = SymmetricDifferenceOperator.Instance.Execute(p1, p2);

    Assert.True(result.IsEmpty);
  }

  [Fact]
  public void TestSymmetricDifference_TwoDifferentPoints_ReturnsMultiPoint()
  {
    var p1 = new Point(10, 20);
    var p2 = new Point(30, 40);

    var result = SymmetricDifferenceOperator.Instance.Execute(p1, p2);

    Assert.IsType<MultiPoint>(result);
    var mp = (MultiPoint)result;
    Assert.Equal(2, mp.Count);
  }

  [Fact]
  public void TestSymmetricDifference_MultiPoints_ReturnsUniquePoints()
  {
    var mp1 = new MultiPoint();
    mp1.Add(new Point(0, 0));
    mp1.Add(new Point(10, 10));
    mp1.Add(new Point(20, 20));

    var mp2 = new MultiPoint();
    mp2.Add(new Point(10, 10));
    mp2.Add(new Point(20, 20));
    mp2.Add(new Point(30, 30));

    var result = SymmetricDifferenceOperator.Instance.Execute(mp1, mp2);

    Assert.IsType<MultiPoint>(result);
    var mp = (MultiPoint)result;
    Assert.Equal(2, mp.Count); // (0,0) from mp1 and (30,30) from mp2
  }

  [Fact]
  public void TestSymmetricDifference_PointAndMultiPoint()
  {
    var p = new Point(10, 10);
    var mp = new MultiPoint();
    mp.Add(new Point(10, 10));
    mp.Add(new Point(20, 20));

    var result = SymmetricDifferenceOperator.Instance.Execute(p, mp);

    Assert.IsType<MultiPoint>(result);
    var resultMp = (MultiPoint)result;
    Assert.Equal(1, resultMp.Count); // Only (20,20) should remain
  }

  [Fact]
  public void TestSymmetricDifference_Envelopes()
  {
    var env1 = new Envelope(0, 0, 10, 10);
    var env2 = new Envelope(5, 5, 15, 15);

    var result = SymmetricDifferenceOperator.Instance.Execute(env1, env2);

    Assert.NotNull(result);
    Assert.False(result.IsEmpty);
  }
}