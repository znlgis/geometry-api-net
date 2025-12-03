using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for testing if two geometries touch at their boundaries but do not overlap.
/// </summary>
public class TouchesOperator : IBinaryGeometryOperator<bool>
{
  private static readonly Lazy<TouchesOperator> _instance = new(() => new TouchesOperator());

  private TouchesOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the touches operator.
  /// </summary>
  public static TouchesOperator Instance => _instance.Value;

  /// <inheritdoc />
  public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // Empty geometries don't touch
    if (geometry1.IsEmpty || geometry2.IsEmpty) return false;

    // Point cannot touch a point (they either intersect or are disjoint)
    if (geometry1 is Point && geometry2 is Point) return false;

    // Check if point touches envelope boundary
    if (geometry1 is Point p && geometry2 is Envelope env)
    {
      // Point touches envelope if it's on the boundary but not inside
      var onBoundary = (Math.Abs(p.X - env.XMin) < GeometryConstants.DefaultTolerance ||
                        Math.Abs(p.X - env.XMax) < GeometryConstants.DefaultTolerance) &&
                       p.Y >= env.YMin && p.Y <= env.YMax;
      onBoundary = onBoundary || ((Math.Abs(p.Y - env.YMin) < GeometryConstants.DefaultTolerance ||
                                   Math.Abs(p.Y - env.YMax) < GeometryConstants.DefaultTolerance) &&
                                  p.X >= env.XMin && p.X <= env.XMax);
      return onBoundary;
    }

    if (geometry1 is Envelope env2 && geometry2 is Point p2) return Execute(p2, env2, spatialRef);

    // Check if envelopes touch (share boundary but don't overlap)
    if (geometry1 is Envelope env3 && geometry2 is Envelope env4)
    {
      // Two envelopes touch if they share an edge but don't overlap
      var touchesX = (Math.Abs(env3.XMax - env4.XMin) < GeometryConstants.DefaultTolerance ||
                      Math.Abs(env3.XMin - env4.XMax) < GeometryConstants.DefaultTolerance) &&
                     !(env3.YMax < env4.YMin || env3.YMin > env4.YMax);
      var touchesY = (Math.Abs(env3.YMax - env4.YMin) < GeometryConstants.DefaultTolerance ||
                      Math.Abs(env3.YMin - env4.YMax) < GeometryConstants.DefaultTolerance) &&
                     !(env3.XMax < env4.XMin || env3.XMin > env4.XMax);
      return touchesX || touchesY;
    }

    // For other geometry types, this would require more complex implementations
    throw new NotImplementedException(
      $"Touches test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
  }
}