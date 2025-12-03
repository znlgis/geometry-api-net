using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.IO;
using OpenGIS.Esri.Geometry.Core.Operators;

namespace OpenGIS.Esri.Geometry.Core;

using SpatialReference_SpatialReference = SpatialReference.SpatialReference;

/// <summary>
///     提供简化的几何操作静态 API。
///     此类使用方便的静态方法包装所有操作符实例。
/// </summary>
/// <remarks>
///     GeometryEngine 提供了比直接使用操作符更简单的 API。
///     对于高级场景或批量操作的更好性能，
///     请考虑直接使用操作符类（例如 UnionOperator、BufferOperator）。
/// </remarks>
public static class GeometryEngine
{
    #region 空间关系操作

    /// <summary>
    ///     测试 geometry1 是否包含 geometry2。
    /// </summary>
    public static bool Contains(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return ContainsOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     测试两个几何对象是否相交。
    /// </summary>
    public static bool Intersects(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return IntersectsOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     计算两个几何对象之间的距离。
    /// </summary>
    public static double Distance(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return DistanceOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     测试两个几何对象在空间上是否相等。
    /// </summary>
    public static bool Equals(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return EqualsOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     测试两个几何对象是否不相交（分离）。
    /// </summary>
    public static bool Disjoint(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return DisjointOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     测试 geometry1 是否在 geometry2 内部。
    /// </summary>
    public static bool Within(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return WithinOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     测试两个几何对象是否交叉。
    /// </summary>
    public static bool Crosses(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return CrossesOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     测试两个几何对象是否在边界处相接。
    /// </summary>
    public static bool Touches(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return TouchesOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     测试相同维度的两个几何对象是否重叠。
    /// </summary>
    public static bool Overlaps(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return OverlapsOperator.Instance.Execute(geometry1, geometry2);
    }

    #endregion

    #region 集合操作

    /// <summary>
    ///     计算两个几何对象的并集。
    /// </summary>
    public static Geometries.Geometry Union(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return UnionOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     计算两个几何对象的交集。
    /// </summary>
    public static Geometries.Geometry Intersection(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return IntersectionOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     计算两个几何对象的差集（geometry1 - geometry2）。
    /// </summary>
    public static Geometries.Geometry Difference(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return DifferenceOperator.Instance.Execute(geometry1, geometry2);
    }

    /// <summary>
    ///     计算两个几何对象的对称差。
    /// </summary>
    public static Geometries.Geometry SymmetricDifference(Geometries.Geometry geometry1, Geometries.Geometry geometry2)
    {
        return SymmetricDifferenceOperator.Instance.Execute(geometry1, geometry2);
    }

    #endregion

    #region 几何操作

    /// <summary>
    ///     围绕几何对象创建缓冲区。
    /// </summary>
    public static Geometries.Geometry Buffer(Geometries.Geometry geometry, double distance)
    {
        return BufferOperator.Instance.Execute(geometry, distance);
    }

    /// <summary>
    ///     计算几何对象的凸包。
    /// </summary>
    public static Geometries.Geometry ConvexHull(Geometries.Geometry geometry)
    {
        return ConvexHullOperator.Instance.Execute(geometry);
    }

    /// <summary>
    ///     计算几何对象的面积。
    /// </summary>
    public static double Area(Geometries.Geometry geometry)
    {
        return AreaOperator.Instance.Execute(geometry);
    }

    /// <summary>
    ///     计算几何对象的长度或周长。
    /// </summary>
    public static double Length(Geometries.Geometry geometry)
    {
        return LengthOperator.Instance.Execute(geometry);
    }

    /// <summary>
    ///     简化几何对象。
    /// </summary>
    public static Geometries.Geometry Simplify(Geometries.Geometry geometry, double tolerance = 0.0)
    {
        return SimplifyOperator.Instance.Execute(geometry, tolerance);
    }

    /// <summary>
    ///     根据 OGC 规范简化几何对象。
    /// </summary>
    public static Geometries.Geometry SimplifyOGC(Geometries.Geometry geometry,
        SpatialReference_SpatialReference? spatialRef = null)
    {
        return SimplifyOGCOperator.Instance.Execute(geometry, spatialRef);
    }

    /// <summary>
    ///     根据 OGC 规范测试几何对象是否简单。
    /// </summary>
    public static bool IsSimpleOGC(Geometries.Geometry geometry, SpatialReference_SpatialReference? spatialRef = null)
    {
        return SimplifyOGCOperator.Instance.IsSimpleOGC(geometry, spatialRef);
    }

    /// <summary>
    ///     计算几何对象的质心（质量中心）。
    /// </summary>
    public static Point Centroid(Geometries.Geometry geometry)
    {
        return CentroidOperator.Instance.Execute(geometry);
    }

    /// <summary>
    ///     根据 OGC 规范计算几何对象的边界。
    /// </summary>
    public static Geometries.Geometry Boundary(Geometries.Geometry geometry)
    {
        return BoundaryOperator.Instance.Execute(geometry);
    }

    /// <summary>
    ///     通过删除顶点来概化几何对象，同时保留其总体形状。
    /// </summary>
    public static Geometries.Geometry Generalize(Geometries.Geometry geometry, double maxDeviation)
    {
        return GeneralizeOperator.Instance.Execute(geometry, maxDeviation);
    }

    /// <summary>
    ///     通过添加顶点来密化几何对象，确保没有线段超过最大长度。
    /// </summary>
    public static Geometries.Geometry Densify(Geometries.Geometry geometry, double maxSegmentLength)
    {
        return DensifyOperator.Instance.Execute(geometry, maxSegmentLength);
    }

    /// <summary>
    ///     将几何对象裁剪到包络范围内。
    /// </summary>
    public static Geometries.Geometry Clip(Geometries.Geometry geometry, Envelope clipEnvelope)
    {
        return ClipOperator.Instance.Execute(geometry, clipEnvelope);
    }

    /// <summary>
    ///     在指定距离处创建偏移曲线或多边形。
    /// </summary>
    public static Geometries.Geometry Offset(Geometries.Geometry geometry, double distance)
    {
        return OffsetOperator.Instance.Execute(geometry, distance);
    }

    #endregion

    #region 大地测量操作

    /// <summary>
    ///     计算 WGS84 椭球上两点之间的大地测量距离。
    /// </summary>
    public static double GeodesicDistance(Point point1, Point point2)
    {
        return GeodesicDistanceOperator.Instance.Execute(point1, point2);
    }

    /// <summary>
    ///     计算 WGS84 椭球上多边形的大地测量面积。
    /// </summary>
    public static double GeodesicArea(Polygon polygon)
    {
        return GeodesicAreaOperator.Instance.Execute(polygon);
    }

    #endregion

    #region 邻近操作

    /// <summary>
    ///     返回几何对象上最接近给定输入点的坐标。
    /// </summary>
    /// <param name="geometry">输入几何对象。</param>
    /// <param name="inputPoint">查询点。</param>
    /// <param name="testPolygonInterior">
    ///     当为 true 且几何对象是多边形时，测试输入点是否在多边形内部。
    /// </param>
    /// <returns>包含最近坐标和距离信息的结果。</returns>
    public static Proximity2DResult GetNearestCoordinate(Geometries.Geometry geometry, Point inputPoint,
        bool testPolygonInterior = false)
    {
        return Proximity2DOperator.Instance.GetNearestCoordinate(geometry, inputPoint, testPolygonInterior);
    }

    /// <summary>
    ///     返回几何对象上最接近给定输入点的顶点。
    /// </summary>
    /// <param name="geometry">输入几何对象。</param>
    /// <param name="inputPoint">查询点。</param>
    /// <returns>包含最近顶点和距离信息的结果。</returns>
    public static Proximity2DResult GetNearestVertex(Geometries.Geometry geometry, Point inputPoint)
    {
        return Proximity2DOperator.Instance.GetNearestVertex(geometry, inputPoint);
    }

    /// <summary>
    ///     返回在给定点的搜索半径内的几何对象顶点。
    /// </summary>
    /// <param name="geometry">输入几何对象。</param>
    /// <param name="inputPoint">查询点。</param>
    /// <param name="searchRadius">到查询点的最大距离。</param>
    /// <param name="maxVertexCount">返回的最大顶点数量。</param>
    /// <returns>按距离排序的结果数组，最近的顶点排在最前面。</returns>
    public static Proximity2DResult[] GetNearestVertices(Geometries.Geometry geometry, Point inputPoint,
        double searchRadius, int maxVertexCount = int.MaxValue)
    {
        return Proximity2DOperator.Instance.GetNearestVertices(geometry, inputPoint, searchRadius, maxVertexCount);
    }

    #endregion

    #region 导入/导出操作

    /// <summary>
    ///     将几何对象导出为 Well-Known Text (WKT) 格式。
    /// </summary>
    public static string GeometryToWkt(Geometries.Geometry geometry)
    {
        return WktExportOperator.ExportToWkt(geometry);
    }

    /// <summary>
    ///     从 Well-Known Text (WKT) 格式导入几何对象。
    /// </summary>
    public static Geometries.Geometry GeometryFromWkt(string wkt)
    {
        return WktImportOperator.ImportFromWkt(wkt);
    }

    /// <summary>
    ///     将几何对象导出为 Well-Known Binary (WKB) 格式。
    /// </summary>
    public static byte[] GeometryToWkb(Geometries.Geometry geometry, bool bigEndian = false)
    {
        return WkbExportOperator.ExportToWkb(geometry, bigEndian);
    }

    /// <summary>
    ///     从 Well-Known Binary (WKB) 格式导入几何对象。
    /// </summary>
    public static Geometries.Geometry GeometryFromWkb(byte[] wkb)
    {
        return WkbImportOperator.ImportFromWkb(wkb);
    }

    /// <summary>
    ///     将几何对象导出为 GeoJSON 格式。
    /// </summary>
    public static string GeometryToGeoJson(Geometries.Geometry geometry)
    {
        return GeoJsonExportOperator.ExportToGeoJson(geometry);
    }

    /// <summary>
    ///     从 GeoJSON 格式导入几何对象。
    /// </summary>
    public static Geometries.Geometry GeometryFromGeoJson(string geoJson)
    {
        return GeoJsonImportOperator.ImportFromGeoJson(geoJson);
    }

    /// <summary>
    ///     将几何对象导出为 Esri JSON 格式。
    /// </summary>
    public static string GeometryToEsriJson(Geometries.Geometry geometry)
    {
        return EsriJsonExportOperator.Instance.Execute(geometry);
    }

    /// <summary>
    ///     从 Esri JSON 格式导入几何对象。
    /// </summary>
    public static Geometries.Geometry GeometryFromEsriJson(string esriJson)
    {
        return EsriJsonImportOperator.ImportFromEsriJson(esriJson);
    }

    #endregion
}