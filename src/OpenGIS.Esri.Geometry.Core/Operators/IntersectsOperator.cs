using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     测试两个几何对象是否相交。
/// </summary>
public class IntersectsOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<IntersectsOperator> _instance = new(() => new IntersectsOperator());

    private IntersectsOperator()
    {
    }

    /// <summary>
    ///     获取 IntersectsOperator 的单例实例。
    /// </summary>
    public static IntersectsOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return RelateOps.IntersectsGeom(geometry1, geometry2);
    }
}
