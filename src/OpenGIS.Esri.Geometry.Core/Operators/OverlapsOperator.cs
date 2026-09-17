using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     测试两个同维几何对象是否部分重叠。
/// </summary>
public class OverlapsOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<OverlapsOperator> _instance = new(() => new OverlapsOperator());

    private OverlapsOperator()
    {
    }

    /// <summary>
    ///     获取 OverlapsOperator 的单例实例。
    /// </summary>
    public static OverlapsOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return RelateOps.OverlapsGeom(geometry1, geometry2);
    }
}
