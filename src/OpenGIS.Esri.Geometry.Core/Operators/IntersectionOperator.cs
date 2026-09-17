using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     计算两个几何对象的交集。
/// </summary>
public sealed class IntersectionOperator : IBinaryGeometryOperator<Geometries.Geometry>
{
    private static readonly Lazy<IntersectionOperator> _instance = new(() => new IntersectionOperator());

    private IntersectionOperator()
    {
    }

    /// <summary>
    ///     获取 IntersectionOperator 的单例实例。
    /// </summary>
    public static IntersectionOperator Instance => _instance.Value;

    /// <inheritdoc />
    public Geometries.Geometry Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return SetOpsCore.Intersection(geometry1, geometry2);
    }
}
