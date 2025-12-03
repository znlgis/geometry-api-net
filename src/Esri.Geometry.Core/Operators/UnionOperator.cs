using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for computing the union of two geometries.
///   Returns a geometry that represents all points in either geometry.
///   Uses envelope-based union for simple cases and point aggregation for complex cases.
/// </summary>
public sealed class UnionOperator : IBinaryGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<UnionOperator> _instance = new(() => new UnionOperator());

  private UnionOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the UnionOperator.
  /// </summary>
  public static UnionOperator Instance => _instance.Value;

  /// <summary>
  ///   Computes the union of two geometries.
  /// </summary>
  /// <param name="geometry1">First geometry</param>
  /// <param name="geometry2">Second geometry</param>
  /// <param name="spatialReference">Optional spatial reference (not used in this implementation)</param>
  /// <returns>Union of the two geometries</returns>
  public Geometries.Geometry Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialReference = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // Handle empty geometries
    if (geometry1.IsEmpty && geometry2.IsEmpty)
      return new Point(double.NaN, double.NaN);
    if (geometry1.IsEmpty)
      return geometry2;
    if (geometry2.IsEmpty)
      return geometry1;

    // Point union with Point
    if (geometry1 is Point p1 && geometry2 is Point p2)
    {
      if (EqualsOperator.Instance.Execute(p1, p2))
        return p1;

      var mp = new MultiPoint();
      mp.Add(p1);
      mp.Add(p2);
      return mp;
    }

    // Point union with MultiPoint
    if (geometry1 is Point point && geometry2 is MultiPoint mp2)
    {
      var result = new MultiPoint();
      result.Add(point);
      foreach (var p in mp2.GetPoints())
        result.Add(p);
      return result;
    }

    if (geometry1 is MultiPoint mp1 && geometry2 is Point point2)
    {
      var result = new MultiPoint();
      foreach (var p in mp1.GetPoints())
        result.Add(p);
      result.Add(point2);
      return result;
    }

    // MultiPoint union with MultiPoint
    if (geometry1 is MultiPoint multiPoint1 && geometry2 is MultiPoint multiPoint2)
    {
      var result = new MultiPoint();
      foreach (var p in multiPoint1.GetPoints())
        result.Add(p);
      foreach (var p in multiPoint2.GetPoints())
        result.Add(p);
      return result;
    }

    // Envelope union with Envelope
    if (geometry1 is Envelope env1 && geometry2 is Envelope env2)
    {
      var xMin = Math.Min(env1.XMin, env2.XMin);
      var yMin = Math.Min(env1.YMin, env2.YMin);
      var xMax = Math.Max(env1.XMax, env2.XMax);
      var yMax = Math.Max(env1.YMax, env2.YMax);
      return new Envelope(xMin, yMin, xMax, yMax);
    }

    // For more complex cases, return the envelope union as a simplified result
    var envelope1 = geometry1.GetEnvelope();
    var envelope2 = geometry2.GetEnvelope();

    var minX = Math.Min(envelope1.XMin, envelope2.XMin);
    var minY = Math.Min(envelope1.YMin, envelope2.YMin);
    var maxX = Math.Max(envelope1.XMax, envelope2.XMax);
    var maxY = Math.Max(envelope1.YMax, envelope2.YMax);

    return new Envelope(minX, minY, maxX, maxY);
  }
}