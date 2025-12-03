using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.IO;

/// <summary>
///     Imports geometries from Well-Known Text (WKT) format.
/// </summary>
public static class WktImportOperator
{
  /// <summary>
  ///     从 WKT 格式导入几何对象.
  /// </summary>
  /// <param name="wkt">The WKT string to parse.</param>
  /// <returns>The parsed geometry.</returns>
  public static Geometries.Geometry ImportFromWkt(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
            throw new ArgumentException("WKT string cannot be null or empty.", nameof(wkt));

        wkt = wkt.Trim();

        // Check for EMPTY geometries
        if (wkt.EndsWith("EMPTY", StringComparison.OrdinalIgnoreCase))
        {
            if (wkt.StartsWith("POINT", StringComparison.OrdinalIgnoreCase))
                return new Point();
            if (wkt.StartsWith("LINESTRING", StringComparison.OrdinalIgnoreCase))
                return new Polyline();
            if (wkt.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase))
                return new Polygon();
            if (wkt.StartsWith("MULTIPOINT", StringComparison.OrdinalIgnoreCase))
                return new MultiPoint();
        }

        if (wkt.StartsWith("POINT", StringComparison.OrdinalIgnoreCase))
            return ParsePoint(wkt);
        if (wkt.StartsWith("LINESTRING", StringComparison.OrdinalIgnoreCase))
            return ParseLineString(wkt);
        if (wkt.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase))
            return ParsePolygon(wkt);
        if (wkt.StartsWith("MULTIPOINT", StringComparison.OrdinalIgnoreCase))
            return ParseMultiPoint(wkt);
        if (wkt.StartsWith("MULTILINESTRING", StringComparison.OrdinalIgnoreCase))
            return ParseMultiLineString(wkt);

        throw new FormatException($"Unsupported or invalid WKT format: {wkt}");
    }

    private static Point ParsePoint(string wkt)
    {
        var match = Regex.Match(wkt, @"POINT\s*Z?\s*\(\s*([\d\.\-\+eE]+)\s+([\d\.\-\+eE]+)(?:\s+([\d\.\-\+eE]+))?\s*\)",
            RegexOptions.IgnoreCase);
        if (!match.Success) throw new FormatException($"Invalid POINT WKT: {wkt}");

        var x = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var y = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        if (match.Groups[3].Success)
        {
            var z = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            return new Point(x, y, z);
        }

        return new Point(x, y);
    }

    private static Polyline ParseLineString(string wkt)
    {
        var match = Regex.Match(wkt, @"LINESTRING\s*\((.*)\)", RegexOptions.IgnoreCase);
        if (!match.Success) throw new FormatException($"Invalid LINESTRING WKT: {wkt}");

        var points = ParseCoordinateList(match.Groups[1].Value);
        var polyline = new Polyline();
        polyline.AddPath(points);
        return polyline;
    }

    private static Polygon ParsePolygon(string wkt)
    {
        var match = Regex.Match(wkt, @"POLYGON\s*\((.*)\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success) throw new FormatException($"Invalid POLYGON WKT: {wkt}");

        var polygon = new Polygon();
        var ringsText = match.Groups[1].Value;
        var rings = ParseRings(ringsText);

        foreach (var ring in rings) polygon.AddRing(ring);

        return polygon;
    }

    private static MultiPoint ParseMultiPoint(string wkt)
    {
        var match = Regex.Match(wkt, @"MULTIPOINT\s*\((.*)\)", RegexOptions.IgnoreCase);
        if (!match.Success) throw new FormatException($"Invalid MULTIPOINT WKT: {wkt}");

        var points = ParseCoordinateList(match.Groups[1].Value);
        return new MultiPoint(points);
    }

    private static Polyline ParseMultiLineString(string wkt)
    {
        var match = Regex.Match(wkt, @"MULTILINESTRING\s*\((.*)\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success) throw new FormatException($"Invalid MULTILINESTRING WKT: {wkt}");

        var polyline = new Polyline();
        var pathsText = match.Groups[1].Value;
        var paths = ParseRings(pathsText);

        foreach (var path in paths) polyline.AddPath(path);

        return polyline;
    }

    private static List<List<Point>> ParseRings(string text)
    {
        var rings = new List<List<Point>>();
        var depth = 0;
        var currentRing = new StringBuilder();

        foreach (var c in text)
        {
            if (c == '(')
            {
                depth++;
                if (depth == 1)
                {
                    currentRing.Clear();
                    continue;
                }
            }
            else if (c == ')')
            {
                depth--;
                if (depth == 0)
                {
                    rings.Add(ParseCoordinateList(currentRing.ToString()));
                    currentRing.Clear();
                    continue;
                }
            }

            if (depth > 0) currentRing.Append(c);
        }

        return rings;
    }

    private static List<Point> ParseCoordinateList(string text)
    {
        var points = new List<Point>();
        var coordPairs = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in coordPairs)
        {
            var coords = pair.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (coords.Length < 2) throw new FormatException($"Invalid coordinate pair: {pair}");

            var x = double.Parse(coords[0], CultureInfo.InvariantCulture);
            var y = double.Parse(coords[1], CultureInfo.InvariantCulture);

            if (coords.Length >= 3)
            {
                var z = double.Parse(coords[2], CultureInfo.InvariantCulture);
                points.Add(new Point(x, y, z));
            }
            else
            {
                points.Add(new Point(x, y));
            }
        }

        return points;
    }
}