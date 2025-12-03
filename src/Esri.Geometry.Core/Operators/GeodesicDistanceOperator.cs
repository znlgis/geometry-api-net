using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for calculating geodesic (great circle) distances on the WGS84 ellipsoid.
///   Points are expected to be in geographic coordinates (longitude, latitude) in degrees.
///   X coordinate = Longitude (-180 to 180), Y coordinate = Latitude (-90 to 90).
/// </summary>
/// <remarks>
///   This operator uses Vincenty's inverse formula, which is one of the most accurate methods
///   for calculating distances on an ellipsoid. It accounts for the Earth's oblate spheroid shape.
///   
///   Algorithm:
///   - Primary: Vincenty's formula (accurate to within 0.5mm on the WGS84 ellipsoid)
///   - Fallback: Haversine formula (if Vincenty fails to converge, typically near antipodal points)
///   
///   WGS84 Parameters:
///   - Semi-major axis (equatorial radius): 6,378,137 meters
///   - Semi-minor axis (polar radius): 6,356,752.314245 meters
///   - Flattening: ~1/298.257223563
///   
///   Time Complexity: O(1) with iterative convergence (typically 2-5 iterations)
///   Accuracy: Sub-millimeter precision for most point pairs
/// </remarks>
public class GeodesicDistanceOperator : IBinaryGeometryOperator<double>
{
  private const double WGS84_SEMI_MAJOR_AXIS = 6378137.0; // meters
  private const double WGS84_SEMI_MINOR_AXIS = 6356752.314245; // meters
  private const double WGS84_FLATTENING = (WGS84_SEMI_MAJOR_AXIS - WGS84_SEMI_MINOR_AXIS) / WGS84_SEMI_MAJOR_AXIS;

  private static readonly Lazy<GeodesicDistanceOperator> _instance = new(() => new GeodesicDistanceOperator());

  private GeodesicDistanceOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the geodesic distance operator.
  /// </summary>
  public static GeodesicDistanceOperator Instance => _instance.Value;

  /// <summary>
  ///   Calculates the geodesic distance between two point geometries.
  /// </summary>
  /// <param name="geometry1">First point geometry in geographic coordinates (longitude, latitude).</param>
  /// <param name="geometry2">Second point geometry in geographic coordinates (longitude, latitude).</param>
  /// <param name="spatialRef">Optional spatial reference (currently not used).</param>
  /// <returns>The geodesic distance in meters.</returns>
  /// <exception cref="ArgumentNullException">Thrown when either geometry is null.</exception>
  /// <exception cref="ArgumentException">Thrown when either geometry is not a Point.</exception>
  /// <example>
  ///   <code>
  ///   // Calculate distance from New York to London
  ///   var newYork = new Point(-74.0060, 40.7128);  // Lon, Lat
  ///   var london = new Point(-0.1278, 51.5074);
  ///   double distance = GeodesicDistanceOperator.Instance.Execute(newYork, london);
  ///   // Result: ~5,570,000 meters (5,570 km)
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
  ///   Calculates the geodesic distance between two points on the WGS84 ellipsoid using Vincenty's inverse formula.
  ///   This iterative method solves the inverse geodetic problem: given two points, find the distance and azimuths.
  /// </summary>
  /// <param name="point1">First point with coordinates in degrees (X=Longitude, Y=Latitude).</param>
  /// <param name="point2">Second point with coordinates in degrees (X=Longitude, Y=Latitude).</param>
  /// <returns>The geodesic distance in meters, accurate to within 0.5mm.</returns>
  /// <remarks>
  ///   The algorithm iteratively converges to the solution using reduced latitudes and
  ///   auxiliary variables. Falls back to Haversine formula if convergence fails (rare,
  ///   typically near antipodal points).
  /// </remarks>
  public static double CalculateGeodesicDistance(Point point1, Point point2)
  {
    // Convert geographic coordinates from degrees to radians
    var lon1 = ToRadians(point1.X);
    var lat1 = ToRadians(point1.Y);
    var lon2 = ToRadians(point2.X);
    var lat2 = ToRadians(point2.Y);

    // WGS84 ellipsoid parameters
    var a = WGS84_SEMI_MAJOR_AXIS;  // Equatorial radius
    var f = WGS84_FLATTENING;        // Flattening
    var b = (1 - f) * a;             // Polar radius

    // Difference in longitude
    var L = lon2 - lon1;
    
    // Reduced latitudes (auxiliary sphere)
    var U1 = Math.Atan((1 - f) * Math.Tan(lat1));
    var U2 = Math.Atan((1 - f) * Math.Tan(lat2));
    var sinU1 = Math.Sin(U1);
    var cosU1 = Math.Cos(U1);
    var sinU2 = Math.Sin(U2);
    var cosU2 = Math.Cos(U2);

    // Initialize lambda (longitude difference on auxiliary sphere)
    var lambda = L;
    double lambdaP;
    var iterLimit = 100;  // Maximum iterations for convergence
    double cosSqAlpha = 0;
    double sinSigma = 0;
    double cos2SigmaM = 0;
    double cosSigma = 0;
    double sigma = 0;

    // Iterative calculation using Vincenty's formula
    do
    {
      var sinLambda = Math.Sin(lambda);
      var cosLambda = Math.Cos(lambda);
      
      // Calculate sine of angular distance
      sinSigma = Math.Sqrt(cosU2 * sinLambda * (cosU2 * sinLambda) +
                           (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) * (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));

      if (sinSigma == 0) return 0; // Co-incident points (same location)

      // Calculate cosine of angular distance
      cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
      sigma = Math.Atan2(sinSigma, cosSigma);
      
      // Calculate azimuth from north
      var sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
      cosSqAlpha = 1 - sinAlpha * sinAlpha;

      // Handle equatorial line case
      if (cosSqAlpha != 0)
        cos2SigmaM = cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;
      else
        cos2SigmaM = 0; // Equatorial line: latitude = 0

      // Calculate convergence parameter C
      var C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));
      lambdaP = lambda;
      
      // Update lambda for next iteration
      lambda = L + (1 - C) * f * sinAlpha *
        (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
    } while (Math.Abs(lambda - lambdaP) > 1e-12 && --iterLimit > 0);

    if (iterLimit == 0)
      // Formula failed to converge (rare, typically near antipodal points)
      // Fall back to simpler Haversine formula
      return HaversineDistance(point1, point2);

    // Calculate final distance using converged values
    var uSq = cosSqAlpha * (a * a - b * b) / (b * b);
    var A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
    var B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
    var deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
                                                           B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) *
                                                           (-3 + 4 * cos2SigmaM * cos2SigmaM)));

    // Distance in meters
    var s = b * A * (sigma - deltaSigma);

    return s;
  }

  /// <summary>
  ///   Calculates distance using the Haversine formula (simpler, less accurate for long distances).
  ///   This is used as a fallback when Vincenty's formula fails to converge.
  ///   Assumes a spherical Earth with radius equal to WGS84 semi-major axis.
  /// </summary>
  /// <param name="point1">First point in degrees.</param>
  /// <param name="point2">Second point in degrees.</param>
  /// <returns>Distance in meters (accurate to within ~0.5% for most distances).</returns>
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
  ///   Converts degrees to radians.
  /// </summary>
  /// <param name="degrees">Angle in degrees.</param>
  /// <returns>Angle in radians.</returns>
  private static double ToRadians(double degrees)
  {
    return degrees * Math.PI / 180.0;
  }
}