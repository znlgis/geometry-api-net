using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for testing if two geometries are spatially disjoint (do not intersect).
/// </summary>
public class DisjointOperator : IBinaryGeometryOperator<bool>
{
  private static readonly Lazy<DisjointOperator> _instance = new(() => new DisjointOperator());

  private DisjointOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the disjoint operator.
  /// </summary>
  public static DisjointOperator Instance => _instance.Value;

  /// <inheritdoc />
  public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // Empty geometries are considered disjoint
    if (geometry1.IsEmpty || geometry2.IsEmpty) return true;

    // Disjoint is the opposite of intersects
    // For envelopes, we can use the envelope intersection test
    if (geometry1 is Envelope env1 && geometry2 is Envelope env2) return !env1.Intersects(env2);

    // For point-envelope test
    if (geometry1 is Point p && geometry2 is Envelope env) return !env.Contains(p);

    if (geometry1 is Envelope env3 && geometry2 is Point p2) return !env3.Contains(p2);

    // For other geometry types, use the intersects operator
    try
    {
      return !IntersectsOperator.Instance.Execute(geometry1, geometry2, spatialRef);
    }
    catch (NotImplementedException)
    {
      throw new NotImplementedException(
        $"Disjoint test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
    }
  }
}