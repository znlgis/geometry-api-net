using System;
using System.Collections.Generic;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   密化操作符 - 向几何对象添加顶点
///   沿线段插入额外的顶点以确保没有线段超过指定长度
/// </summary>
public class DensifyOperator : IGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<DensifyOperator> _instance = new(() => new DensifyOperator());

  private DensifyOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the DensifyOperator
  /// </summary>
  public static DensifyOperator Instance => _instance.Value;

  Geometries.Geometry IGeometryOperator<Geometries.Geometry>.Execute(Geometries.Geometry geometry,
    SpatialReference.SpatialReference? spatialReference)
  {
    throw new NotSupportedException("Use Execute(geometry, maxSegmentLength, spatialReference) instead");
  }

  /// <summary>
  ///   Densifies a geometry by adding vertices so no segment exceeds maxSegmentLength
  /// </summary>
  /// <param name="geometry">The geometry to densify</param>
  /// <param name="maxSegmentLength">Maximum allowed segment length</param>
  /// <param name="spatialReference">Optional spatial reference</param>
  /// <returns>Densified geometry</returns>
  public Geometries.Geometry Execute(Geometries.Geometry geometry, double maxSegmentLength,
    SpatialReference.SpatialReference? spatialReference = null)
  {
    if (geometry == null)
      throw new ArgumentNullException(nameof(geometry));

    if (maxSegmentLength <= 0)
      throw new ArgumentException("Max segment length must be positive", nameof(maxSegmentLength));

    if (geometry.IsEmpty)
      return geometry;

    switch (geometry.Type)
    {
      case GeometryType.Point:
      case GeometryType.MultiPoint:
      case GeometryType.Envelope:
        return geometry; // These types cannot be densified

      case GeometryType.Line:
        return DensifyLine((Line)geometry, maxSegmentLength);

      case GeometryType.Polyline:
        return DensifyPolyline((Polyline)geometry, maxSegmentLength);

      case GeometryType.Polygon:
        return DensifyPolygon((Polygon)geometry, maxSegmentLength);

      default:
        throw new NotSupportedException($"Geometry type {geometry.Type} is not supported");
    }
  }

  private Geometries.Geometry DensifyLine(Line line, double maxSegmentLength)
  {
    var densifiedPoints = DensifySegment(line.Start, line.End, maxSegmentLength);

    // Return a Polyline since we may have more than 2 points
    var result = new Polyline();
    result.AddPath(densifiedPoints);
    return result;
  }

  private Geometries.Geometry DensifyPolyline(Polyline polyline, double maxSegmentLength)
  {
    var result = new Polyline();

    foreach (var path in polyline.GetPaths())
    {
      var densifiedPath = DensifyPath(path, maxSegmentLength);
      result.AddPath(densifiedPath);
    }

    return result;
  }

  private Geometries.Geometry DensifyPolygon(Polygon polygon, double maxSegmentLength)
  {
    var result = new Polygon();

    foreach (var ring in polygon.GetRings())
    {
      var densifiedRing = DensifyPath(ring, maxSegmentLength, true);
      result.AddRing(densifiedRing);
    }

    return result;
  }

  private List<Point> DensifyPath(IReadOnlyList<Point> path, double maxSegmentLength, bool closeRing = false)
  {
    var result = new List<Point>();

    for (var i = 0; i < path.Count - 1; i++)
    {
      var segmentPoints = DensifySegment(path[i], path[i + 1], maxSegmentLength);

      // Add all points except the last one (to avoid duplicates)
      for (var j = 0; j < segmentPoints.Count - 1; j++) result.Add(segmentPoints[j]);
    }

    // Add the last point
    result.Add(path[path.Count - 1]);

    // For closed rings, ensure it's actually closed
    if (closeRing && path.Count > 0)
    {
      var first = path[0];
      var last = result[result.Count - 1];
      if (first.X != last.X || first.Y != last.Y) result.Add(new Point(first.X, first.Y));
    }

    return result;
  }

  private List<Point> DensifySegment(Point start, Point end, double maxSegmentLength)
  {
    var result = new List<Point> { start };

    var dx = end.X - start.X;
    var dy = end.Y - start.Y;
    var segmentLength = Math.Sqrt(dx * dx + dy * dy);

    if (segmentLength <= maxSegmentLength)
    {
      result.Add(end);
      return result;
    }

    // Calculate number of segments needed
    var numSegments = (int)Math.Ceiling(segmentLength / maxSegmentLength);

    // Add intermediate points
    for (var i = 1; i < numSegments; i++)
    {
      var t = (double)i / numSegments;
      var x = start.X + t * dx;
      var y = start.Y + t * dy;

      // Handle Z coordinate if present
      double? z = null;
      if (start.Z.HasValue && end.Z.HasValue)
      {
        z = start.Z.Value + t * (end.Z.Value - start.Z.Value);
        result.Add(new Point(x, y, z.Value));
      }
      else
      {
        result.Add(new Point(x, y));
      }
    }

    result.Add(end);
    return result;
  }
}