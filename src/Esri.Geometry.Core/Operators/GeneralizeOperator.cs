using System;
using System.Collections.Generic;
using System.Linq;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Generalize operator - removes vertices while preserving general shape
///   Uses a tolerance-based approach to simplify geometries
/// </summary>
public class GeneralizeOperator : IGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<GeneralizeOperator> _instance = new(() => new GeneralizeOperator());

  private GeneralizeOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the GeneralizeOperator
  /// </summary>
  public static GeneralizeOperator Instance => _instance.Value;

  Geometries.Geometry IGeometryOperator<Geometries.Geometry>.Execute(Geometries.Geometry geometry,
    SpatialReference.SpatialReference? spatialReference)
  {
    throw new NotSupportedException("Use Execute(geometry, maxDeviation, spatialReference) instead");
  }

  /// <summary>
  ///   Generalizes a geometry by removing vertices within the specified tolerance
  /// </summary>
  /// <param name="geometry">The geometry to generalize</param>
  /// <param name="maxDeviation">Maximum deviation tolerance</param>
  /// <param name="spatialReference">Optional spatial reference</param>
  /// <returns>Generalized geometry</returns>
  public Geometries.Geometry Execute(Geometries.Geometry geometry, double maxDeviation,
    SpatialReference.SpatialReference? spatialReference = null)
  {
    if (geometry == null)
      throw new ArgumentNullException(nameof(geometry));

    if (maxDeviation <= 0)
      throw new ArgumentException("Max deviation must be positive", nameof(maxDeviation));

    if (geometry.IsEmpty)
      return geometry;

    switch (geometry.Type)
    {
      case GeometryType.Point:
        return geometry; // Points cannot be generalized

      case GeometryType.MultiPoint:
        return GeneralizeMultiPoint((MultiPoint)geometry, maxDeviation);

      case GeometryType.Line:
        return GeneralizeLine((Line)geometry, maxDeviation);

      case GeometryType.Polyline:
        return GeneralizePolyline((Polyline)geometry, maxDeviation);

      case GeometryType.Polygon:
        return GeneralizePolygon((Polygon)geometry, maxDeviation);

      case GeometryType.Envelope:
        return geometry; // Envelopes cannot be generalized

      default:
        throw new NotSupportedException($"Geometry type {geometry.Type} is not supported");
    }
  }

  private Geometries.Geometry GeneralizeMultiPoint(MultiPoint multiPoint, double maxDeviation)
  {
    // For multipoint, remove points that are very close together
    var points = new List<Point>();
    var tolerance = maxDeviation;

    foreach (var point in multiPoint.GetPoints())
    {
      var shouldAdd = true;
      foreach (var existingPoint in points)
      {
        var dist = Math.Sqrt(
          Math.Pow(point.X - existingPoint.X, 2) +
          Math.Pow(point.Y - existingPoint.Y, 2));

        if (dist < tolerance)
        {
          shouldAdd = false;
          break;
        }
      }

      if (shouldAdd) points.Add(point);
    }

    return new MultiPoint(points);
  }

  private Geometries.Geometry GeneralizeLine(Line line, double maxDeviation)
  {
    // Lines with only 2 points cannot be generalized further
    return line;
  }

  private Geometries.Geometry GeneralizePolyline(Polyline polyline, double maxDeviation)
  {
    var result = new Polyline();

    foreach (var path in polyline.GetPaths())
    {
      var generalizedPath = GeneralizePath(path, maxDeviation);
      if (generalizedPath.Count >= 2) // Must have at least 2 points
        result.AddPath(generalizedPath);
    }

    return result;
  }

  private Geometries.Geometry GeneralizePolygon(Polygon polygon, double maxDeviation)
  {
    var result = new Polygon();

    foreach (var ring in polygon.GetRings())
    {
      var generalizedRing = GeneralizePath(ring, maxDeviation);
      if (generalizedRing.Count >= 3) // Polygon rings must have at least 3 points
      {
        // Ensure ring is closed
        if (generalizedRing[0].X != generalizedRing[generalizedRing.Count - 1].X ||
            generalizedRing[0].Y != generalizedRing[generalizedRing.Count - 1].Y)
          generalizedRing.Add(new Point(generalizedRing[0].X, generalizedRing[0].Y));
        result.AddRing(generalizedRing);
      }
    }

    return result;
  }

  private List<Point> GeneralizePath(IReadOnlyList<Point> path, double maxDeviation)
  {
    if (path.Count <= 2)
      return path.ToList();

    var result = new List<Point> { path[0] }; // Always keep first point

    // Use perpendicular distance approach
    var i = 0;
    while (i < path.Count - 1)
    {
      var furthest = i + 1;
      double maxDist = 0;

      // Find the furthest point ahead that we can skip to
      for (var j = i + 2; j < path.Count; j++)
      {
        // Check distance from intermediate points to line segment
        var canSkip = true;
        for (var k = i + 1; k < j; k++)
        {
          var dist = PerpendicularDistance(path[k], path[i], path[j]);
          if (dist > maxDeviation)
          {
            canSkip = false;
            break;
          }

          maxDist = Math.Max(maxDist, dist);
        }

        if (canSkip)
          furthest = j;
        else
          break;
      }

      result.Add(path[furthest]);
      i = furthest;
    }

    return result;
  }

  private double PerpendicularDistance(Point point, Point lineStart, Point lineEnd)
  {
    var dx = lineEnd.X - lineStart.X;
    var dy = lineEnd.Y - lineStart.Y;

    if (dx == 0 && dy == 0)
      return Math.Sqrt(
        Math.Pow(point.X - lineStart.X, 2) +
        Math.Pow(point.Y - lineStart.Y, 2));

    var t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
    t = Math.Max(0, Math.Min(1, t));

    var projX = lineStart.X + t * dx;
    var projY = lineStart.Y + t * dy;

    return Math.Sqrt(
      Math.Pow(point.X - projX, 2) +
      Math.Pow(point.Y - projY, 2));
  }
}