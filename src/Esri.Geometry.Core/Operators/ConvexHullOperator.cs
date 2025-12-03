using System;
using System.Collections.Generic;
using System.Linq;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for computing the convex hull of a geometry.
///   The convex hull is the smallest convex polygon that contains all points in the geometry.
/// </summary>
/// <remarks>
///   Uses the Graham Scan algorithm to compute the convex hull:
///   1. Find the point with the lowest Y coordinate (anchor point)
///   2. Sort all other points by polar angle relative to the anchor
///   3. Process points in order, removing concave turns to maintain convexity
///   
///   Time Complexity: O(n log n) due to sorting
///   Space Complexity: O(n) for the hull and sorted points
///   
///   The result is:
///   - A Polygon if the hull contains 3+ points
///   - A Line if the hull contains exactly 2 points
///   - A Point if the hull contains 1 point
///   - An empty Polygon if the input is empty
/// </remarks>
public class ConvexHullOperator : IGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<ConvexHullOperator> _instance = new(() => new ConvexHullOperator());

  private ConvexHullOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the convex hull operator.
  /// </summary>
  public static ConvexHullOperator Instance => _instance.Value;

  /// <summary>
  ///   Computes the convex hull of a geometry using the Graham Scan algorithm.
  /// </summary>
  /// <param name="geometry">The input geometry. Supports all geometry types.</param>
  /// <param name="spatialRef">Optional spatial reference (currently not used).</param>
  /// <returns>
  ///   The convex hull as:
  ///   - Polygon for 3+ points
  ///   - Line for exactly 2 points
  ///   - Point for exactly 1 point
  ///   - Empty Polygon for empty input
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when geometry is null.</exception>
  /// <example>
  ///   <code>
  ///   var multiPoint = new MultiPoint();
  ///   multiPoint.Add(new Point(0, 0));
  ///   multiPoint.Add(new Point(10, 0));
  ///   multiPoint.Add(new Point(5, 10));
  ///   multiPoint.Add(new Point(5, 5)); // Interior point
  ///   var hull = ConvexHullOperator.Instance.Execute(multiPoint);
  ///   // Returns a triangle polygon with vertices (0,0), (10,0), (5,10)
  ///   </code>
  /// </example>
  public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null) throw new ArgumentNullException(nameof(geometry));

    if (geometry.IsEmpty) return new Polygon();

    var points = ExtractPoints(geometry);
    if (points.Count == 0) return new Polygon();

    if (points.Count == 1) return points[0];

    if (points.Count == 2) return new Line(points[0], points[1]);

    // Compute convex hull using Graham scan algorithm
    var hull = GrahamScan(points);

    if (hull.Count < 3)
    {
      if (hull.Count == 1)
        return hull[0];
      if (hull.Count == 2)
        return new Line(hull[0], hull[1]);
      return new Polygon();
    }

    var polygon = new Polygon();
    // Ensure the ring is closed
    var ring = new List<Point>(hull);
    if (!ring[0].Equals(ring[ring.Count - 1])) ring.Add(ring[0]);
    polygon.AddRing(ring);
    return polygon;
  }

  /// <summary>
  ///   Extracts all points from various geometry types into a flat list.
  /// </summary>
  /// <param name="geometry">The geometry to extract points from.</param>
  /// <returns>A list of all points in the geometry.</returns>
  private List<Point> ExtractPoints(Geometries.Geometry geometry)
  {
    var points = new List<Point>();

    if (geometry is Point point && !point.IsEmpty)
    {
      points.Add(point);
    }
    else if (geometry is MultiPoint multiPoint)
    {
      points.AddRange(multiPoint.GetPoints());
    }
    else if (geometry is Envelope envelope && !envelope.IsEmpty)
    {
      points.Add(new Point(envelope.XMin, envelope.YMin));
      points.Add(new Point(envelope.XMax, envelope.YMin));
      points.Add(new Point(envelope.XMax, envelope.YMax));
      points.Add(new Point(envelope.XMin, envelope.YMax));
    }
    else if (geometry is Polyline polyline)
    {
      foreach (var path in polyline.GetPaths()) points.AddRange(path);
    }
    else if (geometry is Polygon polygon)
    {
      foreach (var ring in polygon.GetRings()) points.AddRange(ring);
    }

    return points;
  }

  /// <summary>
  ///   Implements the Graham Scan algorithm to compute the convex hull.
  ///   This algorithm works by:
  ///   1. Finding the anchor point (lowest Y, then lowest X)
  ///   2. Sorting points by polar angle from the anchor
  ///   3. Processing points in order, maintaining only left turns (convexity)
  /// </summary>
  /// <param name="points">The list of points to process.</param>
  /// <returns>An ordered list of points forming the convex hull.</returns>
  private List<Point> GrahamScan(List<Point> points)
  {
    if (points.Count < 3) return points;

    // Find the point with the lowest Y coordinate (and lowest X if tie)
    var lowestPoint = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();

    // Sort points by polar angle with respect to the lowest point
    var sortedPoints = points
      .Where(p => p != lowestPoint)
      .OrderBy(p => Math.Atan2(p.Y - lowestPoint.Y, p.X - lowestPoint.X))
      .ThenBy(p => p.Distance(lowestPoint))
      .ToList();

    // Build the convex hull
    var hull = new List<Point> { lowestPoint };

    foreach (var point in sortedPoints)
    {
      while (hull.Count > 1 && !IsLeftTurn(hull[hull.Count - 2], hull[hull.Count - 1], point))
        hull.RemoveAt(hull.Count - 1);
      hull.Add(point);
    }

    return hull;
  }

  /// <summary>
  ///   Determines if three points make a left turn (counterclockwise).
  ///   Uses the cross product to test orientation.
  /// </summary>
  /// <param name="p1">The first point.</param>
  /// <param name="p2">The second point (the vertex of the turn).</param>
  /// <param name="p3">The third point.</param>
  /// <returns>True if the points make a left turn, false for a right turn or collinear.</returns>
  private bool IsLeftTurn(Point p1, Point p2, Point p3)
  {
    // Cross product: (p2-p1) × (p3-p1)
    // Positive cross product indicates a left turn (counterclockwise)
    var cross = (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
    return cross > 0;
  }
}