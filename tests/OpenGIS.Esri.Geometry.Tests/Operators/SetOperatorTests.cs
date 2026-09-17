using System.Linq;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Operators;

namespace OpenGIS.Esri.Geometry.Tests.Operators;

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
    public void UnionOperator_TwoEnvelopes_ReturnsTrueUnionPolygon()
    {
        var env1 = new Envelope(0, 0, 10, 10);
        var env2 = new Envelope(5, 5, 15, 15);

        var result = UnionOperator.Instance.Execute(env1, env2);

        // 真布尔并集：L 形多边形（面积 175），不再是外包络矩形（225 为错误结果）。
        Assert.IsType<Polygon>(result);
        Assert.Equal(175, ((Polygon)result).CalculateArea2D(), 4); // 裁剪器抖动引入 ~1e-6 级面积误差
        var env = result.GetEnvelope();
        Assert.Equal(0, env.XMin, 6);
        Assert.Equal(15, env.XMax, 6);
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

        Assert.IsType<Polygon>(result);
        Assert.Equal(25, ((Polygon)result).CalculateArea2D(), 4); // 裁剪器抖动误差量级
        var envelope = result.GetEnvelope();
        Assert.Equal(5, envelope.XMin, 6);
        Assert.Equal(5, envelope.YMin, 6);
        Assert.Equal(10, envelope.XMax, 6);
        Assert.Equal(10, envelope.YMax, 6);
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

        // 不交时结果为第一个面（Polygon 表示，面积不变）
        Assert.Equal(25, result.CalculateArea2D(), 4);
    }

    [Fact]
    public void DifferenceOperator_IntersectingEnvelopes_ReturnsFirstEnvelope()
    {
        var env1 = new Envelope(0, 0, 10, 10);
        var env2 = new Envelope(5, 5, 15, 15);

        // 真布尔差集：L 形（100 − 25 = 75），旧实现直接返回第一个包络是错误结果。
        var result = DifferenceOperator.Instance.Execute(env1, env2);

        Assert.IsType<Polygon>(result);
        Assert.Equal(75, result.CalculateArea2D(), 4); // 裁剪器抖动误差量级
    }

    [Fact]
    public void DifferenceOperator_SecondEnvelopeContainsFirst_ReturnsEmpty()
    {
        var env1 = new Envelope(2, 2, 4, 4);
        var env2 = new Envelope(0, 0, 10, 10);

        var result = DifferenceOperator.Instance.Execute(env1, env2);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void DifferenceOperator_TwoPolylines_ReturnsUnchangedLine()
    {
        var line1 = new Polyline();
        line1.AddPath(new[] { new Point(0, 0), new Point(10, 0) });

        var line2 = new Polyline();
        line2.AddPath(new[] { new Point(0, 5), new Point(10, 5) });

        // 线差集已实现：平行不相交时原线保留
        var result = DifferenceOperator.Instance.Execute(line1, line2);

        Assert.IsType<Polyline>(result);
        Assert.Equal(10, result.CalculateLength2D(), 6);
    }

    [Fact]
    public void Union_TwoCrossingPolylines_SplitsAtIntersection()
    {
        var a = new Polyline();
        a.AddPath(new[] { new Point(0, 0), new Point(10, 0) });
        var b = new Polyline();
        b.AddPath(new[] { new Point(5, -5), new Point(5, 5) });

        var u = UnionOperator.Instance.Execute(a, b);

        // 对齐 GEOS：并集总长 20（交点处拆分，无线段重复计长）
        Assert.Equal(20, u.CalculateLength2D(), 9);
        var paths = ((Polyline)u).GetPaths().ToList();
        Assert.True(paths.Count >= 2, " crossing lines must split at intersection");
    }

    [Fact]
    public void Union_TwoOverlappingPolylines_DedupsOverlap()
    {
        var a = new Polyline();
        a.AddPath(new[] { new Point(0, 0), new Point(10, 0) });
        var b = new Polyline();
        b.AddPath(new[] { new Point(5, 0), new Point(15, 0) });

        var u = UnionOperator.Instance.Execute(a, b);

        Assert.Equal(15, u.CalculateLength2D(), 9);
    }
}
