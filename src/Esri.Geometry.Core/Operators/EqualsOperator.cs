using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for testing if two geometries are spatially equal.
/// </summary>
public class EqualsOperator : IBinaryGeometryOperator<bool>
{
  private static readonly Lazy<EqualsOperator> _instance = new(() => new EqualsOperator());

  private EqualsOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the equals operator.
  /// </summary>
  public static EqualsOperator Instance => _instance.Value;

  /// <inheritdoc />
  public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // Check if types are different
    if (geometry1.Type != geometry2.Type) return false;

    // Check if both are empty
    if (geometry1.IsEmpty && geometry2.IsEmpty) return true;

    // If one is empty and the other is not
    if (geometry1.IsEmpty != geometry2.IsEmpty) return false;

    // Point equality
    if (geometry1 is Point p1 && geometry2 is Point p2) return p1.Equals(p2);

    // Envelope equality
    if (geometry1 is Envelope env1 && geometry2 is Envelope env2)
      return Math.Abs(env1.XMin - env2.XMin) < GeometryConstants.DefaultTolerance &&
             Math.Abs(env1.YMin - env2.YMin) < GeometryConstants.DefaultTolerance &&
             Math.Abs(env1.XMax - env2.XMax) < GeometryConstants.DefaultTolerance &&
             Math.Abs(env1.YMax - env2.YMax) < GeometryConstants.DefaultTolerance;

    // For other geometry types, this would require more complex implementations
    throw new NotImplementedException(
      $"Equals test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
  }
}