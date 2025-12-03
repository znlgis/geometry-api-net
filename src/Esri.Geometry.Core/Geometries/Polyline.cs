using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   表示由一条或多条路径组成的折线几何对象。
/// </summary>
public class Polyline : Geometry
{
  private readonly List<List<Point>> _paths;

  /// <summary>
  ///   初始化 <see cref="Polyline" /> 类的新实例。
  /// </summary>
  public Polyline()
  {
    _paths = new List<List<Point>>();
  }

  /// <inheritdoc />
  public override GeometryType Type => GeometryType.Polyline;

  /// <inheritdoc />
  public override bool IsEmpty
  {
    get
    {
      if (_paths.Count == 0)
        return true;

      foreach (var path in _paths)
        if (path.Count > 0)
          return false;
      return true;
    }
  }

  /// <inheritdoc />
  public override int Dimension => 1;

  /// <summary>
  ///   获取折线中的路径数量。
  /// </summary>
  public int PathCount => _paths.Count;

  /// <summary>
  ///   计算折线中所有路径的总长度。
  /// </summary>
  public double Length
  {
    get
    {
      double length = 0;
      foreach (var path in _paths)
        for (var i = 0; i < path.Count - 1; i++)
          length += path[i].Distance(path[i + 1]);

      return length;
    }
  }

  /// <summary>
  ///   向折线添加新的路径。
  /// </summary>
  /// <param name="points">构成路径的点集合。</param>
  public void AddPath(IEnumerable<Point> points)
  {
    if (points == null) throw new ArgumentNullException(nameof(points));
    _paths.Add(points.ToList());
  }

  /// <summary>
  ///   获取指定索引处的路径。
  /// </summary>
  /// <param name="index">路径的索引。</param>
  /// <returns>指定索引处的路径。</returns>
  public IReadOnlyList<Point> GetPath(int index)
  {
    if (index < 0 || index >= _paths.Count) throw new ArgumentOutOfRangeException(nameof(index));
    return _paths[index].AsReadOnly();
  }

  /// <summary>
  ///   获取折线中的所有路径。
  /// </summary>
  /// <returns>路径的可枚举集合。</returns>
  public IEnumerable<IReadOnlyList<Point>> GetPaths()
  {
    return _paths.Select(p => p.AsReadOnly());
  }

  /// <inheritdoc />
  public override Envelope GetEnvelope()
  {
    if (IsEmpty) return new Envelope();

    var envelope = new Envelope();
    foreach (var path in _paths)
    foreach (var point in path)
      envelope.Merge(point);

    return envelope;
  }
}