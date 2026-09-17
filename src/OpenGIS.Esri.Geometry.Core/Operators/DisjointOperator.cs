using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     测试两个几何对象是否不相交。
/// </summary>
public class DisjointOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<DisjointOperator> _instance = new(() => new DisjointOperator());

    private DisjointOperator()
    {
    }

    /// <summary>
    ///     获取 DisjointOperator 的单例实例。
    /// </summary>
    public static DisjointOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        return RelateOps.DisjointGeom(geometry1, geometry2);
    }
}
