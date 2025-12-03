using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   用于测试 geometry1 是否完全在 geometry2 内部的操作符.
/// </summary>
public class WithinOperator : IBinaryGeometryOperator<bool>
{
  private static readonly Lazy<WithinOperator> _instance = new(() => new WithinOperator());

  private WithinOperator()
  {
  }

  /// <summary>
  ///   获取 WithinOperator 的单例实例.
  /// </summary>
  public static WithinOperator Instance => _instance.Value;

  /// <inheritdoc />
  public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // Empty geometry is not within anything
    if (geometry1.IsEmpty || geometry2.IsEmpty) return false;

    // Within is the inverse of contains
    // geometry1 within geometry2 is equivalent to geometry2 contains geometry1
    if (geometry1 is Point p && geometry2 is Envelope env) return env.Contains(p);

    if (geometry1 is Envelope env1 && geometry2 is Envelope env2)
      // env1 is within env2 if all corners of env1 are inside env2
      return env1.XMin >= env2.XMin && env1.XMax <= env2.XMax &&
             env1.YMin >= env2.YMin && env1.YMax <= env2.YMax;

    // For other geometry types, use the contains operator in reverse
    try
    {
      return ContainsOperator.Instance.Execute(geometry2, geometry1, spatialRef);
    }
    catch (NotImplementedException)
    {
      throw new NotImplementedException(
        $"Within test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
    }
  }
}