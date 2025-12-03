using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   用于测试两个几何对象在空间上是否相等的操作符。
/// </summary>
public class EqualsOperator : IBinaryGeometryOperator<bool>
{
  private static readonly Lazy<EqualsOperator> _instance = new(() => new EqualsOperator());

  private EqualsOperator()
  {
  }

  /// <summary>
  ///   获取 EqualsOperator 的单例实例。
  /// </summary>
  public static EqualsOperator Instance => _instance.Value;

  /// <inheritdoc />
  public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // 检查类型是否不同
    if (geometry1.Type != geometry2.Type) return false;

    // 检查是否都为空
    if (geometry1.IsEmpty && geometry2.IsEmpty) return true;

    // 如果一个为空而另一个不为空
    if (geometry1.IsEmpty != geometry2.IsEmpty) return false;

    // 点相等性
    if (geometry1 is Point p1 && geometry2 is Point p2) return p1.Equals(p2);

    // 包络相等性
    if (geometry1 is Envelope env1 && geometry2 is Envelope env2)
      return Math.Abs(env1.XMin - env2.XMin) < GeometryConstants.DefaultTolerance &&
             Math.Abs(env1.YMin - env2.YMin) < GeometryConstants.DefaultTolerance &&
             Math.Abs(env1.XMax - env2.XMax) < GeometryConstants.DefaultTolerance &&
             Math.Abs(env1.YMax - env2.YMax) < GeometryConstants.DefaultTolerance;

    // 对于其他几何类型，需要更复杂的实现
    throw new NotImplementedException(
      $"Equals test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
  }
}