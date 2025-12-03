using System;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于测试两个几何对象是否重叠的操作符.
///     Geometries overlap if they have the same dimension and their interiors intersect.
/// </summary>
public class OverlapsOperator : IBinaryGeometryOperator<bool>
{
    private static readonly Lazy<OverlapsOperator> _instance = new(() => new OverlapsOperator());

    private OverlapsOperator()
    {
    }

    /// <summary>
    ///     获取 OverlapsOperator 的单例实例.
    /// </summary>
    public static OverlapsOperator Instance => _instance.Value;

    /// <inheritdoc />
    public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
        if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

        // Empty geometries don't overlap
        if (geometry1.IsEmpty || geometry2.IsEmpty) return false;

        // Geometries can only overlap if they have the same dimension
        if (geometry1.Dimension != geometry2.Dimension) return false;

        // For envelopes, overlap means they intersect but neither contains the other
        if (geometry1 is Envelope env1 && geometry2 is Envelope env2)
        {
            // Check if they intersect
            if (!env1.Intersects(env2)) return false;

            // Check if one contains the other
            var env1ContainsEnv2 = env2.XMin >= env1.XMin && env2.XMax <= env1.XMax &&
                                   env2.YMin >= env1.YMin && env2.YMax <= env1.YMax;
            var env2ContainsEnv1 = env1.XMin >= env2.XMin && env1.XMax <= env2.XMax &&
                                   env1.YMin >= env2.YMin && env1.YMax <= env2.YMax;

            // They overlap if they intersect but neither contains the other
            return !env1ContainsEnv2 && !env2ContainsEnv1;
        }

        // For other geometry types, this would require more complex implementations
        throw new NotImplementedException(
            $"Overlaps test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
    }
}