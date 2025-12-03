using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Operator for calculating the distance between geometries.
    /// </summary>
    public class DistanceOperator : IBinaryGeometryOperator<double>
    {
        private static readonly Lazy<DistanceOperator> _instance = new Lazy<DistanceOperator>(() => new DistanceOperator());

        /// <summary>
        /// Gets the singleton instance of the distance operator.
        /// </summary>
        public static DistanceOperator Instance => _instance.Value;

        private DistanceOperator()
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

            // Simple implementation for point-to-point distance
            if (geometry1 is Point p1 && geometry2 is Point p2)
            {
                return p1.Distance(p2);
            }

            // For other geometry types, this would require more complex implementations
            throw new NotImplementedException($"Distance calculation between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
        }
    }
}
