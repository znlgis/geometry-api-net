using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   计算 WGS84 椭球体上大地测量（大圆）距离的操作符。
///   点应以度为单位的地理坐标（经度、纬度）表示。
///   X 坐标 = 经度 (-180 到 180)，Y 坐标 = 纬度 (-90 到 90)。
/// </summary>
/// <remarks>
///   此操作符使用 Vincenty 反算公式，这是计算椭球体上距离最准确的方法之一。
///   它考虑了地球的扁球形状。
///   
///   算法：
///   - 主要：Vincenty 公式（在 WGS84 椭球体上精确到 0.5mm 以内）
///   - 后备：Haversine 公式（如果 Vincenty 无法收敛，通常在对跖点附近）
///   
///   WGS84 参数：
///   - 半长轴（赤道半径）：6,378,137 米
///   - 半短轴（极半径）：6,356,752.314245 米
///   - 扁率：~1/298.257223563
///   
///   时间复杂度：O(1)，迭代收敛（通常 2-5 次迭代）
///   精度：对于大多数点对具有亚毫米精度
/// </remarks>
public class GeodesicDistanceOperator : IBinaryGeometryOperator<double>
{
  private const double WGS84_SEMI_MAJOR_AXIS = 6378137.0; // 米
  private const double WGS84_SEMI_MINOR_AXIS = 6356752.314245; // 米
  private const double WGS84_FLATTENING = (WGS84_SEMI_MAJOR_AXIS - WGS84_SEMI_MINOR_AXIS) / WGS84_SEMI_MAJOR_AXIS;

  private static readonly Lazy<GeodesicDistanceOperator> _instance = new(() => new GeodesicDistanceOperator());

  private GeodesicDistanceOperator()
  {
  }

  /// <summary>
  ///   获取大地测量距离操作符的单例实例。
  /// </summary>
  public static GeodesicDistanceOperator Instance => _instance.Value;

  /// <summary>
  ///   计算两个点几何图形之间的大地测量距离。
  /// </summary>
  /// <param name="geometry1">地理坐标（经度、纬度）中的第一个点几何图形。</param>
  /// <param name="geometry2">地理坐标（经度、纬度）中的第二个点几何图形。</param>
  /// <param name="spatialRef">可选的空间参考（当前未使用）。</param>
  /// <returns>大地测量距离，单位为米。</returns>
  /// <exception cref="ArgumentNullException">当任一几何图形为 null 时抛出。</exception>
  /// <exception cref="ArgumentException">当任一几何图形不是 Point 时抛出。</exception>
  /// <example>
  ///   <code>
  ///   // 计算纽约到伦敦的距离
  ///   var newYork = new Point(-74.0060, 40.7128);  // 经度，纬度
  ///   var london = new Point(-0.1278, 51.5074);
  ///   double distance = GeodesicDistanceOperator.Instance.Execute(newYork, london);
  ///   // 结果：约 5,570,000 米（5,570 公里）
  ///   </code>
  /// </example>
  public double Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry1 == null) throw new ArgumentNullException(nameof(geometry1));
    if (geometry2 == null) throw new ArgumentNullException(nameof(geometry2));

    // Only support points for now
    if (!(geometry1 is Point point1))
      throw new ArgumentException("Geodesic distance currently only supports Point geometries for geometry1.",
        nameof(geometry1));

    if (!(geometry2 is Point point2))
      throw new ArgumentException("Geodesic distance currently only supports Point geometries for geometry2.",
        nameof(geometry2));

    return CalculateGeodesicDistance(point1, point2);
  }

  /// <summary>
  ///   使用 Vincenty 反算公式计算 WGS84 椭球体上两点之间的大地测量距离。
  ///   这种迭代方法解决反大地测量问题：给定两点，找到距离和方位角。
  /// </summary>
  /// <param name="point1">第一个点，坐标以度为单位（X=经度，Y=纬度）。</param>
  /// <param name="point2">第二个点，坐标以度为单位（X=经度，Y=纬度）。</param>
  /// <returns>大地测量距离（米），精确到 0.5mm 以内。</returns>
  /// <remarks>
  ///   该算法使用归化纬度和辅助变量迭代收敛到解。如果收敛失败（罕见，
  ///   通常在对跖点附近），则回退到 Haversine 公式。
  /// </remarks>
  public static double CalculateGeodesicDistance(Point point1, Point point2)
  {
    // 将地理坐标从度转换为弧度
    var lon1 = ToRadians(point1.X);
    var lat1 = ToRadians(point1.Y);
    var lon2 = ToRadians(point2.X);
    var lat2 = ToRadians(point2.Y);

    // WGS84 椭球体参数
    var a = WGS84_SEMI_MAJOR_AXIS;  // 赤道半径
    var f = WGS84_FLATTENING;        // 扁率
    var b = (1 - f) * a;             // 极半径

    // 经度差
    var L = lon2 - lon1;
    
    // 归化纬度（辅助球体）
    var U1 = Math.Atan((1 - f) * Math.Tan(lat1));
    var U2 = Math.Atan((1 - f) * Math.Tan(lat2));
    var sinU1 = Math.Sin(U1);
    var cosU1 = Math.Cos(U1);
    var sinU2 = Math.Sin(U2);
    var cosU2 = Math.Cos(U2);

    // 初始化 lambda（辅助球体上的经度差）
    var lambda = L;
    double lambdaP;
    var iterLimit = 100;  // 收敛的最大迭代次数
    double cosSqAlpha = 0;
    double sinSigma = 0;
    double cos2SigmaM = 0;
    double cosSigma = 0;
    double sigma = 0;

    // 使用 Vincenty 公式进行迭代计算
    do
    {
      var sinLambda = Math.Sin(lambda);
      var cosLambda = Math.Cos(lambda);
      
      // 计算角距离的正弦
      sinSigma = Math.Sqrt(cosU2 * sinLambda * (cosU2 * sinLambda) +
                           (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) * (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));

      if (sinSigma == 0) return 0; // 重合点（相同位置）

      // 计算角距离的余弦
      cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
      sigma = Math.Atan2(sinSigma, cosSigma);
      
      // 计算从北方的方位角
      var sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
      cosSqAlpha = 1 - sinAlpha * sinAlpha;

      // 处理赤道线情况
      if (cosSqAlpha != 0)
        cos2SigmaM = cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;
      else
        cos2SigmaM = 0; // 赤道线：纬度 = 0

      // 计算收敛参数 C
      var C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));
      lambdaP = lambda;
      
      // 更新 lambda 进行下次迭代
      lambda = L + (1 - C) * f * sinAlpha *
        (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
    } while (Math.Abs(lambda - lambdaP) > 1e-12 && --iterLimit > 0);

    if (iterLimit == 0)
      // 公式未能收敛（罕见，通常在对跖点附近）
      // 回退到更简单的 Haversine 公式
      return HaversineDistance(point1, point2);

    // 使用收敛值计算最终距离
    var uSq = cosSqAlpha * (a * a - b * b) / (b * b);
    var A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
    var B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
    var deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
                                                           B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) *
                                                           (-3 + 4 * cos2SigmaM * cos2SigmaM)));

    // 距离（米）
    var s = b * A * (sigma - deltaSigma);

    return s;
  }

  /// <summary>
  ///   使用 Haversine 公式计算距离（更简单，长距离精度较低）。
  ///   当 Vincenty 公式无法收敛时用作后备方法。
  ///   假设球形地球，半径等于 WGS84 半长轴。
  /// </summary>
  /// <param name="point1">第一个点（度）。</param>
  /// <param name="point2">第二个点（度）。</param>
  /// <returns>距离（米）（对于大多数距离精确到约 0.5% 以内）。</returns>
  private static double HaversineDistance(Point point1, Point point2)
  {
    var lat1 = ToRadians(point1.Y);
    var lon1 = ToRadians(point1.X);
    var lat2 = ToRadians(point2.Y);
    var lon2 = ToRadians(point2.X);

    var dLat = lat2 - lat1;
    var dLon = lon2 - lon1;

    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

    return WGS84_SEMI_MAJOR_AXIS * c;
  }

  /// <summary>
  ///   将度转换为弧度。
  /// </summary>
  /// <param name="degrees">度数角度。</param>
  /// <returns>弧度角度。</returns>
  private static double ToRadians(double degrees)
  {
    return degrees * Math.PI / 180.0;
  }
}