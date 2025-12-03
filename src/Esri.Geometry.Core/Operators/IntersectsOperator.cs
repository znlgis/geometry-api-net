using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for testing if two geometries intersect.
/// </summary>
public class IntersectsOperator : IBinaryGeometryOperator<bool>
{
  private static readonly Lazy<IntersectsOperator> _instance = new(() => new IntersectsOperator());

  private IntersectsOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the intersects operator.
  /// </summary>
  public static IntersectsOperator Instance => _instance.Value;

  /// <inheritdoc />
  public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // Simple implementation for envelope-envelope intersection
    if (geometry1 is Envelope env1 && geometry2 is Envelope env2) return env1.Intersects(env2);

    // For other geometry types, this would require more complex implementations
    throw new NotImplementedException(
      $"Intersects test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
  }
}