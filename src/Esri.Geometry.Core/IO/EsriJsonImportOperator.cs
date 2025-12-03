using Esri.Geometry.Core.Geometries;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Esri.Geometry.Core.IO
{
    /// <summary>
    /// Imports geometries from Esri JSON format.
    /// </summary>
    public static class EsriJsonImportOperator
    {
        /// <summary>
        /// Imports a geometry from Esri JSON format.
        /// </summary>
        /// <param name="esriJson">The Esri JSON string</param>
        /// <returns>The parsed geometry</returns>
        public static Geometries.Geometry ImportFromEsriJson(string esriJson)
        {
            if (string.IsNullOrWhiteSpace(esriJson))
                throw new ArgumentException("Esri JSON string cannot be null or empty", nameof(esriJson));

            using (JsonDocument doc = JsonDocument.Parse(esriJson))
            {
                JsonElement root = doc.RootElement;

                // Check for Point (has x, y properties)
                if (root.TryGetProperty("x", out JsonElement xElement) && 
                    root.TryGetProperty("y", out JsonElement yElement))
                {
                    return ParsePoint(root);
                }

                // Check for MultiPoint (has points array)
                if (root.TryGetProperty("points", out JsonElement pointsElement))
                {
                    return ParseMultiPoint(pointsElement);
                }

                // Check for Polyline (has paths array)
                if (root.TryGetProperty("paths", out JsonElement pathsElement))
                {
                    return ParsePolyline(pathsElement);
                }

                // Check for Polygon (has rings array)
                if (root.TryGetProperty("rings", out JsonElement ringsElement))
                {
                    return ParsePolygon(ringsElement);
                }

                // Check for Envelope (has xmin, ymin, xmax, ymax)
                if (root.TryGetProperty("xmin", out _) && root.TryGetProperty("ymin", out _))
                {
                    return ParseEnvelope(root);
                }

                throw new ArgumentException("Unrecognized Esri JSON geometry format");
            }
        }

        private static Point ParsePoint(JsonElement element)
        {
            double x = element.GetProperty("x").GetDouble();
            double y = element.GetProperty("y").GetDouble();
            
            var point = new Point(x, y);

            if (element.TryGetProperty("z", out JsonElement zElement))
            {
                point.Z = zElement.GetDouble();
            }
            if (element.TryGetProperty("m", out JsonElement mElement))
            {
                point.M = mElement.GetDouble();
            }

            return point;
        }

        private static MultiPoint ParseMultiPoint(JsonElement pointsArray)
        {
            var points = new List<Point>();

            foreach (JsonElement pointElement in pointsArray.EnumerateArray())
            {
                if (pointElement.ValueKind == JsonValueKind.Array)
                {
                    var coords = ParseCoordinateArray(pointElement);
                    points.Add(coords);
                }
            }

            return new MultiPoint(points);
        }

        private static Polyline ParsePolyline(JsonElement pathsArray)
        {
            var polyline = new Polyline();

            foreach (JsonElement pathElement in pathsArray.EnumerateArray())
            {
                var path = new List<Point>();

                foreach (JsonElement pointElement in pathElement.EnumerateArray())
                {
                    var coords = ParseCoordinateArray(pointElement);
                    path.Add(coords);
                }

                if (path.Count >= 2)
                {
                    polyline.AddPath(path);
                }
            }

            return polyline;
        }

        private static Polygon ParsePolygon(JsonElement ringsArray)
        {
            var polygon = new Polygon();

            foreach (JsonElement ringElement in ringsArray.EnumerateArray())
            {
                var ring = new List<Point>();

                foreach (JsonElement pointElement in ringElement.EnumerateArray())
                {
                    var coords = ParseCoordinateArray(pointElement);
                    ring.Add(coords);
                }

                if (ring.Count >= 3)
                {
                    polygon.AddRing(ring);
                }
            }

            return polygon;
        }

        private static Envelope ParseEnvelope(JsonElement element)
        {
            double xmin = element.GetProperty("xmin").GetDouble();
            double ymin = element.GetProperty("ymin").GetDouble();
            double xmax = element.GetProperty("xmax").GetDouble();
            double ymax = element.GetProperty("ymax").GetDouble();

            return new Envelope(xmin, ymin, xmax, ymax);
        }

        private static Point ParseCoordinateArray(JsonElement coordArray)
        {
            int length = coordArray.GetArrayLength();

            if (length < 2)
                throw new ArgumentException("Coordinate array must have at least 2 elements");

            double x = coordArray[0].GetDouble();
            double y = coordArray[1].GetDouble();
            
            var point = new Point(x, y);
            
            if (length > 2)
            {
                point.Z = coordArray[2].GetDouble();
            }
            if (length > 3)
            {
                point.M = coordArray[3].GetDouble();
            }

            return point;
        }
    }
}
