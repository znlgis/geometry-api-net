namespace OpenGIS.Esri.Geometry.Core.Geometries;

/// <summary>
///     定义 API 支持的几何类型。
/// </summary>
public enum GeometryType
{
    /// <summary>
    ///     未知几何类型。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     点几何类型。
    /// </summary>
    Point = 1,

    /// <summary>
    ///     线几何类型（两点之间的线段）。
    /// </summary>
    Line = 2,

    /// <summary>
    ///     包络（边界矩形）几何类型。
    /// </summary>
    Envelope = 3,

    /// <summary>
    ///     多点几何类型。
    /// </summary>
    MultiPoint = 4,

    /// <summary>
    ///     折线几何类型。
    /// </summary>
    Polyline = 5,

    /// <summary>
    ///     多边形几何类型。
    /// </summary>
    Polygon = 6
}