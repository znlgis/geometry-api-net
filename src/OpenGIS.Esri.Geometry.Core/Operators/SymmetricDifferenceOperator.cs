using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     计算两个几何对象的对称差 ((A−B) ∪ (B−A))。
/// </summary>
public sealed class SymmetricDifferenceOperator : IBinaryGeometryOperator<Geometries.Geometry>
{
    private static readonly Lazy<SymmetricDifferenceOperator> _instance = new(() => new SymmetricDifferenceOperator());

    private SymmetricDifferenceOperator()
    {
    }

    /// <summary>
    ///     获取 SymmetricDifferenceOperator 的单例实例。
    /// </summary>
    public static SymmetricDifferenceOperator Instance => _instance.Value;

    /// <inheritdoc />
    public Geometries.Geometry Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return SetOpsCore.SymmetricDifference(geometry1, geometry2);
    }
}
