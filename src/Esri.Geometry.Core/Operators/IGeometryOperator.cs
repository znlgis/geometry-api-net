using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators
{
    /// <summary>
    /// Interface for geometry operators.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    public interface IGeometryOperator<TResult>
    {
        /// <summary>
        /// Executes the operation on a geometry.
        /// </summary>
        /// <param name="geometry">The geometry to operate on.</param>
        /// <param name="spatialRef">The spatial reference (optional).</param>
        /// <returns>The result of the operation.</returns>
        TResult Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null);
    }

    /// <summary>
    /// Interface for binary geometry operators.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    public interface IBinaryGeometryOperator<TResult>
    {
        /// <summary>
        /// Executes the operation on two geometries.
        /// </summary>
        /// <param name="geometry1">The first geometry.</param>
        /// <param name="geometry2">The second geometry.</param>
        /// <param name="spatialRef">The spatial reference (optional).</param>
        /// <returns>The result of the operation.</returns>
        TResult Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2, SpatialReference.SpatialReference? spatialRef = null);
    }
}
