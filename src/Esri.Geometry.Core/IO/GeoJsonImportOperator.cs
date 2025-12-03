using System;
using System.Collections.Generic;
using System.Text.Json;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.IO;

/// <summary>
///   Operator for importing geometries from GeoJSON format.
/// </summary>
public class GeoJsonImportOperator
{
    /// <summary>
    ///   从 GeoJSON 格式导入几何对象.
    /// </summary>
    /// <param name="geoJson">The GeoJSON string to parse.</param>
    /// <returns>The parsed geometry.</returns>
    public static Geometries.Geometry ImportFromGeoJson(string geoJson)
  {
    if (string.IsNullOrWhiteSpace(geoJson))
      throw new ArgumentException("GeoJSON string cannot be null or empty", nameof(geoJson));

    using (var doc = JsonDocument.Parse(geoJson))
    {
      return ParseGeometry(doc.RootElement);
    }
  }

  private static Geometries.Geometry ParseGeometry(JsonElement element)
  {
    if (!element.TryGetProperty("type", out var typeElement))
      throw new ArgumentException("GeoJSON object must have a 'type' property");

    var type = typeElement.GetString() ?? throw new ArgumentException("GeoJSON type cannot be null");

    if (!element.TryGetProperty("coordinates", out var coordinatesElement))
    {
      if (type == "GeometryCollection")
        // Empty geometry collection returns empty point
        return new Point();
      throw new ArgumentException("GeoJSON object must have a 'coordinates' property");
    }

    switch (type)
    {
      case "Point":
        return ParsePoint(coordinatesElement);
      case "MultiPoint":
        return ParseMultiPoint(coordinatesElement);
      case "LineString":
        return ParseLineString(coordinatesElement);
      case "MultiLineString":
        return ParseMultiLineString(coordinatesElement);
      case "Polygon":
        return ParsePolygon(coordinatesElement);
      default:
        throw new ArgumentException($"Unsupported GeoJSON geometry type: {type}");
    }
  }

  private static Point ParsePoint(JsonElement coordinates)
  {
    var coords = ParseCoordinate(coordinates);
    return coords.Count == 3
      ? new Point(coords[0], coords[1], coords[2])
      : new Point(coords[0], coords[1]);
  }

  private static MultiPoint ParseMultiPoint(JsonElement coordinates)
  {
    var multiPoint = new MultiPoint();

    foreach (var coordElement in coordinates.EnumerateArray())
    {
      var coords = ParseCoordinate(coordElement);
      multiPoint.Add(coords.Count == 3
        ? new Point(coords[0], coords[1], coords[2])
        : new Point(coords[0], coords[1]));
    }

    return multiPoint;
  }

  private static Polyline ParseLineString(JsonElement coordinates)
  {
    var polyline = new Polyline();
    var points = new List<Point>();

    foreach (var coordElement in coordinates.EnumerateArray())
    {
      var coords = ParseCoordinate(coordElement);
      points.Add(coords.Count == 3
        ? new Point(coords[0], coords[1], coords[2])
        : new Point(coords[0], coords[1]));
    }

    if (points.Count > 0) polyline.AddPath(points);

    return polyline;
  }

  private static Polyline ParseMultiLineString(JsonElement coordinates)
  {
    var polyline = new Polyline();

    foreach (var pathElement in coordinates.EnumerateArray())
    {
      var points = new List<Point>();

      foreach (var coordElement in pathElement.EnumerateArray())
      {
        var coords = ParseCoordinate(coordElement);
        points.Add(coords.Count == 3
          ? new Point(coords[0], coords[1], coords[2])
          : new Point(coords[0], coords[1]));
      }

      if (points.Count > 0) polyline.AddPath(points);
    }

    return polyline;
  }

  private static Polygon ParsePolygon(JsonElement coordinates)
  {
    var polygon = new Polygon();

    foreach (var ringElement in coordinates.EnumerateArray())
    {
      var points = new List<Point>();

      foreach (var coordElement in ringElement.EnumerateArray())
      {
        var coords = ParseCoordinate(coordElement);
        points.Add(coords.Count == 3
          ? new Point(coords[0], coords[1], coords[2])
          : new Point(coords[0], coords[1]));
      }

      if (points.Count > 0) polygon.AddRing(points);
    }

    return polygon;
  }

  private static List<double> ParseCoordinate(JsonElement coordinate)
  {
    var coords = new List<double>();

    foreach (var value in coordinate.EnumerateArray()) coords.Add(value.GetDouble());

    if (coords.Count < 2)
      throw new ArgumentException("Coordinate must have at least 2 values (X, Y)");

    return coords;
  }
}