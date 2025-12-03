using System;
using System.Collections.Generic;
using System.Linq;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   计算几何图形凸包的操作符。
///   凸包是包含几何图形中所有点的最小凸多边形。
/// </summary>
/// <remarks>
///   使用 Graham Scan 算法计算凸包：
///   1. 找到 Y 坐标最低的点（锚点）
///   2. 按相对于锚点的极角对所有其他点进行排序
///   3. 按顺序处理点，删除凹转以保持凸性
///   
///   时间复杂度：O(n log n)（由于排序）
///   空间复杂度：O(n)（用于凸包和排序后的点）
///   
///   结果为：
///   - 如果凸包包含 3+ 个点，则为 Polygon
///   - 如果凸包恰好包含 2 个点，则为 Line
///   - 如果凸包包含 1 个点，则为 Point
///   - 如果输入为空，则为空 Polygon
/// </remarks>
public class ConvexHullOperator : IGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<ConvexHullOperator> _instance = new(() => new ConvexHullOperator());

  private ConvexHullOperator()
  {
  }

  /// <summary>
  ///   获取凸包操作符的单例实例。
  /// </summary>
  public static ConvexHullOperator Instance => _instance.Value;

  /// <summary>
  ///   使用 Graham Scan 算法计算几何图形的凸包。
  /// </summary>
  /// <param name="geometry">输入几何图形。支持所有几何类型。</param>
  /// <param name="spatialRef">可选的空间参考（当前未使用）。</param>
  /// <returns>
  ///   凸包为：
  ///   - 3+ 个点时为 Polygon
  ///   - 恰好 2 个点时为 Line
  ///   - 恰好 1 个点时为 Point
  ///   - 空输入时为空 Polygon
  /// </returns>
  /// <exception cref="ArgumentNullException">当 geometry 为 null 时抛出。</exception>
  /// <example>
  ///   <code>
  ///   var multiPoint = new MultiPoint();
  ///   multiPoint.Add(new Point(0, 0));
  ///   multiPoint.Add(new Point(10, 0));
  ///   multiPoint.Add(new Point(5, 10));
  ///   multiPoint.Add(new Point(5, 5)); // 内部点
  ///   var hull = ConvexHullOperator.Instance.Execute(multiPoint);
  ///   // 返回顶点为 (0,0)、(10,0)、(5,10) 的三角形多边形
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

    // 使用 Graham scan 算法计算凸包
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
    // 确保环是闭合的
    var ring = new List<Point>(hull);
    if (!ring[0].Equals(ring[ring.Count - 1])) ring.Add(ring[0]);
    polygon.AddRing(ring);
    return polygon;
  }

  /// <summary>
  ///   从各种几何类型中提取所有点到扁平列表。
  /// </summary>
  /// <param name="geometry">要从中提取点的几何图形。</param>
  /// <returns>几何图形中所有点的列表。</returns>
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
  ///   实现 Graham Scan 算法来计算凸包。
  ///   该算法的工作原理：
  ///   1. 找到锚点（最低 Y，然后最低 X）
  ///   2. 按相对于锚点的极角对点进行排序
  ///   3. 按顺序处理点，仅保持左转（凸性）
  /// </summary>
  /// <param name="points">要处理的点列表。</param>
  /// <returns>构成凸包的有序点列表。</returns>
  private List<Point> GrahamScan(List<Point> points)
  {
    if (points.Count < 3) return points;

    // 找到 Y 坐标最低的点（如果相等则取 X 最低的）
    var lowestPoint = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();

    // 按相对于最低点的极角对点进行排序
    var sortedPoints = points
      .Where(p => p != lowestPoint)
      .OrderBy(p => Math.Atan2(p.Y - lowestPoint.Y, p.X - lowestPoint.X))
      .ThenBy(p => p.Distance(lowestPoint))
      .ToList();

    // 构建凸包
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
  ///   确定三个点是否形成左转（逆时针）。
  ///   使用叉积测试方向。
  /// </summary>
  /// <param name="p1">第一个点。</param>
  /// <param name="p2">第二个点（转折点）。</param>
  /// <param name="p3">第三个点。</param>
  /// <returns>如果点形成左转则为 true，右转或共线则为 false。</returns>
  private bool IsLeftTurn(Point p1, Point p2, Point p3)
  {
    // 叉积：(p2-p1) × (p3-p1)
    // 正叉积表示左转（逆时针）
    var cross = (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
    return cross > 0;
  }
}