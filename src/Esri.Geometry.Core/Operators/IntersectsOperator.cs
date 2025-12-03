using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   用于测试两个几何对象是否相交的操作符。
/// </summary>
public class IntersectsOperator : IBinaryGeometryOperator<bool>
{
  private static readonly Lazy<IntersectsOperator> _instance = new(() => new IntersectsOperator());

  private IntersectsOperator()
  {
  }

  /// <summary>
  ///   获取 IntersectsOperator 的单例实例。
  /// </summary>
  public static IntersectsOperator Instance => _instance.Value;

  /// <inheritdoc />
  public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // 包络-包络相交的简单实现
    if (geometry1 is Envelope env1 && geometry2 is Envelope env2) return env1.Intersects(env2);

    // 对于其他几何类型，需要更复杂的实现
    throw new NotImplementedException(
      $"Intersects test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
  }
}