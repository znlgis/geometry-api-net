using System;
using System.Collections.Generic;
using System.IO;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.IO
{
    /// <summary>
    /// Imports geometries from Well-Known Binary (WKB) format.
    /// </summary>
    public static class WkbImportOperator
    {
        private const byte WKB_POINT = 1;
        private const byte WKB_LINESTRING = 2;
        private const byte WKB_POLYGON = 3;
        private const byte WKB_MULTIPOINT = 4;
        private const byte WKB_MULTILINESTRING = 5;

        /// <summary>
        /// Imports a geometry from WKB format.
        /// </summary>
        /// <param name="wkb">The WKB byte array to parse.</param>
        /// <returns>The parsed geometry.</returns>
        public static Geometries.Geometry ImportFromWkb(byte[] wkb)
        {
            if (wkb == null || wkb.Length == 0)
            {
                throw new ArgumentException("WKB data cannot be null or empty.", nameof(wkb));
            }

            using (var stream = new MemoryStream(wkb))
            using (var reader = new BinaryReader(stream))
            {
                return ReadGeometry(reader);
            }
        }

        private static Geometries.Geometry ReadGeometry(BinaryReader reader)
        {
            // Read byte order
            byte byteOrder = reader.ReadByte();
            bool bigEndian = (byteOrder == 0);

            // Read geometry type
            int geometryType = ReadInt32(reader, bigEndian);

            return geometryType switch
            {
                WKB_POINT => ReadPoint(reader, bigEndian),
                WKB_LINESTRING => ReadLineString(reader, bigEndian),
                WKB_POLYGON => ReadPolygon(reader, bigEndian),
                WKB_MULTIPOINT => ReadMultiPoint(reader, bigEndian),
                WKB_MULTILINESTRING => ReadMultiLineString(reader, bigEndian),
                _ => throw new FormatException($"Unsupported WKB geometry type: {geometryType}")
            };
        }

        private static Point ReadPoint(BinaryReader reader, bool bigEndian)
        {
            double x = ReadDouble(reader, bigEndian);
            double y = ReadDouble(reader, bigEndian);
            return new Point(x, y);
        }

        private static Polyline ReadLineString(BinaryReader reader, bool bigEndian)
        {
            int numPoints = ReadInt32(reader, bigEndian);
            var points = new List<Point>(numPoints);
            
            for (int i = 0; i < numPoints; i++)
            {
                double x = ReadDouble(reader, bigEndian);
                double y = ReadDouble(reader, bigEndian);
                points.Add(new Point(x, y));
            }

            var polyline = new Polyline();
            polyline.AddPath(points);
            return polyline;
        }

        private static Polygon ReadPolygon(BinaryReader reader, bool bigEndian)
        {
            int numRings = ReadInt32(reader, bigEndian);
            var polygon = new Polygon();

            for (int i = 0; i < numRings; i++)
            {
                int numPoints = ReadInt32(reader, bigEndian);
                var ring = new List<Point>(numPoints);
                
                for (int j = 0; j < numPoints; j++)
                {
                    double x = ReadDouble(reader, bigEndian);
                    double y = ReadDouble(reader, bigEndian);
                    ring.Add(new Point(x, y));
                }

                polygon.AddRing(ring);
            }

            return polygon;
        }

        private static MultiPoint ReadMultiPoint(BinaryReader reader, bool bigEndian)
        {
            int numPoints = ReadInt32(reader, bigEndian);
            var multiPoint = new MultiPoint();

            for (int i = 0; i < numPoints; i++)
            {
                // Each point has its own byte order and type
                byte pointByteOrder = reader.ReadByte();
                bool pointBigEndian = (pointByteOrder == 0);
                int pointType = ReadInt32(reader, pointBigEndian);
                
                if (pointType != WKB_POINT)
                {
                    throw new FormatException($"Expected point type in multipoint, got {pointType}");
                }

                double x = ReadDouble(reader, pointBigEndian);
                double y = ReadDouble(reader, pointBigEndian);
                multiPoint.Add(new Point(x, y));
            }

            return multiPoint;
        }

        private static Polyline ReadMultiLineString(BinaryReader reader, bool bigEndian)
        {
            int numLineStrings = ReadInt32(reader, bigEndian);
            var polyline = new Polyline();

            for (int i = 0; i < numLineStrings; i++)
            {
                // Each linestring has its own byte order and type
                byte lsByteOrder = reader.ReadByte();
                bool lsBigEndian = (lsByteOrder == 0);
                int lsType = ReadInt32(reader, lsBigEndian);
                
                if (lsType != WKB_LINESTRING)
                {
                    throw new FormatException($"Expected linestring type in multilinestring, got {lsType}");
                }

                int numPoints = ReadInt32(reader, lsBigEndian);
                var points = new List<Point>(numPoints);
                
                for (int j = 0; j < numPoints; j++)
                {
                    double x = ReadDouble(reader, lsBigEndian);
                    double y = ReadDouble(reader, lsBigEndian);
                    points.Add(new Point(x, y));
                }

                polyline.AddPath(points);
            }

            return polyline;
        }

        private static int ReadInt32(BinaryReader reader, bool bigEndian)
        {
            var bytes = reader.ReadBytes(4);
            if (bigEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        private static double ReadDouble(BinaryReader reader, bool bigEndian)
        {
            var bytes = reader.ReadBytes(8);
            if (bigEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToDouble(bytes, 0);
        }
    }
}
