using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     计算两个几何对象的并集（面×面为真布尔并，含洞与多部件；混合维度按语义支持或不支持抛错）。
/// </summary>
public sealed class UnionOperator : IBinaryGeometryOperator<Geometries.Geometry>
{
    private static readonly Lazy<UnionOperator> _instance = new(() => new UnionOperator());

    private UnionOperator()
    {
    }

    /// <summary>
    ///     获取 UnionOperator 的单例实例。
    /// </summary>
    public static UnionOperator Instance => _instance.Value;

    /// <inheritdoc />
    public Geometries.Geometry Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return SetOpsCore.Union(geometry1, geometry2);
    }
}
