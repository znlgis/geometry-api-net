using System;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于测试两个几何对象是否交叉的操作符.
///     如果几何对象有一些但不是全部内部点相同，则它们交叉.
/// </summary>
public class CrossesOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<CrossesOperator> _instance = new(() => new CrossesOperator());

    private CrossesOperator()
    {
    }

    /// <summary>
    ///     获取 CrossesOperator 的单例实例.
    /// </summary>
    public static CrossesOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        // Empty geometries don't cross
        if (geometry1.IsEmpty || geometry2.IsEmpty) return false;

        // Crosses applies mainly to line/line, line/polygon, and point/line relationships
        // For the basic implementation, we'll focus on envelope intersections

        // Two geometries of the same dimension don't typically "cross"
        if (geometry1.Dimension == geometry2.Dimension) return false;

        // For other geometry types, this would require more complex implementations
        // Proper crossing detection requires analyzing the interior and boundary of geometries
        throw new NotImplementedException(
            $"Crosses test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
    }
}