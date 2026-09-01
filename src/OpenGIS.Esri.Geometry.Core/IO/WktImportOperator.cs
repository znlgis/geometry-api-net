using System;
using System.Collections.Generic;
using System.Globalization;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.IO;

/// <summary>
///     Imports geometries from Well-Known Text (WKT) format.
/// </summary>
public static class WktImportOperator
{
    private const int MaxInputLength = 1_000_000;

    /// <summary>
    ///     从 WKT 格式导入几何对象.
    /// </summary>
    /// <param name="wkt">The WKT string to parse.</param>
    /// <returns>The parsed geometry.</returns>
    public static Geometries.Geometry ImportFromWkt(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
            throw new ArgumentException("WKT string cannot be null or empty.", nameof(wkt));

        if (wkt.Length > MaxInputLength)
            throw new FormatException($"WKT input exceeds the maximum allowed length of {MaxInputLength} characters.");

        var parser = new WktParser(wkt);
        return parser.Parse();
    }

    private sealed class WktParser
    {
        private readonly string _text;
        private int _pos;

        public WktParser(string text)
        {
            _text = text;
        }

        public Geometries.Geometry Parse()
        {
            SkipWhitespace();
            var type = ReadTypeToken();

            SkipWhitespace();
            if (TryReadKeyword("EMPTY"))
            {
                SkipWhitespace();
                if (_pos < _text.Length)
                    throw Error("Unexpected trailing content after EMPTY.");
                return CreateEmpty(type);
            }

            // Optional dimensionality modifier (Z, M, or ZM) between the type and the coordinates.
            SkipWhitespace();
            if (TryReadKeyword("Z") || TryReadKeyword("M") || TryReadKeyword("ZM"))
                SkipWhitespace();

            if (_pos >= _text.Length || _text[_pos] != '(')
                throw Error($"Expected '(' after '{type}'.");

            return type switch
            {
                "POINT" => ParsePoint(),
                "LINESTRING" => ParseLineString(),
                "POLYGON" => ParsePolygon(),
                "MULTIPOINT" => ParseMultiPoint(),
                "MULTILINESTRING" => ParseMultiLineString(),
                _ => throw Error($"Unsupported or invalid WKT type '{type}'.")
            };
        }

        private Geometries.Geometry ParsePoint()
        {
            var coords = ReadCoordinateList(expectParens: true);
            if (coords.Count != 1)
                throw Error("POINT must contain exactly one coordinate.");

            var p = coords[0];
            return p.Z.HasValue ? new Point(p.X, p.Y, p.Z.Value) : new Point(p.X, p.Y);
        }

        private Polyline ParseLineString()
        {
            var points = ReadCoordinateList(expectParens: true);
            var polyline = new Polyline();
            polyline.AddPath(points);
            return polyline;
        }

        private Polygon ParsePolygon()
        {
            var polygon = new Polygon();
            foreach (var ring in ReadRingList())
                polygon.AddRing(ring);
            return polygon;
        }

        private MultiPoint ParseMultiPoint()
        {
            var points = ReadCoordinateList(expectParens: true);
            return new MultiPoint(points);
        }

        private Polyline ParseMultiLineString()
        {
            var polyline = new Polyline();
            foreach (var path in ReadRingList())
                polyline.AddPath(path);
            return polyline;
        }

        private static Geometries.Geometry CreateEmpty(string type)
        {
            return type switch
            {
                "POINT" => new Point(),
                "LINESTRING" => new Polyline(),
                "POLYGON" => new Polygon(),
                "MULTIPOINT" => new MultiPoint(),
                "MULTILINESTRING" => new Polyline(),
                _ => throw new FormatException($"Unsupported or invalid WKT type '{type}'.")
            };
        }

        private List<List<Point>> ReadRingList()
        {
            var rings = new List<List<Point>>();
            Expect('(');
            SkipWhitespace();

            if (TryConsume(')'))
                return rings;

            while (true)
            {
                rings.Add(ReadCoordinateList(expectParens: true));
                SkipWhitespace();

                if (TryConsume(')'))
                    break;

                Expect(',');
                SkipWhitespace();
            }

            return rings;
        }

        private List<Point> ReadCoordinateList(bool expectParens)
        {
            if (expectParens)
                Expect('(');

            var points = new List<Point>();
            SkipWhitespace();

            if (TryConsume(')'))
                return points;

            while (true)
            {
                points.Add(ReadCoordinate());
                SkipWhitespace();

                if (TryConsume(')'))
                    break;

                Expect(',');
                SkipWhitespace();
            }

            return points;
        }

        private Point ReadCoordinate()
        {
            var x = ReadNumber();
            SkipWhitespace();
            var y = ReadNumber();
            SkipWhitespace();

            if (_pos < _text.Length && (_text[_pos] == ',' || _text[_pos] == ')'))
                return new Point(x, y);

            var z = ReadNumber();
            return new Point(x, y, z);
        }

        private double ReadNumber()
        {
            var start = _pos;
            while (_pos < _text.Length && !char.IsWhiteSpace(_text[_pos]) && _text[_pos] != ',' && _text[_pos] != ')')
                _pos++;

            if (start == _pos)
                throw Error("Expected a numeric coordinate.");

            var token = _text.Substring(start, _pos - start);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw Error($"Invalid numeric coordinate '{token}'.");

            return value;
        }

        private string ReadTypeToken()
        {
            var start = _pos;
            while (_pos < _text.Length && char.IsLetter(_text[_pos]))
                _pos++;

            if (start == _pos)
                throw Error("Expected a geometry type keyword.");

            return _text.Substring(start, _pos - start).ToUpperInvariant();
        }

        private bool TryReadKeyword(string keyword)
        {
            if (_pos + keyword.Length > _text.Length)
                return false;

            if (!string.Equals(_text.Substring(_pos, keyword.Length), keyword, StringComparison.OrdinalIgnoreCase))
                return false;

            _pos += keyword.Length;
            return true;
        }

        private void Expect(char c)
        {
            if (_pos >= _text.Length || _text[_pos] != c)
                throw Error($"Expected '{c}'.");
            _pos++;
        }

        private bool TryConsume(char c)
        {
            if (_pos < _text.Length && _text[_pos] == c)
            {
                _pos++;
                return true;
            }

            return false;
        }

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
                _pos++;
        }

        private FormatException Error(string message)
        {
            var context = _pos < _text.Length ? _text.Substring(_pos, Math.Min(20, _text.Length - _pos)) : "<end>";
            return new FormatException($"{message} (at position {_pos}, near '{context}').");
        }
    }
}
