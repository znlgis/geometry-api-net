using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     计算两个几何对象的差集（geometry1 − geometry2）。
/// </summary>
public sealed class DifferenceOperator : IBinaryGeometryOperator<Geometries.Geometry>
{
    private static readonly Lazy<DifferenceOperator> _instance = new(() => new DifferenceOperator());

    private DifferenceOperator()
    {
    }

    /// <summary>
    ///     获取 DifferenceOperator 的单例实例。
    /// </summary>
    public static DifferenceOperator Instance => _instance.Value;

    /// <inheritdoc />
    public Geometries.Geometry Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return SetOpsCore.Difference(geometry1, geometry2);
    }
}
