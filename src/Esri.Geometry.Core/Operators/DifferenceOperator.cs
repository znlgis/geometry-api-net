using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   用于计算两个几何对象差集的操作符.
///   返回表示第一个几何对象中不在第二个几何对象中的所有点的几何对象.
///   Implements difference for basic geometry types.
/// </summary>
public sealed class DifferenceOperator : IBinaryGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<DifferenceOperator> _instance = new(() => new DifferenceOperator());

  private DifferenceOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the DifferenceOperator.
  /// </summary>
  public static DifferenceOperator Instance => _instance.Value;

  /// <summary>
  ///   计算两个几何对象的差集（geometry1 - geometry2）.
  /// </summary>
  /// <param name="geometry1">First geometry</param>
  /// <param name="geometry2">Second geometry (to subtract)</param>
  /// <param name="spatialReference">Optional spatial reference (not used in this implementation)</param>
  /// <returns>Difference of the two geometries</returns>
  public Geometries.Geometry Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialReference = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // If first geometry is empty, return empty
    if (geometry1.IsEmpty)
      return new Point(double.NaN, double.NaN);

    // If second geometry is empty, return first geometry
    if (geometry2.IsEmpty)
      return geometry1;

    // Point difference with Point
    if (geometry1 is Point p1 && geometry2 is Point p2)
    {
      if (EqualsOperator.Instance.Execute(p1, p2))
        return new Point(double.NaN, double.NaN);
      return p1;
    }

    // Point difference with Envelope
    if (geometry1 is Point point && geometry2 is Envelope envelope)
    {
      if (ContainsOperator.Instance.Execute(envelope, point))
        return new Point(double.NaN, double.NaN);
      return point;
    }

    // MultiPoint difference with Point
    if (geometry1 is MultiPoint mp && geometry2 is Point pt)
    {
      var result = new MultiPoint();
      foreach (var p in mp.GetPoints())
        if (!EqualsOperator.Instance.Execute(p, pt))
          result.Add(p);

      if (result.Count == 0)
        return new Point(double.NaN, double.NaN);
      if (result.Count == 1)
        return result.GetPoint(0);
      return result;
    }

    // MultiPoint difference with MultiPoint
    if (geometry1 is MultiPoint multiPoint1 && geometry2 is MultiPoint multiPoint2)
    {
      var result = new MultiPoint();
      foreach (var pt1 in multiPoint1.GetPoints())
      {
        var found = false;
        foreach (var pt2 in multiPoint2.GetPoints())
          if (EqualsOperator.Instance.Execute(pt1, pt2))
          {
            found = true;
            break;
          }

        if (!found)
          result.Add(pt1);
      }

      if (result.Count == 0)
        return new Point(double.NaN, double.NaN);
      if (result.Count == 1)
        return result.GetPoint(0);
      return result;
    }

    // MultiPoint difference with Envelope
    if (geometry1 is MultiPoint multiPoint && geometry2 is Envelope env)
    {
      var result = new MultiPoint();
      foreach (var p in multiPoint.GetPoints())
        if (!ContainsOperator.Instance.Execute(env, p))
          result.Add(p);

      if (result.Count == 0)
        return new Point(double.NaN, double.NaN);
      if (result.Count == 1)
        return result.GetPoint(0);
      return result;
    }

    // Envelope difference - simplified implementation
    // For full envelope difference with envelope, we'd need to compute the regions
    // This is a simplified version that returns the first envelope if they don't overlap
    if (geometry1 is Envelope env1 && geometry2 is Envelope env2)
    {
      // If they don't intersect, return first envelope
      if (!IntersectsOperator.Instance.Execute(env1, env2))
        return env1;

      // If env2 completely contains env1, return empty
      if (ContainsOperator.Instance.Execute(env2, env1))
        return new Point(double.NaN, double.NaN);

      // Otherwise return env1 (simplified - full implementation would require polygon operations)
      return env1;
    }

    // For other complex cases, if geometry2 contains geometry1, return empty
    // Otherwise return geometry1 (simplified implementation)
    if (ContainsOperator.Instance.Execute(geometry2, geometry1))
      return new Point(double.NaN, double.NaN);

    return geometry1;
  }
}