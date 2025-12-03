using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core
{
    /// <summary>
    /// The MapGeometry class bundles a geometry with its spatial reference together.
    /// To work with a geometry object in a map, it is necessary to have a spatial
    /// reference defined for this geometry.
    /// </summary>
    public class MapGeometry : IEquatable<MapGeometry>
    {
        /// <summary>
        /// Gets or sets the geometry.
        /// </summary>
        public Geometries.Geometry? Geometry { get; set; }

        /// <summary>
        /// Gets or sets the spatial reference.
        /// </summary>
        public SpatialReference.SpatialReference? SpatialReference { get; set; }

        /// <summary>
        /// Constructs an empty MapGeometry instance.
        /// </summary>
        public MapGeometry()
        {
        }

        /// <summary>
        /// Constructs a MapGeometry instance using the specified geometry and spatial reference.
        /// </summary>
        /// <param name="geometry">The geometry to construct the new MapGeometry object.</param>
        /// <param name="spatialReference">The spatial reference of the geometry.</param>
        public MapGeometry(Geometries.Geometry? geometry, SpatialReference.SpatialReference? spatialReference)
        {
            Geometry = geometry;
            SpatialReference = spatialReference;
        }

        /// <summary>
        /// Returns a string representation of this MapGeometry for debugging purposes.
        /// </summary>
        public override string ToString()
        {
            if (Geometry == null)
                return "MapGeometry [null geometry]";

            try
            {
                var json = IO.EsriJsonExportOperator.Instance.Execute(Geometry, SpatialReference);
                if (json.Length > 200)
                {
                    return json.Substring(0, 197) + $"... ({json.Length} characters)";
                }
                return json;
            }
            catch
            {
                return $"MapGeometry [Type: {Geometry.Type}]";
            }
        }

        /// <summary>
        /// Determines whether this MapGeometry is equal to another MapGeometry.
        /// </summary>
        public bool Equals(MapGeometry? other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            // Compare spatial references
            bool srEqual = false;
            if (SpatialReference == null && other.SpatialReference == null)
            {
                srEqual = true;
            }
            else if (SpatialReference != null && other.SpatialReference != null)
            {
                // Compare by WKID if both have it
                if (SpatialReference.Wkid.HasValue && other.SpatialReference.Wkid.HasValue)
                {
                    srEqual = SpatialReference.Wkid.Value == other.SpatialReference.Wkid.Value;
                }
                else if (SpatialReference.Wkt != null && other.SpatialReference.Wkt != null)
                {
                    srEqual = SpatialReference.Wkt == other.SpatialReference.Wkt;
                }
                else
                {
                    srEqual = ReferenceEquals(SpatialReference, other.SpatialReference);
                }
            }

            if (!srEqual)
                return false;

            // Compare geometries
            if (Geometry == null && other.Geometry == null)
                return true;
            
            if (Geometry == null || other.Geometry == null)
                return false;
                
            // For Point geometries, use value-based comparison
            if (Geometry is Geometries.Point point1 && other.Geometry is Geometries.Point point2)
            {
                return point1.Equals(point2, 1e-10);
            }
            
            // For other geometries, use reference comparison for now
            return ReferenceEquals(Geometry, other.Geometry);
        }

        /// <summary>
        /// Determines whether this MapGeometry is equal to another object.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return Equals(obj as MapGeometry);
        }

        /// <summary>
        /// Returns the hash code for this MapGeometry.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 0x2937912;
            
            if (SpatialReference != null)
                hash ^= SpatialReference.GetHashCode();
            
            if (Geometry != null)
                hash ^= Geometry.GetHashCode();
            
            return hash;
        }

        /// <summary>
        /// Equality operator for MapGeometry.
        /// </summary>
        public static bool operator ==(MapGeometry? left, MapGeometry? right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for MapGeometry.
        /// </summary>
        public static bool operator !=(MapGeometry? left, MapGeometry? right)
        {
            return !(left == right);
        }
    }
}
