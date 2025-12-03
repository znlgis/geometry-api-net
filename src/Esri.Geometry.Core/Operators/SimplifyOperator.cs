using System;
using System.Collections.Generic;
using System.Linq;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for simplifying geometries using the Douglas-Peucker algorithm.
///   This algorithm reduces the number of vertices in a polyline or polygon while preserving its general shape.
/// </summary>
/// <remarks>
///   The Douglas-Peucker algorithm is a recursive line simplification algorithm that:
///   1. Finds the point furthest from the line segment connecting the endpoints
///   2. If this distance is greater than the tolerance, splits the line at that point and recurses
///   3. Otherwise, removes all intermediate points
///   
///   Time Complexity: O(n log n) average case, O(n²) worst case
///   Space Complexity: O(n) for recursion stack
/// </remarks>
public class SimplifyOperator : IGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<SimplifyOperator> _instance = new(() => new SimplifyOperator());

  private SimplifyOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the simplify operator.
  /// </summary>
  public static SimplifyOperator Instance => _instance.Value;

  /// <inheritdoc />
  public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    throw new NotImplementedException(
      "Simplify operator requires a tolerance parameter. Use Execute(geometry, tolerance, spatialRef) instead.");
  }

  /// <summary>
  ///   Simplifies a geometry using the Douglas-Peucker algorithm.
  ///   Reduces the number of vertices while preserving the general shape within the specified tolerance.
  /// </summary>
  /// <param name="geometry">The geometry to simplify. Supports Point, Polyline, and Polygon types.</param>
  /// <param name="tolerance">The maximum perpendicular distance a vertex can be from the simplified line.
  ///   Larger values result in more aggressive simplification.</param>
  /// <param name="spatialRef">Optional spatial reference (currently not used in simplification).</param>
  /// <returns>A simplified geometry with fewer vertices. Points are returned unchanged.
  ///   Empty geometries are returned as-is.</returns>
  /// <exception cref="ArgumentNullException">Thrown when geometry is null.</exception>
  /// <exception cref="ArgumentException">Thrown when tolerance is not positive.</exception>
  /// <example>
  ///   <code>
  ///   var polyline = new Polyline();
  ///   polyline.AddPath(new[] { new Point(0,0), new Point(1,0.1), new Point(2,0) });
  ///   var simplified = SimplifyOperator.Instance.Execute(polyline, 0.2);
  ///   // Result will have only 2 points: (0,0) and (2,0)
  ///   </code>
  /// </example>
  public Geometries.Geometry Execute(Geometries.Geometry geometry, double tolerance,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null) throw new ArgumentNullException(nameof(geometry));

    if (geometry.IsEmpty) return geometry;

    if (tolerance <= 0) throw new ArgumentException("Tolerance must be positive.", nameof(tolerance));

    // Points don't need simplification
    if (geometry is Point) return geometry;

    // Simplify polyline
    if (geometry is Polyline polyline) return SimplifyPolyline(polyline, tolerance);

    // Simplify polygon
    if (geometry is Polygon polygon) return SimplifyPolygon(polygon, tolerance);

    // For other types, return as-is
    return geometry;
  }

  /// <summary>
  ///   Simplifies a polyline by applying the Douglas-Peucker algorithm to each path.
  /// </summary>
  /// <param name="polyline">The polyline to simplify.</param>
  /// <param name="tolerance">The simplification tolerance.</param>
  /// <returns>A simplified polyline with fewer vertices per path.</returns>
  private Polyline SimplifyPolyline(Polyline polyline, double tolerance)
  {
    var simplified = new Polyline();
    foreach (var path in polyline.GetPaths())
    {
      var simplifiedPath = DouglasPeucker(path.ToList(), tolerance);
      // Only add paths with at least 2 points (minimum for a valid path)
      if (simplifiedPath.Count >= 2) simplified.AddPath(simplifiedPath);
    }

    return simplified;
  }

  /// <summary>
  ///   Simplifies a polygon by applying the Douglas-Peucker algorithm to each ring.
  /// </summary>
  /// <param name="polygon">The polygon to simplify.</param>
  /// <param name="tolerance">The simplification tolerance.</param>
  /// <returns>A simplified polygon with fewer vertices per ring.</returns>
  private Polygon SimplifyPolygon(Polygon polygon, double tolerance)
  {
    var simplified = new Polygon();
    foreach (var ring in polygon.GetRings())
    {
      var simplifiedRing = DouglasPeucker(ring.ToList(), tolerance);
      // Ensure ring has at least 3 points plus closing point (minimum for a valid polygon ring)
      if (simplifiedRing.Count >= 3)
      {
        // Ensure the ring is closed (first and last points must be the same)
        if (!simplifiedRing[0].Equals(simplifiedRing[simplifiedRing.Count - 1])) simplifiedRing.Add(simplifiedRing[0]);
        simplified.AddRing(simplifiedRing);
      }
    }

    return simplified;
  }

  /// <summary>
  ///   Implements the Douglas-Peucker line simplification algorithm recursively.
  ///   This algorithm finds the point with maximum perpendicular distance from the line
  ///   connecting the first and last points. If this distance exceeds the tolerance,
  ///   the line is split at that point and both segments are processed recursively.
  /// </summary>
  /// <param name="points">The list of points to simplify.</param>
  /// <param name="tolerance">The maximum allowed perpendicular distance.</param>
  /// <returns>A simplified list of points that approximates the original line.</returns>
  private List<Point> DouglasPeucker(List<Point> points, double tolerance)
  {
    if (points.Count < 3) return points;

    // Find the point with maximum distance from the line segment
    double maxDistance = 0;
    var maxIndex = 0;
    var end = points.Count - 1;

    for (var i = 1; i < end; i++)
    {
      var distance = PerpendicularDistance(points[i], points[0], points[end]);
      if (distance > maxDistance)
      {
        maxDistance = distance;
        maxIndex = i;
      }
    }

    // If max distance is greater than tolerance, recursively simplify
    if (maxDistance > tolerance)
    {
      // Recursive call
      var leftSegment = DouglasPeucker(points.GetRange(0, maxIndex + 1), tolerance);
      var rightSegment = DouglasPeucker(points.GetRange(maxIndex, end - maxIndex + 1), tolerance);

      // Combine results (removing duplicate point at maxIndex)
      var result = new List<Point>(leftSegment);
      result.AddRange(rightSegment.Skip(1));
      return result;
    }

    // Return only the endpoints
    return new List<Point> { points[0], points[end] };
  }

  /// <summary>
  ///   Calculates the perpendicular distance from a point to a line segment.
  ///   Uses vector projection to find the closest point on the line segment.
  /// </summary>
  /// <param name="point">The point to measure distance from.</param>
  /// <param name="lineStart">The start point of the line segment.</param>
  /// <param name="lineEnd">The end point of the line segment.</param>
  /// <returns>The perpendicular distance from the point to the line segment.</returns>
  private double PerpendicularDistance(Point point, Point lineStart, Point lineEnd)
  {
    var dx = lineEnd.X - lineStart.X;
    var dy = lineEnd.Y - lineStart.Y;

    // Calculate the magnitude of the line segment
    var mag = Math.Sqrt(dx * dx + dy * dy);
    // If line segment is effectively a point, return distance to that point
    if (mag < GeometryConstants.Epsilon) return point.Distance(lineStart);

    // Calculate the projection parameter u (0 to 1 represents a point on the segment)
    var u = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (mag * mag);

    // If projection is before the start of the segment
    if (u < 0) return point.Distance(lineStart);

    // If projection is after the end of the segment
    if (u > 1) return point.Distance(lineEnd);

    // Calculate the intersection point on the line segment
    var ix = lineStart.X + u * dx;
    var iy = lineStart.Y + u * dy;
    return point.Distance(new Point(ix, iy));
  }
}