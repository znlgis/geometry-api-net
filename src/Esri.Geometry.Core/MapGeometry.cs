using System;
using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.IO;

namespace Esri.Geometry.Core;

/// <summary>
///   MapGeometry 类将几何对象与其空间参考捆绑在一起。
///   要在地图中使用几何对象，必须为该几何对象定义空间参考。
/// </summary>
public class MapGeometry : IEquatable<MapGeometry>
{
    /// <summary>
    ///   构造空的 MapGeometry 实例。
    /// </summary>
    public MapGeometry()
  {
  }

    /// <summary>
    ///   使用指定的几何对象和空间参考构造 MapGeometry 实例。
    /// </summary>
    /// <param name="geometry">用于构造新 MapGeometry 对象的几何对象。</param>
    /// <param name="spatialReference">几何对象的空间参考。</param>
    public MapGeometry(Geometries.Geometry? geometry, SpatialReference.SpatialReference? spatialReference)
  {
    Geometry = geometry;
    SpatialReference = spatialReference;
  }

    /// <summary>
    ///   获取或设置几何对象。
    /// </summary>
    public Geometries.Geometry? Geometry { get; set; }

    /// <summary>
    ///   获取或设置空间参考。
    /// </summary>
    public SpatialReference.SpatialReference? SpatialReference { get; set; }

    /// <summary>
    ///   判断此 MapGeometry 是否等于另一个 MapGeometry。
    /// </summary>
    public bool Equals(MapGeometry? other)
  {
    if (other == null)
      return false;

    if (ReferenceEquals(this, other))
      return true;

    // 比较空间参考
    var srEqual = false;
    if (SpatialReference == null && other.SpatialReference == null)
    {
      srEqual = true;
    }
    else if (SpatialReference != null && other.SpatialReference != null)
    {
      // 如果两者都有 WKID，则通过 WKID 比较
      if (SpatialReference.Wkid.HasValue && other.SpatialReference.Wkid.HasValue)
        srEqual = SpatialReference.Wkid.Value == other.SpatialReference.Wkid.Value;
      else if (SpatialReference.Wkt != null && other.SpatialReference.Wkt != null)
        srEqual = SpatialReference.Wkt == other.SpatialReference.Wkt;
      else
        srEqual = ReferenceEquals(SpatialReference, other.SpatialReference);
    }

    if (!srEqual)
      return false;

    // 比较几何对象
    if (Geometry == null && other.Geometry == null)
      return true;

    if (Geometry == null || other.Geometry == null)
      return false;

    // 对于 Point 几何对象，使用基于值的比较
    if (Geometry is Point point1 && other.Geometry is Point point2) return point1.Equals(point2);

    // 对于其他几何对象，目前使用引用比较
    return ReferenceEquals(Geometry, other.Geometry);
  }

    /// <summary>
    ///   返回此 MapGeometry 的字符串表示，用于调试目的。
    /// </summary>
    public override string ToString()
  {
    if (Geometry == null)
      return "MapGeometry [null geometry]";

    try
    {
      var json = EsriJsonExportOperator.Instance.Execute(Geometry, SpatialReference);
      if (json.Length > 200) return json.Substring(0, 197) + $"... ({json.Length} characters)";
      return json;
    }
    catch
    {
      return $"MapGeometry [Type: {Geometry.Type}]";
    }
  }

    /// <summary>
    ///   判断此 MapGeometry 是否等于另一个对象。
    /// </summary>
    public override bool Equals(object? obj)
  {
    return Equals(obj as MapGeometry);
  }

    /// <summary>
    ///   返回此 MapGeometry 的哈希码。
    /// </summary>
    public override int GetHashCode()
  {
    var hash = 0x2937912;

    if (SpatialReference != null)
      hash ^= SpatialReference.GetHashCode();

    if (Geometry != null)
      hash ^= Geometry.GetHashCode();

    return hash;
  }

    /// <summary>
    ///   MapGeometry 的相等运算符。
    /// </summary>
    public static bool operator ==(MapGeometry? left, MapGeometry? right)
  {
    if (ReferenceEquals(left, right))
      return true;

    if (left is null || right is null)
      return false;

    return left.Equals(right);
  }

    /// <summary>
    ///   MapGeometry 的不等运算符。
    /// </summary>
    public static bool operator !=(MapGeometry? left, MapGeometry? right)
  {
    return !(left == right);
  }
}