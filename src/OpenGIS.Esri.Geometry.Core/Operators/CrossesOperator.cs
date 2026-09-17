using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     测试两个几何对象是否交叉。
/// </summary>
public class CrossesOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<CrossesOperator> _instance = new(() => new CrossesOperator());

    private CrossesOperator()
    {
    }

    /// <summary>
    ///     获取 CrossesOperator 的单例实例。
    /// </summary>
    public static CrossesOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return RelateOps.CrossesGeom(geometry1, geometry2);
    }
}
