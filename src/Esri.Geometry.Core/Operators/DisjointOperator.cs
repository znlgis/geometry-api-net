using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   用于测试两个几何对象在空间上是否分离（不相交）的操作符。
/// </summary>
public class DisjointOperator : IBinaryGeometryOperator<bool>
{
  private static readonly Lazy<DisjointOperator> _instance = new(() => new DisjointOperator());

  private DisjointOperator()
  {
  }

  /// <summary>
  ///   获取 DisjointOperator 的单例实例。
  /// </summary>
  public static DisjointOperator Instance => _instance.Value;

  /// <inheritdoc />
  public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // 空几何对象被认为是分离的
    if (geometry1.IsEmpty || geometry2.IsEmpty) return true;

    // 分离是相交的反义
    // 对于包络，我们可以使用包络相交测试
    if (geometry1 is Envelope env1 && geometry2 is Envelope env2) return !env1.Intersects(env2);

    // 点-包络测试
    if (geometry1 is Point p && geometry2 is Envelope env) return !env.Contains(p);

    if (geometry1 is Envelope env3 && geometry2 is Point p2) return !env3.Contains(p2);

    // 对于其他几何类型，使用相交操作符
    try
    {
      return !IntersectsOperator.Instance.Execute(geometry1, geometry2, spatialRef);
    }
    catch (NotImplementedException)
    {
      throw new NotImplementedException(
        $"Disjoint test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
    }
  }
}