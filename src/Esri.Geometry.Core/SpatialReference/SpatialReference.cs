namespace Esri.Geometry.Core.SpatialReference
{
    /// <summary>
    /// Represents a spatial reference system for geometries.
    /// </summary>
    public class SpatialReference
    {
        /// <summary>
        /// Gets or sets the well-known ID (WKID) of the spatial reference.
        /// </summary>
        public int? Wkid { get; set; }

        /// <summary>
        /// Gets or sets the latest well-known ID for the spatial reference.
        /// </summary>
        public int? LatestWkid { get; set; }

        /// <summary>
        /// Gets or sets the well-known text (WKT) representation of the spatial reference.
        /// </summary>
        public string? Wkt { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialReference"/> class.
        /// </summary>
        public SpatialReference()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialReference"/> class with a WKID.
        /// </summary>
        /// <param name="wkid">The well-known ID.</param>
        public SpatialReference(int wkid)
        {
            Wkid = wkid;
        }

        /// <summary>
        /// Creates a spatial reference for WGS 84 (EPSG:4326).
        /// </summary>
        /// <returns>A WGS 84 spatial reference.</returns>
        public static SpatialReference Wgs84()
        {
            return new SpatialReference(4326);
        }

        /// <summary>
        /// Creates a spatial reference for Web Mercator (EPSG:3857).
        /// </summary>
        /// <returns>A Web Mercator spatial reference.</returns>
        public static SpatialReference WebMercator()
        {
            return new SpatialReference(3857);
        }
    }
}
