using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for calculating the area of a geometry.
/// </summary>
public class AreaOperator : IGeometryOperator<double>
{
  private static readonly Lazy<AreaOperator> _instance = new(() => new AreaOperator());

  private AreaOperator()
  {
  }

  /// <summary>
  ///   获取 AreaOperator 的单例实例.
  /// </summary>
  public static AreaOperator Instance => _instance.Value;

  /// <inheritdoc />
  public double Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null) throw new ArgumentNullException(nameof(geometry));

    if (geometry.IsEmpty) return 0;

    // Only 2D geometries have area
    if (geometry.Dimension < 2) return 0;

    if (geometry is Polygon polygon) return polygon.Area;

    if (geometry is Envelope envelope) return envelope.Area;

    return 0;
  }
}

/// <summary>
///   Operator for calculating the length of a geometry.
/// </summary>
public class LengthOperator : IGeometryOperator<double>
{
  private static readonly Lazy<LengthOperator> _instance = new(() => new LengthOperator());

  private LengthOperator()
  {
  }

  /// <summary>
  ///   获取 LengthOperator 的单例实例.
  /// </summary>
  public static LengthOperator Instance => _instance.Value;

  /// <inheritdoc />
  public double Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null) throw new ArgumentNullException(nameof(geometry));

    if (geometry.IsEmpty) return 0;

    if (geometry is Line line) return line.Length;

    if (geometry is Polyline polyline) return polyline.Length;

    // Envelope perimeter
    if (geometry is Envelope envelope) return 2 * (envelope.Width + envelope.Height);

    // Polygon perimeter
    if (geometry is Polygon polygon)
    {
      double length = 0;
      foreach (var ring in polygon.GetRings())
        for (var i = 0; i < ring.Count - 1; i++)
          length += ring[i].Distance(ring[i + 1]);

      return length;
    }

    return 0;
  }
}