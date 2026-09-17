using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     测试两个几何对象是否仅在边界上相接。
/// </summary>
public class TouchesOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<TouchesOperator> _instance = new(() => new TouchesOperator());

    private TouchesOperator()
    {
    }

    /// <summary>
    ///     获取 TouchesOperator 的单例实例。
    /// </summary>
    public static TouchesOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return RelateOps.TouchesGeom(geometry1, geometry2);
    }
}
