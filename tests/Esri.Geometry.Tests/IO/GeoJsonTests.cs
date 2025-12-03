using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.IO;

namespace Esri.Geometry.Tests.IO;

public class GeoJsonTests
{
  [Fact]
  public void TestPointGeoJsonExportImport()
  {
    var point = new Point(10.5, 20.3);
    var geoJson = GeoJsonExportOperator.ExportToGeoJson(point);

    Assert.Contains("\"type\":\"Point\"", geoJson);
    Assert.Contains("\"coordinates\":", geoJson);

    var imported = GeoJsonImportOperator.ImportFromGeoJson(geoJson);
    Assert.IsType<Point>(imported);
    var importedPoint = (Point)imported;
    Assert.Equal(10.5, importedPoint.X, 10);
    Assert.Equal(20.3, importedPoint.Y, 10);
  }

  [Fact]
  public void TestPointWithZGeoJsonExportImport()
  {
    var point = new Point(10.5, 20.3, 30.7);
    var geoJson = GeoJsonExportOperator.ExportToGeoJson(point);

    Assert.Contains("\"type\":\"Point\"", geoJson);
    Assert.Contains("\"coordinates\":", geoJson);
    // Check that there are 3 coordinates (X, Y, Z)
    var coordStart = geoJson.IndexOf("[");
    var coordEnd = geoJson.IndexOf("]");
    var coords = geoJson.Substring(coordStart, coordEnd - coordStart + 1);
    var commaCount = coords.Split(',').Length;
    Assert.Equal(3, commaCount); // 3 values means 2 commas and 3 numbers

    var imported = GeoJsonImportOperator.ImportFromGeoJson(geoJson);
    Assert.IsType<Point>(imported);
    var importedPoint = (Point)imported;
    Assert.Equal(10.5, importedPoint.X, 10);
    Assert.Equal(20.3, importedPoint.Y, 10);
    Assert.True(importedPoint.Z.HasValue);
    Assert.Equal(30.7, importedPoint.Z.Value, 10);
  }

  [Fact]
  public void TestMultiPointGeoJsonExportImport()
  {
    var multiPoint = new MultiPoint();
    multiPoint.Add(new Point(10, 20));
    multiPoint.Add(new Point(30, 40));
    multiPoint.Add(new Point(50, 60));

    var geoJson = GeoJsonExportOperator.ExportToGeoJson(multiPoint);

    Assert.Contains("\"type\":\"MultiPoint\"", geoJson);
    Assert.Contains("\"coordinates\":", geoJson);

    var imported = GeoJsonImportOperator.ImportFromGeoJson(geoJson);
    Assert.IsType<MultiPoint>(imported);
    var importedMultiPoint = (MultiPoint)imported;
    Assert.Equal(3, importedMultiPoint.Count);
    Assert.Equal(10, importedMultiPoint.GetPoint(0).X);
    Assert.Equal(60, importedMultiPoint.GetPoint(2).Y);
  }

  [Fact]
  public void TestLineStringGeoJsonExportImport()
  {
    var line = new Line(new Point(0, 0), new Point(10, 10));
    var geoJson = GeoJsonExportOperator.ExportToGeoJson(line);

    Assert.Contains("\"type\":\"LineString\"", geoJson);

    var imported = GeoJsonImportOperator.ImportFromGeoJson(geoJson);
    Assert.IsType<Polyline>(imported);
    var importedPolyline = (Polyline)imported;
    Assert.Equal(1, importedPolyline.PathCount);
    var path = importedPolyline.GetPath(0);
    Assert.Equal(2, path.Count);
    Assert.Equal(0, path[0].X);
    Assert.Equal(10, path[1].Y);
  }

  [Fact]
  public void TestPolylineGeoJsonExportImport()
  {
    var polyline = new Polyline();
    var path = new List<Point>
    {
      new(0, 0),
      new(10, 0),
      new(10, 10)
    };
    polyline.AddPath(path);

    var geoJson = GeoJsonExportOperator.ExportToGeoJson(polyline);

    Assert.Contains("\"type\":\"LineString\"", geoJson);

    var imported = GeoJsonImportOperator.ImportFromGeoJson(geoJson);
    Assert.IsType<Polyline>(imported);
    var importedPolyline = (Polyline)imported;
    Assert.Equal(1, importedPolyline.PathCount);
    Assert.Equal(3, importedPolyline.GetPath(0).Count);
  }

  [Fact]
  public void TestMultiLineStringGeoJsonExportImport()
  {
    var polyline = new Polyline();
    var path1 = new List<Point>
    {
      new(0, 0),
      new(10, 0)
    };
    var path2 = new List<Point>
    {
      new(20, 20),
      new(30, 30)
    };
    polyline.AddPath(path1);
    polyline.AddPath(path2);

    var geoJson = GeoJsonExportOperator.ExportToGeoJson(polyline);

    Assert.Contains("\"type\":\"MultiLineString\"", geoJson);

    var imported = GeoJsonImportOperator.ImportFromGeoJson(geoJson);
    Assert.IsType<Polyline>(imported);
    var importedPolyline = (Polyline)imported;
    Assert.Equal(2, importedPolyline.PathCount);
  }

  [Fact]
  public void TestPolygonGeoJsonExportImport()
  {
    var polygon = new Polygon();
    var ring = new List<Point>
    {
      new(0, 0),
      new(10, 0),
      new(10, 10),
      new(0, 10),
      new(0, 0) // Closed ring
    };
    polygon.AddRing(ring);

    var geoJson = GeoJsonExportOperator.ExportToGeoJson(polygon);

    Assert.Contains("\"type\":\"Polygon\"", geoJson);
    Assert.Contains("\"coordinates\":", geoJson);

    var imported = GeoJsonImportOperator.ImportFromGeoJson(geoJson);
    Assert.IsType<Polygon>(imported);
    var importedPolygon = (Polygon)imported;
    Assert.Equal(1, importedPolygon.RingCount);
    Assert.Equal(5, importedPolygon.GetRing(0).Count);
  }

  [Fact]
  public void TestEnvelopeGeoJsonExport()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    var geoJson = GeoJsonExportOperator.ExportToGeoJson(envelope);

    Assert.Contains("\"type\":\"Polygon\"", geoJson);
    // Envelope is exported as a rectangular polygon
    Assert.Contains("\"coordinates\":", geoJson);
  }
}