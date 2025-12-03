using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.IO;

namespace OpenGIS.Esri.Geometry.Tests.IO;

public class EsriJsonTests
{
    [Fact]
    public void TestPointEsriJsonRoundTrip()
    {
        var point = new Point(10.5, 20.3);
        point.Z = 30.1;

        var esriJson = EsriJsonExportOperator.Instance.Execute(point);
        var parsed = EsriJsonImportOperator.ImportFromEsriJson(esriJson);

        var parsedPoint = Assert.IsType<Point>(parsed);
        Assert.Equal(10.5, parsedPoint.X, 10);
        Assert.Equal(20.3, parsedPoint.Y, 10);
        Assert.True(parsedPoint.Z.HasValue);
        Assert.Equal(30.1, parsedPoint.Z.Value, 10);
    }

    [Fact]
    public void TestMultiPointEsriJsonRoundTrip()
    {
        var points = new List<Point>
        {
            new(1, 2),
            new(3, 4),
            new(5, 6)
        };
        var multiPoint = new MultiPoint(points);

        var esriJson = EsriJsonExportOperator.Instance.Execute(multiPoint);
        var parsed = EsriJsonImportOperator.ImportFromEsriJson(esriJson);

        var parsedMultiPoint = Assert.IsType<MultiPoint>(parsed);
        Assert.Equal(3, parsedMultiPoint.Count);
    }

    [Fact]
    public void TestPolylineEsriJsonRoundTrip()
    {
        var polyline = new Polyline();
        polyline.AddPath(new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10)
        });

        var esriJson = EsriJsonExportOperator.Instance.Execute(polyline);
        var parsed = EsriJsonImportOperator.ImportFromEsriJson(esriJson);

        var parsedPolyline = Assert.IsType<Polyline>(parsed);
        Assert.Equal(1, parsedPolyline.PathCount);
        Assert.Equal(3, parsedPolyline.GetPath(0).Count);
    }

    [Fact]
    public void TestPolygonEsriJsonRoundTrip()
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

        var esriJson = EsriJsonExportOperator.Instance.Execute(polygon);
        var parsed = EsriJsonImportOperator.ImportFromEsriJson(esriJson);

        var parsedPolygon = Assert.IsType<Polygon>(parsed);
        Assert.Equal(1, parsedPolygon.RingCount);
        Assert.Equal(5, parsedPolygon.GetRing(0).Count);
    }

    [Fact]
    public void TestEnvelopeEsriJsonRoundTrip()
    {
        var envelope = new Envelope(1.5, 2.5, 10.5, 20.5);

        var esriJson = EsriJsonExportOperator.Instance.Execute(envelope);
        var parsed = EsriJsonImportOperator.ImportFromEsriJson(esriJson);

        var parsedEnvelope = Assert.IsType<Envelope>(parsed);
        Assert.Equal(1.5, parsedEnvelope.XMin, 10);
        Assert.Equal(2.5, parsedEnvelope.YMin, 10);
        Assert.Equal(10.5, parsedEnvelope.XMax, 10);
        Assert.Equal(20.5, parsedEnvelope.YMax, 10);
    }

    [Fact]
    public void TestPointWithMValueEsriJson()
    {
        var point = new Point(10, 20);
        point.Z = 30;
        point.M = 40;

        var esriJson = EsriJsonExportOperator.Instance.Execute(point);

        Assert.Contains("\"x\":", esriJson);
        Assert.Contains("\"y\":", esriJson);
        Assert.Contains("\"z\":", esriJson);
        Assert.Contains("\"m\":", esriJson);
    }

    [Fact]
    public void TestEsriJsonFormat()
    {
        // Verify Esri JSON format (uses different structure than GeoJSON)
        var point = new Point(10, 20);
        var esriJson = EsriJsonExportOperator.Instance.Execute(point);

        // Esri JSON uses x, y (not coordinates array like GeoJSON)
        Assert.Contains("\"x\":", esriJson);
        Assert.Contains("\"y\":", esriJson);
        Assert.DoesNotContain("coordinates", esriJson);
        Assert.DoesNotContain("type", esriJson);
    }
}