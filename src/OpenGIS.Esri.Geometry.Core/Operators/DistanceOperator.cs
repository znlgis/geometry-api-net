using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于计算几何对象之间最小平面距离的操作符。
///     相交（含包含/相接）返回 0；空几何返回 NaN。
/// </summary>
public class DistanceOperator : IBinaryGeometryOperator<double>
{
    private static readonly Lazy<DistanceOperator> _instance = new(() => new DistanceOperator());

    private DistanceOperator()
    {
    }

    /// <summary>
    ///     获取 DistanceOperator 的单例实例.
    /// </summary>
    public static DistanceOperator Instance => _instance.Value;

    /// <inheritdoc />
    public double Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return RelateOps.DistanceGeom(geometry1, geometry2);
    }
}
