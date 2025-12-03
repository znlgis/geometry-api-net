using System;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于计算 WGS84 椭球上大地测量（大圆）距离的操作符.
///     Points are expected to be in geographic coordinates (longitude, latitude) in degrees.
///     X coordinate = Longitude (-180 to 180), Y coordinate = Latitude (-90 to 90).
/// </summary>
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
    ///     获取 GeodesicDistanceOperator 的单例实例.
    /// </summary>
    public static GeodesicDistanceOperator Instance => _instance.Value;

    /// <inheritdoc />
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
    ///     使用 Vincenty 公式计算 WGS84 椭球上两点之间的大地测量距离.
    /// </summary>
    /// <param name="point1">First point with coordinates in degrees (X=Longitude, Y=Latitude).</param>
    /// <param name="point2">Second point with coordinates in degrees (X=Longitude, Y=Latitude).</param>
    /// <returns>Distance in meters.</returns>
    public static double CalculateGeodesicDistance(Point point1, Point point2)
    {
        // Points are in lon/lat (X, Y) format in degrees
        var lon1 = ToRadians(point1.X);
        var lat1 = ToRadians(point1.Y);
        var lon2 = ToRadians(point2.X);
        var lat2 = ToRadians(point2.Y);

        // Vincenty's formula for distance on an ellipsoid
        var a = WGS84_SEMI_MAJOR_AXIS;
        var f = WGS84_FLATTENING;
        var b = (1 - f) * a;

        var L = lon2 - lon1;
        var U1 = Math.Atan((1 - f) * Math.Tan(lat1));
        var U2 = Math.Atan((1 - f) * Math.Tan(lat2));
        var sinU1 = Math.Sin(U1);
        var cosU1 = Math.Cos(U1);
        var sinU2 = Math.Sin(U2);
        var cosU2 = Math.Cos(U2);

        var lambda = L;
        double lambdaP;
        var iterLimit = 100;
        double cosSqAlpha = 0;
        double sinSigma = 0;
        double cos2SigmaM = 0;
        double cosSigma = 0;
        double sigma = 0;

        do
        {
            var sinLambda = Math.Sin(lambda);
            var cosLambda = Math.Cos(lambda);
            sinSigma = Math.Sqrt(cosU2 * sinLambda * (cosU2 * sinLambda) +
                                 (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) *
                                 (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));

            if (sinSigma == 0) return 0; // Co-incident points

            cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            sigma = Math.Atan2(sinSigma, cosSigma);
            var sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            cosSqAlpha = 1 - sinAlpha * sinAlpha;

            if (cosSqAlpha != 0)
                cos2SigmaM = cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;
            else
                cos2SigmaM = 0; // Equatorial line

            var C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));
            lambdaP = lambda;
            lambda = L + (1 - C) * f * sinAlpha *
                (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
        } while (Math.Abs(lambda - lambdaP) > 1e-12 && --iterLimit > 0);

        if (iterLimit == 0)
            // Formula failed to converge, fall back to simpler haversine
            return HaversineDistance(point1, point2);

        var uSq = cosSqAlpha * (a * a - b * b) / (b * b);
        var A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
        var B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
        var deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
                                                               B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) *
                                                               (-3 + 4 * cos2SigmaM * cos2SigmaM)));

        var s = b * A * (sigma - deltaSigma);

        return s;
    }

    /// <summary>
    ///     使用 Haversine 公式计算距离（更简单，但对长距离精度较低）.
    /// </summary>
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

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}