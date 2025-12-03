using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   表示点的集合。
/// </summary>
public class MultiPoint : Geometry
{
  private readonly List<Point> _points;

  /// <summary>
  ///   初始化 <see cref="MultiPoint" /> 类的新实例。
  /// </summary>
  public MultiPoint()
  {
    _points = new List<Point>();
  }

  /// <summary>
  ///   使用指定的点集合初始化 <see cref="MultiPoint" /> 类的新实例。
  /// </summary>
  /// <param name="points">点的集合。</param>
  public MultiPoint(IEnumerable<Point> points)
  {
    _points = points?.ToList() ?? new List<Point>();
  }

  /// <inheritdoc />
  public override GeometryType Type => GeometryType.MultiPoint;

  /// <inheritdoc />
  public override bool IsEmpty => _points.Count == 0;

  /// <inheritdoc />
  public override int Dimension => 0;

  /// <summary>
  ///   获取多点集合中的点数量。
  /// </summary>
  public int Count => _points.Count;

  /// <summary>
  ///   获取指定索引处的点。
  /// </summary>
  /// <param name="index">点的索引。</param>
  /// <returns>指定索引处的点。</returns>
  public Point GetPoint(int index)
  {
    if (index < 0 || index >= _points.Count) throw new ArgumentOutOfRangeException(nameof(index));
    return _points[index];
  }

  /// <summary>
  ///   向多点集合添加一个点。
  /// </summary>
  /// <param name="point">要添加的点。</param>
  public void Add(Point point)
  {
    if (point == null) throw new ArgumentNullException(nameof(point));
    _points.Add(point);
  }

  /// <summary>
  ///   获取多点集合中的所有点。
  /// </summary>
  /// <returns>点的可枚举集合。</returns>
  public IEnumerable<Point> GetPoints()
  {
    return _points;
  }

  /// <inheritdoc />
  public override Envelope GetEnvelope()
  {
    if (IsEmpty) return new Envelope();

    var envelope = new Envelope();
    foreach (var point in _points) envelope.Merge(point);
    return envelope;
  }
}