using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   Represents a polygon geometry consisting of one or more rings.
///   A polygon is a closed 2D shape defined by one exterior ring and zero or more interior rings (holes).
/// </summary>
/// <remarks>
///   Polygon structure:
///   - The first ring is typically the exterior boundary (outer ring)
///   - Subsequent rings represent holes (interior rings)
///   - Rings must be closed (first point equals last point)
///   - Exterior rings should be oriented counter-clockwise
///   - Hole rings should be oriented clockwise (by OGC convention)
///   
///   Area calculation:
///   - Uses the shoelace formula (Gauss's area formula)
///   - Works for simple polygons without self-intersections
///   - Takes absolute value to handle different ring orientations
///   
///   Example usage:
///   - Creating a simple rectangle
///   - Creating a polygon with a hole
///   - Multi-part polygons (multiple exterior rings)
/// </remarks>
public class Polygon : Geometry
{
  private readonly List<List<Point>> _rings;

  /// <summary>
  ///   Initializes a new instance of the <see cref="Polygon" /> class.
  /// </summary>
  public Polygon()
  {
    _rings = new List<List<Point>>();
  }

  /// <inheritdoc />
  public override GeometryType Type => GeometryType.Polygon;

  /// <inheritdoc />
  public override bool IsEmpty
  {
    get
    {
      if (_rings.Count == 0)
        return true;

      foreach (var ring in _rings)
        if (ring.Count > 0)
          return false;
      return true;
    }
  }

  /// <inheritdoc />
  public override int Dimension => 2;

  /// <summary>
  ///   Gets the number of rings in the polygon.
  /// </summary>
  public int RingCount => _rings.Count;

  /// <summary>
  ///   Calculates the area of the polygon using the shoelace formula (Gauss's area formula).
  ///   Sums the areas of all rings (exterior and interior).
  /// </summary>
  /// <remarks>
  ///   The shoelace formula: Area = 0.5 * |Σ(x[i] * y[i+1] - x[i+1] * y[i])|
  ///   - For exterior rings: positive area contribution
  ///   - For holes: area should be subtracted (but this implementation takes absolute value)
  ///   - Works correctly for simple polygons without self-intersections
  ///   - Does not handle complex polygons with overlapping rings
  /// </remarks>
  public double Area
  {
    get
    {
      double area = 0;
      foreach (var ring in _rings)
      {
        var count = ring.Count;
        // Need at least 3 points to form a valid ring (triangle is minimum)
        if (count < 3)
          continue;

        // Apply shoelace formula: sum of cross products
        double ringArea = 0;
        for (var i = 0; i < count - 1; i++) 
          ringArea += ring[i].X * ring[i + 1].Y - ring[i + 1].X * ring[i].Y;
        
        // Close the ring by connecting last point to first
        ringArea += ring[count - 1].X * ring[0].Y - ring[0].X * ring[count - 1].Y;
        
        // Take absolute value and multiply by 0.5
        area += Math.Abs(ringArea) * 0.5;
      }

      return area;
    }
  }

  /// <summary>
  ///   Adds a new ring to the polygon.
  ///   The first ring added is typically the exterior boundary, subsequent rings are holes.
  /// </summary>
  /// <param name="points">The points that make up the ring. Should form a closed loop
  ///   (first point should equal last point, though not strictly enforced).</param>
  /// <exception cref="ArgumentNullException">Thrown when points is null.</exception>
  /// <remarks>
  ///   Best practices:
  ///   - Exterior rings should be counter-clockwise
  ///   - Hole rings should be clockwise (by OGC convention)
  ///   - Rings should be closed (first point = last point)
  ///   - Rings should not self-intersect
  /// </remarks>
  public void AddRing(IEnumerable<Point> points)
  {
    if (points == null) throw new ArgumentNullException(nameof(points));
    _rings.Add(points.ToList());
  }

  /// <summary>
  ///   Gets the ring at the specified index.
  /// </summary>
  /// <param name="index">The index of the ring.</param>
  /// <returns>The ring at the specified index.</returns>
  public IReadOnlyList<Point> GetRing(int index)
  {
    if (index < 0 || index >= _rings.Count) throw new ArgumentOutOfRangeException(nameof(index));
    return _rings[index].AsReadOnly();
  }

  /// <summary>
  ///   Gets all rings in the polygon.
  /// </summary>
  /// <returns>An enumerable collection of rings.</returns>
  public IEnumerable<IReadOnlyList<Point>> GetRings()
  {
    return _rings.Select(r => r.AsReadOnly());
  }

  /// <inheritdoc />
  public override Envelope GetEnvelope()
  {
    if (IsEmpty) return new Envelope();

    var envelope = new Envelope();
    foreach (var ring in _rings)
    foreach (var point in ring)
      envelope.Merge(point);

    return envelope;
  }
}