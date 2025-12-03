namespace Esri.Geometry.Core.Geometries
{
    /// <summary>
    /// Defines the types of geometries supported by the API.
    /// </summary>
    public enum GeometryType
    {
        /// <summary>
        /// Unknown geometry type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A point geometry.
        /// </summary>
        Point = 1,

        /// <summary>
        /// A line geometry (segment between two points).
        /// </summary>
        Line = 2,

        /// <summary>
        /// An envelope (bounding rectangle) geometry.
        /// </summary>
        Envelope = 3,

        /// <summary>
        /// A multi-point geometry.
        /// </summary>
        MultiPoint = 4,

        /// <summary>
        /// A polyline geometry.
        /// </summary>
        Polyline = 5,

        /// <summary>
        /// A polygon geometry.
        /// </summary>
        Polygon = 6
    }
}
