using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     测试 geometry1 是否完全位于 geometry2 内部。
/// </summary>
public class WithinOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<WithinOperator> _instance = new(() => new WithinOperator());

    private WithinOperator()
    {
    }

    /// <summary>
    ///     获取 WithinOperator 的单例实例。
    /// </summary>
    public static WithinOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return RelateOps.WithinGeom(geometry1, geometry2);
    }
}
