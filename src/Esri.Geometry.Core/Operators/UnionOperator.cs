using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for computing the union of two geometries.
///   Returns a geometry that represents all points in either geometry.
/// </summary>
/// <remarks>
///   Union operation (A ∪ B) combines two geometries into one containing all points from both.
///   
///   Supported combinations:
///   - Point ∪ Point → Point (if equal) or MultiPoint (if different)
///   - Point ∪ MultiPoint → MultiPoint (aggregated points)
///   - MultiPoint ∪ MultiPoint → MultiPoint (all points combined)
///   - Envelope ∪ Envelope → Envelope (bounding rectangle of both)
///   - Other combinations → Simplified envelope-based result
///   
///   Implementation notes:
///   - This is a simplified implementation for basic geometry types
///   - Full polygon union requires complex topology algorithms (not yet implemented)
///   - For envelopes, returns the minimum bounding rectangle of both
///   - For points, aggregates into MultiPoint collections
///   
///   Time Complexity: O(n + m) where n and m are the number of points in each geometry
/// </remarks>
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
  ///   Computes the union of two geometries (all points in either geometry).
  /// </summary>
  /// <param name="geometry1">First geometry to union.</param>
  /// <param name="geometry2">Second geometry to union.</param>
  /// <param name="spatialReference">Optional spatial reference (currently not used).</param>
  /// <returns>
  ///   A geometry representing the union of the inputs:
  ///   - Point or MultiPoint for point-based inputs
  ///   - Envelope for envelope-based inputs
  ///   - Simplified envelope union for complex geometry combinations
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when either geometry is null.</exception>
  /// <example>
  ///   <code>
  ///   // Union of two points
  ///   var p1 = new Point(0, 0);
  ///   var p2 = new Point(10, 10);
  ///   var union = UnionOperator.Instance.Execute(p1, p2);
  ///   // Result: MultiPoint with both points
  ///   
  ///   // Union of two envelopes
  ///   var env1 = new Envelope(0, 0, 10, 10);
  ///   var env2 = new Envelope(5, 5, 15, 15);
  ///   var envUnion = UnionOperator.Instance.Execute(env1, env2);
  ///   // Result: Envelope(0, 0, 15, 15)
  ///   </code>
  /// </example>
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