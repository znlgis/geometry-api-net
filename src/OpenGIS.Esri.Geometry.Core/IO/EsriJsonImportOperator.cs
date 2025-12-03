using System;
using System.Collections.Generic;
using System.Text.Json;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.IO;

/// <summary>
///     从 Esri JSON 格式导入几何对象.
/// </summary>
public static class EsriJsonImportOperator
{
  /// <summary>
  ///     从 Esri JSON 格式导入几何对象.
  /// </summary>
  /// <param name="esriJson">The Esri JSON string</param>
  /// <returns>The parsed geometry</returns>
  public static Geometries.Geometry ImportFromEsriJson(string esriJson)
    {
        if (string.IsNullOrWhiteSpace(esriJson))
            throw new ArgumentException("Esri JSON string cannot be null or empty", nameof(esriJson));

        using (var doc = JsonDocument.Parse(esriJson))
        {
            var root = doc.RootElement;

            // Check for Point (has x, y properties)
            if (root.TryGetProperty("x", out var xElement) &&
                root.TryGetProperty("y", out var yElement))
                return ParsePoint(root);

            // Check for MultiPoint (has points array)
            if (root.TryGetProperty("points", out var pointsElement)) return ParseMultiPoint(pointsElement);

            // Check for Polyline (has paths array)
            if (root.TryGetProperty("paths", out var pathsElement)) return ParsePolyline(pathsElement);

            // Check for Polygon (has rings array)
            if (root.TryGetProperty("rings", out var ringsElement)) return ParsePolygon(ringsElement);

            // Check for Envelope (has xmin, ymin, xmax, ymax)
            if (root.TryGetProperty("xmin", out _) && root.TryGetProperty("ymin", out _)) return ParseEnvelope(root);

            throw new ArgumentException("Unrecognized Esri JSON geometry format");
        }
    }

    private static Point ParsePoint(JsonElement element)
    {
        var x = element.GetProperty("x").GetDouble();
        var y = element.GetProperty("y").GetDouble();

        var point = new Point(x, y);

        if (element.TryGetProperty("z", out var zElement)) point.Z = zElement.GetDouble();
        if (element.TryGetProperty("m", out var mElement)) point.M = mElement.GetDouble();

        return point;
    }

    private static MultiPoint ParseMultiPoint(JsonElement pointsArray)
    {
        var points = new List<Point>();

        foreach (var pointElement in pointsArray.EnumerateArray())
            if (pointElement.ValueKind == JsonValueKind.Array)
            {
                var coords = ParseCoordinateArray(pointElement);
                points.Add(coords);
            }

        return new MultiPoint(points);
    }

    private static Polyline ParsePolyline(JsonElement pathsArray)
    {
        var polyline = new Polyline();

        foreach (var pathElement in pathsArray.EnumerateArray())
        {
            var path = new List<Point>();

            foreach (var pointElement in pathElement.EnumerateArray())
            {
                var coords = ParseCoordinateArray(pointElement);
                path.Add(coords);
            }

            if (path.Count >= 2) polyline.AddPath(path);
        }

        return polyline;
    }

    private static Polygon ParsePolygon(JsonElement ringsArray)
    {
        var polygon = new Polygon();

        foreach (var ringElement in ringsArray.EnumerateArray())
        {
            var ring = new List<Point>();

            foreach (var pointElement in ringElement.EnumerateArray())
            {
                var coords = ParseCoordinateArray(pointElement);
                ring.Add(coords);
            }

            if (ring.Count >= 3) polygon.AddRing(ring);
        }

        return polygon;
    }

    private static Envelope ParseEnvelope(JsonElement element)
    {
        var xmin = element.GetProperty("xmin").GetDouble();
        var ymin = element.GetProperty("ymin").GetDouble();
        var xmax = element.GetProperty("xmax").GetDouble();
        var ymax = element.GetProperty("ymax").GetDouble();

        return new Envelope(xmin, ymin, xmax, ymax);
    }

    private static Point ParseCoordinateArray(JsonElement coordArray)
    {
        var length = coordArray.GetArrayLength();

        if (length < 2)
            throw new ArgumentException("Coordinate array must have at least 2 elements");

        var x = coordArray[0].GetDouble();
        var y = coordArray[1].GetDouble();

        var point = new Point(x, y);

        if (length > 2) point.Z = coordArray[2].GetDouble();
        if (length > 3) point.M = coordArray[3].GetDouble();

        return point;
    }
}