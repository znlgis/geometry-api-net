using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Operator for calculating geodesic (great circle) distances on the WGS84 ellipsoid.
    /// Points are expected to be in geographic coordinates (longitude, latitude) in degrees.
    /// X coordinate = Longitude (-180 to 180), Y coordinate = Latitude (-90 to 90).
    /// </summary>
    public class GeodesicDistanceOperator : IBinaryGeometryOperator<double>
    {
        private const double WGS84_SEMI_MAJOR_AXIS = 6378137.0; // meters
        private const double WGS84_SEMI_MINOR_AXIS = 6356752.314245; // meters
        private const double WGS84_FLATTENING = (WGS84_SEMI_MAJOR_AXIS - WGS84_SEMI_MINOR_AXIS) / WGS84_SEMI_MAJOR_AXIS;

        private static readonly Lazy<GeodesicDistanceOperator> _instance = new Lazy<GeodesicDistanceOperator>(() => new GeodesicDistanceOperator());

        /// <summary>
        /// Gets the singleton instance of the geodesic distance operator.
        /// </summary>
        public static GeodesicDistanceOperator Instance => _instance.Value;

        private GeodesicDistanceOperator()
        {
        }

        /// <inheritdoc/>
        public double Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2, SpatialReference.SpatialReference? spatialRef = null)
        {
            if (geometry1 == null)
            {
                throw new ArgumentNullException(nameof(geometry1));
            }
            if (geometry2 == null)
            {
                throw new ArgumentNullException(nameof(geometry2));
            }

            // Only support points for now
            if (!(geometry1 is Point point1))
            {
                throw new ArgumentException("Geodesic distance currently only supports Point geometries for geometry1.", nameof(geometry1));
            }

            if (!(geometry2 is Point point2))
            {
                throw new ArgumentException("Geodesic distance currently only supports Point geometries for geometry2.", nameof(geometry2));
            }

            return CalculateGeodesicDistance(point1, point2);
        }

        /// <summary>
        /// Calculates the geodesic distance between two points on the WGS84 ellipsoid using Vincenty's formula.
        /// </summary>
        /// <param name="point1">First point with coordinates in degrees (X=Longitude, Y=Latitude).</param>
        /// <param name="point2">Second point with coordinates in degrees (X=Longitude, Y=Latitude).</param>
        /// <returns>Distance in meters.</returns>
        public static double CalculateGeodesicDistance(Point point1, Point point2)
        {
            // Points are in lon/lat (X, Y) format in degrees
            double lon1 = ToRadians(point1.X);
            double lat1 = ToRadians(point1.Y);
            double lon2 = ToRadians(point2.X);
            double lat2 = ToRadians(point2.Y);

            // Vincenty's formula for distance on an ellipsoid
            double a = WGS84_SEMI_MAJOR_AXIS;
            double f = WGS84_FLATTENING;
            double b = (1 - f) * a;

            double L = lon2 - lon1;
            double U1 = Math.Atan((1 - f) * Math.Tan(lat1));
            double U2 = Math.Atan((1 - f) * Math.Tan(lat2));
            double sinU1 = Math.Sin(U1);
            double cosU1 = Math.Cos(U1);
            double sinU2 = Math.Sin(U2);
            double cosU2 = Math.Cos(U2);

            double lambda = L;
            double lambdaP;
            int iterLimit = 100;
            double cosSqAlpha = 0;
            double sinSigma = 0;
            double cos2SigmaM = 0;
            double cosSigma = 0;
            double sigma = 0;

            do
            {
                double sinLambda = Math.Sin(lambda);
                double cosLambda = Math.Cos(lambda);
                sinSigma = Math.Sqrt((cosU2 * sinLambda) * (cosU2 * sinLambda) +
                    (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) * (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));

                if (sinSigma == 0)
                {
                    return 0; // Co-incident points
                }

                cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
                sigma = Math.Atan2(sinSigma, cosSigma);
                double sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
                cosSqAlpha = 1 - sinAlpha * sinAlpha;

                if (cosSqAlpha != 0)
                {
                    cos2SigmaM = cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;
                }
                else
                {
                    cos2SigmaM = 0; // Equatorial line
                }

                double C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));
                lambdaP = lambda;
                lambda = L + (1 - C) * f * sinAlpha *
                    (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));

            } while (Math.Abs(lambda - lambdaP) > 1e-12 && --iterLimit > 0);

            if (iterLimit == 0)
            {
                // Formula failed to converge, fall back to simpler haversine
                return HaversineDistance(point1, point2);
            }

            double uSq = cosSqAlpha * (a * a - b * b) / (b * b);
            double A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
            double B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
            double deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
                B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM * cos2SigmaM)));

            double s = b * A * (sigma - deltaSigma);

            return s;
        }

        /// <summary>
        /// Calculates distance using the Haversine formula (simpler, less accurate for long distances).
        /// </summary>
        private static double HaversineDistance(Point point1, Point point2)
        {
            double lat1 = ToRadians(point1.Y);
            double lon1 = ToRadians(point1.X);
            double lat2 = ToRadians(point2.Y);
            double lon2 = ToRadians(point2.X);

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return WGS84_SEMI_MAJOR_AXIS * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
