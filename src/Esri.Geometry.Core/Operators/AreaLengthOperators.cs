using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for calculating the 2D area of a geometry.
///   Returns the planar (Euclidean) area in square coordinate system units.
/// </summary>
/// <remarks>
///   Area calculation applies to:
///   - Polygon: Sum of areas of all rings (uses shoelace formula)
///   - Envelope: Width × Height
///   - Other geometry types: Returns 0 (points and lines have no area)
///   
///   Important notes:
///   - Calculates planar 2D area, not geodesic area
///   - For geographic coordinates (lat/lon), use GeodesicAreaOperator instead
///   - Area is always non-negative
///   - Empty or degenerate geometries return 0
///   
///   Time Complexity: O(n) where n is the number of vertices
/// </remarks>
public class AreaOperator : IGeometryOperator<double>
{
  private static readonly Lazy<AreaOperator> _instance = new(() => new AreaOperator());

  private AreaOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the area operator.
  /// </summary>
  public static AreaOperator Instance => _instance.Value;

  /// <summary>
  ///   Calculates the 2D planar area of the geometry.
  /// </summary>
  /// <param name="geometry">The geometry to calculate area for.</param>
  /// <param name="spatialRef">Optional spatial reference (currently not used).</param>
  /// <returns>
  ///   The area in square coordinate system units:
  ///   - Polygon: Area using shoelace formula
  ///   - Envelope: Width × Height
  ///   - Other types: 0
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when geometry is null.</exception>
  public double Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null) throw new ArgumentNullException(nameof(geometry));

    if (geometry.IsEmpty) return 0;

    // Only 2D geometries have area
    if (geometry.Dimension < 2) return 0;

    if (geometry is Polygon polygon) return polygon.Area;

    if (geometry is Envelope envelope) return envelope.Area;

    return 0;
  }
}

/// <summary>
///   Operator for calculating the length or perimeter of a geometry.
///   Returns the planar (Euclidean) length in coordinate system units.
/// </summary>
/// <remarks>
///   Length calculation applies to:
///   - Line: Euclidean distance between start and end points
///   - Polyline: Sum of all segment lengths across all paths
///   - Polygon: Perimeter (sum of all ring lengths)
///   - Envelope: Perimeter = 2 × (Width + Height)
///   - Point/MultiPoint: Returns 0 (no length)
///   
///   Important notes:
///   - Calculates planar 2D length, not geodesic length
///   - For geographic coordinates (lat/lon), lengths may be inaccurate over large distances
///   - Length is always non-negative
///   - Empty geometries return 0
///   
///   Time Complexity: O(n) where n is the number of segments
/// </remarks>
public class LengthOperator : IGeometryOperator<double>
{
  private static readonly Lazy<LengthOperator> _instance = new(() => new LengthOperator());

  private LengthOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the length operator.
  /// </summary>
  public static LengthOperator Instance => _instance.Value;

  /// <summary>
  ///   Calculates the length or perimeter of the geometry.
  /// </summary>
  /// <param name="geometry">The geometry to calculate length for.</param>
  /// <param name="spatialRef">Optional spatial reference (currently not used).</param>
  /// <returns>
  ///   The length/perimeter in coordinate system units:
  ///   - Line: Distance between endpoints
  ///   - Polyline: Sum of all path lengths
  ///   - Polygon: Perimeter (sum of all ring lengths)
  ///   - Envelope: 2 × (Width + Height)
  ///   - Point: 0
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when geometry is null.</exception>
  public double Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null) throw new ArgumentNullException(nameof(geometry));

    if (geometry.IsEmpty) return 0;

    if (geometry is Line line) return line.Length;

    if (geometry is Polyline polyline) return polyline.Length;

    // Envelope perimeter
    if (geometry is Envelope envelope) return 2 * (envelope.Width + envelope.Height);

    // Calculate polygon perimeter by summing edge lengths
    if (geometry is Polygon polygon)
    {
      double length = 0;
      foreach (var ring in polygon.GetRings())
        // Sum distances between consecutive vertices
        for (var i = 0; i < ring.Count - 1; i++)
          length += ring[i].Distance(ring[i + 1]);

      return length;
    }

    return 0;
  }
}