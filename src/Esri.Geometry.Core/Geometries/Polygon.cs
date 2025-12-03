using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   表示由一个或多个环组成的多边形几何对象。
/// </summary>
public class Polygon : Geometry
{
  private readonly List<List<Point>> _rings;

  /// <summary>
  ///   初始化 <see cref="Polygon" /> 类的新实例。
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
  ///   获取多边形中的环数量。
  /// </summary>
  public int RingCount => _rings.Count;

  /// <summary>
  ///   使用鞋带公式计算多边形的面积。
  ///   注意：这是适用于简单多边形的简单实现。
  /// </summary>
  public double Area
  {
    get
    {
      double area = 0;
      foreach (var ring in _rings)
      {
        var count = ring.Count;
        if (count < 3)
          continue;

        double ringArea = 0;
        for (var i = 0; i < count - 1; i++) ringArea += ring[i].X * ring[i + 1].Y - ring[i + 1].X * ring[i].Y;
        // 闭合环
        ringArea += ring[count - 1].X * ring[0].Y - ring[0].X * ring[count - 1].Y;
        area += Math.Abs(ringArea) * 0.5;
      }

      return area;
    }
  }

  /// <summary>
  ///   向多边形添加新的环。
  /// </summary>
  /// <param name="points">构成环的点集合。</param>
  public void AddRing(IEnumerable<Point> points)
  {
    if (points == null) throw new ArgumentNullException(nameof(points));
    _rings.Add(points.ToList());
  }

  /// <summary>
  ///   获取指定索引处的环。
  /// </summary>
  /// <param name="index">环的索引。</param>
  /// <returns>指定索引处的环。</returns>
  public IReadOnlyList<Point> GetRing(int index)
  {
    if (index < 0 || index >= _rings.Count) throw new ArgumentOutOfRangeException(nameof(index));
    return _rings[index].AsReadOnly();
  }

  /// <summary>
  ///   获取多边形中的所有环。
  /// </summary>
  /// <returns>环的可枚举集合。</returns>
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