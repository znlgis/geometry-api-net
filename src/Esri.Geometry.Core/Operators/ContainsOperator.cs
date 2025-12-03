using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Operator for testing if one geometry contains another.
    /// </summary>
    public class ContainsOperator : IBinaryGeometryOperator<bool>
    {
        private static readonly Lazy<ContainsOperator> _instance = new Lazy<ContainsOperator>(() => new ContainsOperator());

        /// <summary>
        /// Gets the singleton instance of the contains operator.
        /// </summary>
        public static ContainsOperator Instance => _instance.Value;

        private ContainsOperator()
        {
        }

        /// <inheritdoc/>
        public bool Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2, SpatialReference.SpatialReference? spatialRef = null)
        {
            if (geometry1 == null)
            {
                throw new ArgumentNullException(nameof(geometry1));
            }
            if (geometry2 == null)
            {
                throw new ArgumentNullException(nameof(geometry2));
            }

            // Simple implementation for envelope-point containment
            if (geometry1 is Envelope env && geometry2 is Point p)
            {
                return env.Contains(p);
            }

            // For other geometry types, this would require more complex implementations
            throw new NotImplementedException($"Contains test between {geometry1.Type} and {geometry2.Type} is not yet implemented.");
        }
    }
}
