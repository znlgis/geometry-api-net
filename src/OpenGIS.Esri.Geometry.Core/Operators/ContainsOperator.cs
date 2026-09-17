using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     测试 geometry1 是否包含 geometry2（DE-9IM 关系矩阵推导）。
/// </summary>
public class ContainsOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<ContainsOperator> _instance = new(() => new ContainsOperator());

    private ContainsOperator()
    {
    }

    /// <summary>
    ///     获取 ContainsOperator 的单例实例。
    /// </summary>
    public static ContainsOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return RelateOps.ContainsGeom(geometry1, geometry2);
    }
}
