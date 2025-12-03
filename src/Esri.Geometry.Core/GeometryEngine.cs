using System;
using System.Collections.Generic;
using Esri.Geometry.Core.IO;
using Geometries = Esri.Geometry.Core.Geometries;
using Operators = Esri.Geometry.Core.Operators;
using SpatialRef = Esri.Geometry.Core.SpatialReference;

namespace Esri.Geometry.Core
{
    /// <summary>
    /// Provides a simplified, static API for geometry operations.
    /// This class wraps all the operator instances with convenient static methods.
    /// </summary>
    /// <remarks>
    /// GeometryEngine provides a simpler API compared to using operators directly.
    /// For advanced scenarios or better performance with batch operations,
    /// consider using the operator classes directly (e.g., UnionOperator, BufferOperator).
    /// </remarks>
    public static class GeometryEngine
    {
        #region Spatial Relationship Operations

        /// <summary>
        /// Tests if geometry1 contains geometry2.
        /// </summary>
        public static bool Contains(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.ContainsOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Tests if two geometries intersect.
        /// </summary>
        public static bool Intersects(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.IntersectsOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Calculates the distance between two geometries.
        /// </summary>
        public static double Distance(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.DistanceOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Tests if two geometries are spatially equal.
        /// </summary>
        public static bool Equals(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.EqualsOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Tests if two geometries are disjoint (do not intersect).
        /// </summary>
        public static bool Disjoint(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.DisjointOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Tests if geometry1 is within geometry2.
        /// </summary>
        public static bool Within(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.WithinOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Tests if two geometries cross.
        /// </summary>
        public static bool Crosses(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.CrossesOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Tests if two geometries touch at their boundaries.
        /// </summary>
        public static bool Touches(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.TouchesOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Tests if two geometries of the same dimension overlap.
        /// </summary>
        public static bool Overlaps(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.OverlapsOperator.Instance.Execute(geometry1, geometry2);
        }

        #endregion

        #region Set Operations

        /// <summary>
        /// Computes the union of two geometries.
        /// </summary>
        public static Geometries.Geometry Union(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.UnionOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Computes the intersection of two geometries.
        /// </summary>
        public static Geometries.Geometry Intersection(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.IntersectionOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Computes the difference of two geometries (geometry1 - geometry2).
        /// </summary>
        public static Geometries.Geometry Difference(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.DifferenceOperator.Instance.Execute(geometry1, geometry2);
        }

        /// <summary>
        /// Computes the symmetric difference of two geometries.
        /// </summary>
        public static Geometries.Geometry SymmetricDifference(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
        {
            return Operators.SymmetricDifferenceOperator.Instance.Execute(geometry1, geometry2);
        }

        #endregion

        #region Geometry Operations

        /// <summary>
        /// Creates a buffer around a geometry.
        /// </summary>
        public static Geometries.Geometry Buffer(Geometries.Geometry geometry, double distance)
        {
            return Operators.BufferOperator.Instance.Execute(geometry, distance);
        }

        /// <summary>
        /// Computes the convex hull of a geometry.
        /// </summary>
        public static Geometries.Geometry ConvexHull(Geometries.Geometry geometry)
        {
            return Operators.ConvexHullOperator.Instance.Execute(geometry);
        }

        /// <summary>
        /// Calculates the area of a geometry.
        /// </summary>
        public static double Area(Geometries.Geometry geometry)
        {
            return Operators.AreaOperator.Instance.Execute(geometry);
        }

        /// <summary>
        /// Calculates the length or perimeter of a geometry.
        /// </summary>
        public static double Length(Geometries.Geometry geometry)
        {
            return Operators.LengthOperator.Instance.Execute(geometry);
        }

        /// <summary>
        /// Simplifies a geometry.
        /// </summary>
        public static Geometries.Geometry Simplify(Geometries.Geometry geometry, double tolerance = 0.0)
        {
            return Operators.SimplifyOperator.Instance.Execute(geometry, tolerance);
        }

        /// <summary>
        /// Simplifies a geometry according to OGC specification.
        /// </summary>
        public static Geometries.Geometry SimplifyOGC(Geometries.Geometry geometry, SpatialRef.SpatialReference? spatialRef = null)
        {
            return Operators.SimplifyOGCOperator.Instance.Execute(geometry, spatialRef);
        }

        /// <summary>
        /// Tests if a geometry is simple according to OGC specification.
        /// </summary>
        public static bool IsSimpleOGC(Geometries.Geometry geometry, SpatialRef.SpatialReference? spatialRef = null)
        {
            return Operators.SimplifyOGCOperator.Instance.IsSimpleOGC(geometry, spatialRef);
        }

        /// <summary>
        /// Calculates the centroid (center of mass) of a geometry.
        /// </summary>
        public static Geometries.Point Centroid(Geometries.Geometry geometry)
        {
            return Operators.CentroidOperator.Instance.Execute(geometry);
        }

        /// <summary>
        /// Computes the boundary of a geometry according to OGC specification.
        /// </summary>
        public static Geometries.Geometry Boundary(Geometries.Geometry geometry)
        {
            return Operators.BoundaryOperator.Instance.Execute(geometry);
        }

        /// <summary>
        /// Generalizes a geometry by removing vertices while preserving its general shape.
        /// </summary>
        public static Geometries.Geometry Generalize(Geometries.Geometry geometry, double maxDeviation)
        {
            return Operators.GeneralizeOperator.Instance.Execute(geometry, maxDeviation);
        }

        /// <summary>
        /// Densifies a geometry by adding vertices to ensure no segment exceeds the maximum length.
        /// </summary>
        public static Geometries.Geometry Densify(Geometries.Geometry geometry, double maxSegmentLength)
        {
            return Operators.DensifyOperator.Instance.Execute(geometry, maxSegmentLength);
        }

        /// <summary>
        /// Clips a geometry to an envelope.
        /// </summary>
        public static Geometries.Geometry Clip(Geometries.Geometry geometry, Geometries.Envelope clipEnvelope)
        {
            return Operators.ClipOperator.Instance.Execute(geometry, clipEnvelope);
        }

        /// <summary>
        /// Creates an offset curve or polygon at the specified distance.
        /// </summary>
        public static Geometries.Geometry Offset(Geometries.Geometry geometry, double distance)
        {
            return Operators.OffsetOperator.Instance.Execute(geometry, distance);
        }

        #endregion

        #region Geodesic Operations

        /// <summary>
        /// Calculates the geodesic distance between two points on the WGS84 ellipsoid.
        /// </summary>
        public static double GeodesicDistance(Geometries.Point point1, Geometries.Point point2)
        {
            return Operators.GeodesicDistanceOperator.Instance.Execute(point1, point2);
        }

        /// <summary>
        /// Calculates the geodesic area of a polygon on the WGS84 ellipsoid.
        /// </summary>
        public static double GeodesicArea(Geometries.Polygon polygon)
        {
            return Operators.GeodesicAreaOperator.Instance.Execute(polygon);
        }

        #endregion

        #region Proximity Operations

        /// <summary>
        /// Returns the nearest coordinate on the geometry to the given input point.
        /// </summary>
        /// <param name="geometry">The input geometry.</param>
        /// <param name="inputPoint">The query point.</param>
        /// <param name="testPolygonInterior">
        /// When true and geometry is a polygon, tests if the input point is inside the polygon.
        /// </param>
        /// <returns>Result containing the nearest coordinate and distance information.</returns>
        public static Operators.Proximity2DResult GetNearestCoordinate(Geometries.Geometry geometry, Geometries.Point inputPoint, 
            bool testPolygonInterior = false)
        {
            return Operators.Proximity2DOperator.Instance.GetNearestCoordinate(geometry, inputPoint, testPolygonInterior);
        }

        /// <summary>
        /// Returns the nearest vertex of the geometry to the given input point.
        /// </summary>
        /// <param name="geometry">The input geometry.</param>
        /// <param name="inputPoint">The query point.</param>
        /// <returns>Result containing the nearest vertex and distance information.</returns>
        public static Operators.Proximity2DResult GetNearestVertex(Geometries.Geometry geometry, Geometries.Point inputPoint)
        {
            return Operators.Proximity2DOperator.Instance.GetNearestVertex(geometry, inputPoint);
        }

        /// <summary>
        /// Returns vertices of the geometry that are within the search radius of the given point.
        /// </summary>
        /// <param name="geometry">The input geometry.</param>
        /// <param name="inputPoint">The query point.</param>
        /// <param name="searchRadius">The maximum distance to the query point.</param>
        /// <param name="maxVertexCount">The maximum number of vertices to return.</param>
        /// <returns>Array of results sorted by distance, with the closest vertex first.</returns>
        public static Operators.Proximity2DResult[] GetNearestVertices(Geometries.Geometry geometry, Geometries.Point inputPoint, 
            double searchRadius, int maxVertexCount = int.MaxValue)
        {
            return Operators.Proximity2DOperator.Instance.GetNearestVertices(geometry, inputPoint, searchRadius, maxVertexCount);
        }

        #endregion

        #region Import/Export Operations

        /// <summary>
        /// Exports a geometry to Well-Known Text (WKT) format.
        /// </summary>
        public static string GeometryToWkt(Geometries.Geometry geometry)
        {
            return WktExportOperator.ExportToWkt(geometry);
        }

        /// <summary>
        /// Imports a geometry from Well-Known Text (WKT) format.
        /// </summary>
        public static Geometries.Geometry GeometryFromWkt(string wkt)
        {
            return WktImportOperator.ImportFromWkt(wkt);
        }

        /// <summary>
        /// Exports a geometry to Well-Known Binary (WKB) format.
        /// </summary>
        public static byte[] GeometryToWkb(Geometries.Geometry geometry, bool bigEndian = false)
        {
            return WkbExportOperator.ExportToWkb(geometry, bigEndian);
        }

        /// <summary>
        /// Imports a geometry from Well-Known Binary (WKB) format.
        /// </summary>
        public static Geometries.Geometry GeometryFromWkb(byte[] wkb)
        {
            return WkbImportOperator.ImportFromWkb(wkb);
        }

        /// <summary>
        /// Exports a geometry to GeoJSON format.
        /// </summary>
        public static string GeometryToGeoJson(Geometries.Geometry geometry)
        {
            return GeoJsonExportOperator.ExportToGeoJson(geometry);
        }

        /// <summary>
        /// Imports a geometry from GeoJSON format.
        /// </summary>
        public static Geometries.Geometry GeometryFromGeoJson(string geoJson)
        {
            return GeoJsonImportOperator.ImportFromGeoJson(geoJson);
        }

        /// <summary>
        /// Exports a geometry to Esri JSON format.
        /// </summary>
        public static string GeometryToEsriJson(Geometries.Geometry geometry)
        {
            return EsriJsonExportOperator.Instance.Execute(geometry);
        }

        /// <summary>
        /// Imports a geometry from Esri JSON format.
        /// </summary>
        public static Geometries.Geometry GeometryFromEsriJson(string esriJson)
        {
            return EsriJsonImportOperator.ImportFromEsriJson(esriJson);
        }

        #endregion
    }
}
