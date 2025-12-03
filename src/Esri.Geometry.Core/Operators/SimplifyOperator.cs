using System;
using System.Collections.Generic;
using System.Linq;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   使用 Douglas-Peucker 算法简化几何图形的操作符。
///   该算法在保持总体形状的同时减少折线或多边形的顶点数量。
/// </summary>
/// <remarks>
///   Douglas-Peucker 算法是一种递归的线简化算法：
///   1. 找到距离连接端点的线段最远的点
///   2. 如果该距离大于容差，则在该点处分割线段并递归处理
///   3. 否则，删除所有中间点
///   
///   时间复杂度：平均情况 O(n log n)，最坏情况 O(n²)
///   空间复杂度：O(n)（递归栈）
/// </remarks>
public class SimplifyOperator : IGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<SimplifyOperator> _instance = new(() => new SimplifyOperator());

  private SimplifyOperator()
  {
  }

  /// <summary>
  ///   获取简化操作符的单例实例。
  /// </summary>
  public static SimplifyOperator Instance => _instance.Value;

  /// <inheritdoc />
  public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    throw new NotImplementedException(
      "Simplify operator requires a tolerance parameter. Use Execute(geometry, tolerance, spatialRef) instead.");
  }

  /// <summary>
  ///   使用 Douglas-Peucker 算法简化几何图形。
  ///   在指定容差范围内减少顶点数量，同时保持总体形状。
  /// </summary>
  /// <param name="geometry">要简化的几何图形。支持 Point、Polyline 和 Polygon 类型。</param>
  /// <param name="tolerance">顶点可以偏离简化线的最大垂直距离。
  ///   较大的值会导致更激进的简化。</param>
  /// <param name="spatialRef">可选的空间参考（当前在简化中未使用）。</param>
  /// <returns>顶点较少的简化几何图形。点保持不变。
  ///   空几何图形按原样返回。</returns>
  /// <exception cref="ArgumentNullException">当 geometry 为 null 时抛出。</exception>
  /// <exception cref="ArgumentException">当 tolerance 不是正数时抛出。</exception>
  /// <example>
  ///   <code>
  ///   var polyline = new Polyline();
  ///   polyline.AddPath(new[] { new Point(0,0), new Point(1,0.1), new Point(2,0) });
  ///   var simplified = SimplifyOperator.Instance.Execute(polyline, 0.2);
  ///   // 结果将只有 2 个点：(0,0) 和 (2,0)
  ///   </code>
  /// </example>
  public Geometries.Geometry Execute(Geometries.Geometry geometry, double tolerance,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null) throw new ArgumentNullException(nameof(geometry));

    if (geometry.IsEmpty) return geometry;

    if (tolerance <= 0) throw new ArgumentException("Tolerance must be positive.", nameof(tolerance));

    // 点不需要简化
    if (geometry is Point) return geometry;

    // 简化折线
    if (geometry is Polyline polyline) return SimplifyPolyline(polyline, tolerance);

    // 简化多边形
    if (geometry is Polygon polygon) return SimplifyPolygon(polygon, tolerance);

    // 对于其他类型，按原样返回
    return geometry;
  }

  /// <summary>
  ///   通过对每条路径应用 Douglas-Peucker 算法来简化折线。
  /// </summary>
  /// <param name="polyline">要简化的折线。</param>
  /// <param name="tolerance">简化容差。</param>
  /// <returns>每条路径顶点较少的简化折线。</returns>
  private Polyline SimplifyPolyline(Polyline polyline, double tolerance)
  {
    var simplified = new Polyline();
    foreach (var path in polyline.GetPaths())
    {
      var simplifiedPath = DouglasPeucker(path.ToList(), tolerance);
      // 只添加至少有 2 个点的路径（有效路径的最小值）
      if (simplifiedPath.Count >= 2) simplified.AddPath(simplifiedPath);
    }

    return simplified;
  }

  /// <summary>
  ///   通过对每个环应用 Douglas-Peucker 算法来简化多边形。
  /// </summary>
  /// <param name="polygon">要简化的多边形。</param>
  /// <param name="tolerance">简化容差。</param>
  /// <returns>每个环顶点较少的简化多边形。</returns>
  private Polygon SimplifyPolygon(Polygon polygon, double tolerance)
  {
    var simplified = new Polygon();
    foreach (var ring in polygon.GetRings())
    {
      var simplifiedRing = DouglasPeucker(ring.ToList(), tolerance);
      // 确保环至少有 3 个点加上闭合点（有效多边形环的最小值）
      if (simplifiedRing.Count >= 3)
      {
        // 确保环是闭合的（首尾点必须相同）
        if (!simplifiedRing[0].Equals(simplifiedRing[simplifiedRing.Count - 1])) simplifiedRing.Add(simplifiedRing[0]);
        simplified.AddRing(simplifiedRing);
      }
    }

    return simplified;
  }

  /// <summary>
  ///   递归实现 Douglas-Peucker 线简化算法。
  ///   该算法找到距离连接首尾点的线段垂直距离最大的点。如果该距离超过容差，
  ///   则在该点处分割线段，并递归处理两个线段。
  /// </summary>
  /// <param name="points">要简化的点列表。</param>
  /// <param name="tolerance">允许的最大垂直距离。</param>
  /// <returns>近似原始线的简化点列表。</returns>
  private List<Point> DouglasPeucker(List<Point> points, double tolerance)
  {
    if (points.Count < 3) return points;

    // 找到距离线段最远的点
    double maxDistance = 0;
    var maxIndex = 0;
    var end = points.Count - 1;

    for (var i = 1; i < end; i++)
    {
      var distance = PerpendicularDistance(points[i], points[0], points[end]);
      if (distance > maxDistance)
      {
        maxDistance = distance;
        maxIndex = i;
      }
    }

    // 如果最大距离大于容差，递归简化
    if (maxDistance > tolerance)
    {
      // 递归调用
      var leftSegment = DouglasPeucker(points.GetRange(0, maxIndex + 1), tolerance);
      var rightSegment = DouglasPeucker(points.GetRange(maxIndex, end - maxIndex + 1), tolerance);

      // 合并结果（删除 maxIndex 处的重复点）
      var result = new List<Point>(leftSegment);
      result.AddRange(rightSegment.Skip(1));
      return result;
    }

    // 只返回端点
    return new List<Point> { points[0], points[end] };
  }

  /// <summary>
  ///   计算点到线段的垂直距离。
  ///   使用向量投影找到线段上最近的点。
  /// </summary>
  /// <param name="point">要测量距离的点。</param>
  /// <param name="lineStart">线段的起点。</param>
  /// <param name="lineEnd">线段的终点。</param>
  /// <returns>点到线段的垂直距离。</returns>
  private double PerpendicularDistance(Point point, Point lineStart, Point lineEnd)
  {
    var dx = lineEnd.X - lineStart.X;
    var dy = lineEnd.Y - lineStart.Y;

    // 计算线段的长度
    var mag = Math.Sqrt(dx * dx + dy * dy);
    // 如果线段实际上是一个点，返回到该点的距离
    if (mag < GeometryConstants.Epsilon) return point.Distance(lineStart);

    // 计算投影参数 u（0 到 1 表示线段上的点）
    var u = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (mag * mag);

    // 如果投影在线段起点之前
    if (u < 0) return point.Distance(lineStart);

    // 如果投影在线段终点之后
    if (u > 1) return point.Distance(lineEnd);

    // 计算线段上的交点
    var ix = lineStart.X + u * dx;
    var iy = lineStart.Y + u * dy;
    return point.Distance(new Point(ix, iy));
  }
}