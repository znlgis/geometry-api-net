using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Operators;

namespace OpenGIS.Esri.Geometry.Tests.Operators;

public class AdditionalOperatorTests
{
    [Fact]
    public void SimplifyOperator_Polyline_ReducesPoints()
    {
        var polyline = new Polyline();
        // Create a path with points that should be simplified
        var path = new[]
        {
            new Point(0, 0),
            new Point(1, 0.1), // Close to the line
            new Point(2, 0),
            new Point(3, 0.1), // Close to the line
            new Point(4, 0),
            new Point(5, 0)
        };
        polyline.AddPath(path);

        var simplified = SimplifyOperator.Instance.Execute(polyline, 0.2);

        Assert.IsType<Polyline>(simplified);
        var simplifiedPolyline = (Polyline)simplified;
        Assert.Equal(1, simplifiedPolyline.PathCount);

        // Should have fewer points than original
        var simplifiedPath = simplifiedPolyline.GetPath(0);
        Assert.True(simplifiedPath.Count < path.Length);
    }

    [Fact]
    public void SimplifyOperator_Polygon_ReducesVertices()
    {
        var polygon = new Polygon();
        var ring = new[]
        {
            new Point(0, 0),
            new Point(5, 0.1),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        };
        polygon.AddRing(ring);

        var simplified = SimplifyOperator.Instance.Execute(polygon, 0.2);

        Assert.IsType<Polygon>(simplified);
        var simplifiedPolygon = (Polygon)simplified;
        Assert.Equal(1, simplifiedPolygon.RingCount);

        // Should have fewer vertices than original
        var simplifiedRing = simplifiedPolygon.GetRing(0);
        Assert.True(simplifiedRing.Count < ring.Length);
    }

    [Fact]
    public void SimplifyOperator_Point_ReturnsUnchanged()
    {
        var point = new Point(10, 20);

        var simplified = SimplifyOperator.Instance.Execute(point, 1.0);

        Assert.IsType<Point>(simplified);
        var simplifiedPoint = (Point)simplified;
        Assert.Equal(10, simplifiedPoint.X);
        Assert.Equal(20, simplifiedPoint.Y);
    }

    [Fact]
    public void CentroidOperator_Point_ReturnsSamePoint()
    {
        var point = new Point(10, 20);

        var centroid = CentroidOperator.Instance.Execute(point);

        Assert.Equal(10, centroid.X);
        Assert.Equal(20, centroid.Y);
    }

    [Fact]
    public void CentroidOperator_MultiPoint_ReturnsAverage()
    {
        var multiPoint = new MultiPoint();
        multiPoint.Add(new Point(0, 0));
        multiPoint.Add(new Point(10, 0));
        multiPoint.Add(new Point(10, 10));

        var centroid = CentroidOperator.Instance.Execute(multiPoint);

        // Average should be approximately (6.67, 3.33)
        Assert.Equal(6.666, centroid.X, 2);
        Assert.Equal(3.333, centroid.Y, 2);
    }

    [Fact]
    public void CentroidOperator_Envelope_ReturnsCenter()
    {
        var envelope = new Envelope(0, 0, 10, 10);

        var centroid = CentroidOperator.Instance.Execute(envelope);

        Assert.Equal(5, centroid.X);
        Assert.Equal(5, centroid.Y);
    }

    [Fact]
    public void CentroidOperator_Line_ReturnsMidpoint()
    {
        var line = new Line(new Point(0, 0), new Point(10, 10));

        var centroid = CentroidOperator.Instance.Execute(line);

        Assert.Equal(5, centroid.X);
        Assert.Equal(5, centroid.Y);
    }

    [Fact]
    public void CentroidOperator_Polygon_ReturnsAreaCentroid()
    {
        var polygon = new Polygon();
        var ring = new[]
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        };
        polygon.AddRing(ring);

        var centroid = CentroidOperator.Instance.Execute(polygon);

        // Square centroid should be at (5, 5)
        Assert.Equal(5, centroid.X, 1);
        Assert.Equal(5, centroid.Y, 1);
    }

    [Fact]
    public void BoundaryOperator_Point_ReturnsEmptyMultiPoint()
    {
        var point = new Point(10, 20);

        var boundary = BoundaryOperator.Instance.Execute(point);

        Assert.IsType<MultiPoint>(boundary);
        Assert.True(boundary.IsEmpty);
    }

    [Fact]
    public void BoundaryOperator_Line_ReturnsEndpoints()
    {
        var line = new Line(new Point(0, 0), new Point(10, 10));

        var boundary = BoundaryOperator.Instance.Execute(line);

        Assert.IsType<MultiPoint>(boundary);
        var multiPoint = (MultiPoint)boundary;
        Assert.Equal(2, multiPoint.Count);
    }

    [Fact]
    public void BoundaryOperator_Polyline_ReturnsEndpoints()
    {
        var polyline = new Polyline();
        var path = new[] { new Point(0, 0), new Point(5, 5), new Point(10, 10) };
        polyline.AddPath(path);

        var boundary = BoundaryOperator.Instance.Execute(polyline);

        Assert.IsType<MultiPoint>(boundary);
        var multiPoint = (MultiPoint)boundary;
        Assert.Equal(2, multiPoint.Count); // Start and end points
    }

    [Fact]
    public void BoundaryOperator_Polygon_ReturnsRings()
    {
        var polygon = new Polygon();
        var ring = new[]
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        };
        polygon.AddRing(ring);

        var boundary = BoundaryOperator.Instance.Execute(polygon);

        Assert.IsType<Polyline>(boundary);
        var polyline = (Polyline)boundary;
        Assert.Equal(1, polyline.PathCount);
    }

    [Fact]
    public void BoundaryOperator_Envelope_ReturnsPerimeter()
    {
        var envelope = new Envelope(0, 0, 10, 10);

        var boundary = BoundaryOperator.Instance.Execute(envelope);

        Assert.IsType<Polyline>(boundary);
        var polyline = (Polyline)boundary;
        Assert.Equal(1, polyline.PathCount);

        var path = polyline.GetPath(0);
        Assert.Equal(5, path.Count); // 4 corners + closing point
    }
}