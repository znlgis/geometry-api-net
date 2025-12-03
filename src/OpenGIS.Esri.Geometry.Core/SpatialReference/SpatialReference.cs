namespace OpenGIS.Esri.Geometry.Core.SpatialReference;

/// <summary>
///     表示几何对象的空间参考系统。
/// </summary>
public class SpatialReference
{
  /// <summary>
  ///     初始化 <see cref="SpatialReference" /> 类的新实例。
  /// </summary>
  public SpatialReference()
    {
    }

  /// <summary>
  ///     使用 WKID 初始化 <see cref="SpatialReference" /> 类的新实例。
  /// </summary>
  /// <param name="wkid">众所周知的 ID。</param>
  public SpatialReference(int wkid)
    {
        Wkid = wkid;
    }

  /// <summary>
  ///     获取或设置空间参考的众所周知的 ID (WKID)。
  /// </summary>
  public int? Wkid { get; set; }

  /// <summary>
  ///     获取或设置空间参考的最新众所周知的 ID。
  /// </summary>
  public int? LatestWkid { get; set; }

  /// <summary>
  ///     获取或设置空间参考的 Well-Known Text (WKT) 表示。
  /// </summary>
  public string? Wkt { get; set; }

  /// <summary>
  ///     创建 WGS 84 (EPSG:4326) 的空间参考。
  /// </summary>
  /// <returns>WGS 84 空间参考。</returns>
  public static SpatialReference Wgs84()
    {
        return new SpatialReference(4326);
    }

  /// <summary>
  ///     创建 Web Mercator (EPSG:3857) 的空间参考。
  /// </summary>
  /// <returns>Web Mercator 空间参考。</returns>
  public static SpatialReference WebMercator()
    {
        return new SpatialReference(3857);
    }
}