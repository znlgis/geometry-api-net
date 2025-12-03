using System;
using System.Collections.Generic;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Creates offset curves/polygons at a specified distance.
///   Simplified implementation for basic offset operations.
/// </summary>
public class OffsetOperator : IGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<OffsetOperator> _instance = new(() => new OffsetOperator());

  private OffsetOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the OffsetOperator.
  /// </summary>
  public static OffsetOperator Instance => _instance.Value;

  /// <inheritdoc />
  public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    // Default distance of 1.0 if not specified in overload
    return Execute(geometry, 1.0);
  }

  /// <summary>
  ///   Creates an offset geometry at the specified distance.
  /// </summary>
  /// <param name="geometry">The input geometry</param>
  /// <param name="distance">The offset distance (positive for outward, negative for inward)</param>
  /// <returns>The offset geometry</returns>
  public Geometries.Geometry Execute(Geometries.Geometry geometry, double distance)
  {
    if (geometry == null || geometry.IsEmpty)
      return geometry;

    switch (geometry)
    {
      case Point point:
        // Points can't be offset meaningfully, return buffer instead
        return BufferOperator.Instance.Execute(point, Math.Abs(distance));

      case Envelope envelope:
        return OffsetEnvelope(envelope, distance);

      case Polygon polygon:
        return OffsetPolygon(polygon, distance);

      case Polyline polyline:
        return OffsetPolyline(polyline, distance);

      default:
        // Return unchanged for unsupported types
        return geometry;
    }
  }

  private Geometries.Geometry OffsetEnvelope(Envelope envelope, double distance)
  {
    // Expand or shrink envelope by distance
    return new Envelope(
      envelope.XMin - distance,
      envelope.YMin - distance,
      envelope.XMax + distance,
      envelope.YMax + distance
    );
  }

  private Geometries.Geometry OffsetPolygon(Polygon polygon, double distance)
  {
    // Simplified implementation: offset each ring independently
    var offsetPolygon = new Polygon();

    foreach (var ring in polygon.GetRings())
    {
      var offsetRing = OffsetRing(ring, distance);
      if (offsetRing.Count >= 3) offsetPolygon.AddRing(offsetRing);
    }

    return offsetPolygon;
  }

  private Geometries.Geometry OffsetPolyline(Polyline polyline, double distance)
  {
    // Offset each path independently
    var offsetPolyline = new Polyline();

    foreach (var path in polyline.GetPaths())
    {
      var offsetPath = OffsetPath(path, distance);
      if (offsetPath.Count >= 2) offsetPolyline.AddPath(offsetPath);
    }

    return offsetPolyline;
  }

  private List<Point> OffsetRing(IReadOnlyList<Point> ring, double distance)
  {
    // Simplified offset using perpendicular displacement
    var offsetPoints = new List<Point>();
    var n = ring.Count;

    if (n < 3)
      return offsetPoints;

    for (var i = 0; i < n - 1; i++)
    {
      var current = ring[i];
      var next = ring[(i + 1) % (n - 1)];

      // Calculate perpendicular offset direction
      var dx = next.X - current.X;
      var dy = next.Y - current.Y;
      var length = Math.Sqrt(dx * dx + dy * dy);

      if (length > 0)
      {
        // Perpendicular vector (rotated 90 degrees counterclockwise)
        var perpX = -dy / length;
        var perpY = dx / length;

        // Offset point
        offsetPoints.Add(new Point(
          current.X + perpX * distance,
          current.Y + perpY * distance,
          current.Z ?? double.NaN
        ));
      }
    }

    // Close the ring
    if (offsetPoints.Count > 0)
      offsetPoints.Add(new Point(offsetPoints[0].X, offsetPoints[0].Y, offsetPoints[0].Z ?? double.NaN));

    return offsetPoints;
  }

  private List<Point> OffsetPath(IReadOnlyList<Point> path, double distance)
  {
    // Similar to ring offset but without closing
    var offsetPoints = new List<Point>();
    var n = path.Count;

    if (n < 2)
      return offsetPoints;

    for (var i = 0; i < n - 1; i++)
    {
      var current = path[i];
      var next = path[i + 1];

      // Calculate perpendicular offset direction
      var dx = next.X - current.X;
      var dy = next.Y - current.Y;
      var length = Math.Sqrt(dx * dx + dy * dy);

      if (length > 0)
      {
        // Perpendicular vector
        var perpX = -dy / length;
        var perpY = dx / length;

        // Offset point
        offsetPoints.Add(new Point(
          current.X + perpX * distance,
          current.Y + perpY * distance,
          current.Z ?? double.NaN
        ));
      }
    }

    // Add the last point
    if (n > 1)
    {
      var secondLast = path[n - 2];
      var last = path[n - 1];
      var dx = last.X - secondLast.X;
      var dy = last.Y - secondLast.Y;
      var length = Math.Sqrt(dx * dx + dy * dy);

      if (length > 0)
      {
        var perpX = -dy / length;
        var perpY = dx / length;

        offsetPoints.Add(new Point(
          last.X + perpX * distance,
          last.Y + perpY * distance,
          last.Z ?? double.NaN
        ));
      }
    }

    return offsetPoints;
  }
}