namespace Esri.Geometry.Core.Geometries
{
    /// <summary>
    /// Base abstract class for all geometry types.
    /// </summary>
    public abstract class Geometry
    {
        /// <summary>
        /// Gets the type of the geometry.
        /// </summary>
        public abstract GeometryType Type { get; }

        /// <summary>
        /// Gets a value indicating whether this geometry is empty.
        /// </summary>
        public abstract bool IsEmpty { get; }

        /// <summary>
        /// Gets the envelope (bounding rectangle) of this geometry.
        /// </summary>
        /// <returns>An Envelope object representing the bounding rectangle.</returns>
        public abstract Envelope GetEnvelope();

        /// <summary>
        /// Gets the dimension of the geometry.
        /// 0 for points, 1 for lines, 2 for polygons.
        /// </summary>
        public abstract int Dimension { get; }
    }
}
