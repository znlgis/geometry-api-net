using System;
using System.Collections.Generic;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   用于在几何对象周围创建缓冲区（偏移多边形）的操作符。
///   注意：这是一个简化实现。完整的缓冲区需要复杂的算法。
/// </summary>
public class BufferOperator : IGeometryOperator<Polygon>
{
  private static readonly Lazy<BufferOperator> _instance = new(() => new BufferOperator());

  private BufferOperator()
  {
  }

  /// <summary>
  ///   获取 BufferOperator 的单例实例。
  /// </summary>
  public static BufferOperator Instance => _instance.Value;

  /// <inheritdoc />
  public Polygon Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    throw new NotImplementedException(
      "Buffer operator requires a distance parameter. Use Execute(geometry, distance, spatialRef) instead.");
  }

  /// <summary>
  ///   围绕几何对象创建缓冲区多边形。
  /// </summary>
  /// <param name="geometry">要缓冲的几何对象。</param>
  /// <param name="distance">缓冲距离。</param>
  /// <param name="spatialRef">可选的空间参考。</param>
  /// <returns>表示缓冲区域的多边形。</returns>
  public Polygon Execute(Geometries.Geometry geometry, double distance,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null)
      throw new ArgumentNullException(nameof(geometry));

    if (geometry.IsEmpty)
      return new Polygon();

    if (distance <= 0)
      throw new ArgumentException("Buffer distance must be positive.", nameof(distance));

    return geometry switch
    {
      Point point => BufferPoint(point, distance),
      Envelope envelope => BufferEnvelope(envelope, distance),
      _ => throw new NotImplementedException($"Buffer operation for {geometry.Type} is not yet implemented.")
    };
  }

  private Polygon BufferPoint(Point point, double distance)
  {
    // 在点周围创建简单的正方形缓冲区
    // 在完整实现中，这将创建圆形或多边形
    var polygon = new Polygon();
    var ring = new List<Point>
    {
      new(point.X - distance, point.Y - distance),
      new(point.X + distance, point.Y - distance),
      new(point.X + distance, point.Y + distance),
      new(point.X - distance, point.Y + distance),
      new(point.X - distance, point.Y - distance)
    };
    polygon.AddRing(ring);
    return polygon;
  }

  private Polygon BufferEnvelope(Envelope envelope, double distance)
  {
    // 在各个方向上按距离扩展包络
    var polygon = new Polygon();
    var ring = new List<Point>
    {
      new(envelope.XMin - distance, envelope.YMin - distance),
      new(envelope.XMax + distance, envelope.YMin - distance),
      new(envelope.XMax + distance, envelope.YMax + distance),
      new(envelope.XMin - distance, envelope.YMax + distance),
      new(envelope.XMin - distance, envelope.YMin - distance)
    };
    polygon.AddRing(ring);
    return polygon;
  }
}