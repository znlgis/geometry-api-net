using System;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   Represents an axis-aligned bounding rectangle (AABR).
///   Also known as a Minimum Bounding Rectangle (MBR) or bounding box.
/// </summary>
/// <remarks>
///   An envelope is defined by its minimum and maximum X and Y coordinates,
///   forming a rectangle with sides parallel to the coordinate axes.
///   
///   Common uses:
///   - Spatial indexing (quick bounds checking before detailed tests)
///   - Viewport/window clipping
///   - Rough containment tests (faster than precise geometry tests)
///   - Geometry simplification (representing complex shapes)
///   
///   Properties:
///   - Always axis-aligned (cannot be rotated)
///   - Empty envelopes have NaN coordinates
///   - Can be degenerate (point or line if XMin=XMax or YMin=YMax)
///   
///   Performance note: Envelope operations are typically O(1) and very fast,
///   making them ideal for preliminary spatial filtering.
/// </remarks>
public class Envelope : Geometry
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="Envelope" /> class.
    /// </summary>
    public Envelope()
  {
    XMin = double.NaN;
    YMin = double.NaN;
    XMax = double.NaN;
    YMax = double.NaN;
  }

    /// <summary>
    ///   Initializes a new instance of the <see cref="Envelope" /> class with specified bounds.
    /// </summary>
    /// <param name="xMin">The minimum X coordinate (left edge).</param>
    /// <param name="yMin">The minimum Y coordinate (bottom edge).</param>
    /// <param name="xMax">The maximum X coordinate (right edge).</param>
    /// <param name="yMax">The maximum Y coordinate (top edge).</param>
    /// <remarks>
    ///   Note: This constructor does not validate that xMin ≤ xMax and yMin ≤ yMax.
    ///   Callers should ensure proper ordering of coordinates.
    /// </remarks>
    public Envelope(double xMin, double yMin, double xMax, double yMax)
  {
    XMin = xMin;
    YMin = yMin;
    XMax = xMax;
    YMax = yMax;
  }

    /// <summary>
    ///   Gets or sets the minimum X coordinate.
    /// </summary>
    public double XMin { get; set; }

    /// <summary>
    ///   Gets or sets the minimum Y coordinate.
    /// </summary>
    public double YMin { get; set; }

    /// <summary>
    ///   Gets or sets the maximum X coordinate.
    /// </summary>
    public double XMax { get; set; }

    /// <summary>
    ///   Gets or sets the maximum Y coordinate.
    /// </summary>
    public double YMax { get; set; }

    /// <inheritdoc />
    public override GeometryType Type => GeometryType.Envelope;

    /// <inheritdoc />
    public override bool IsEmpty => double.IsNaN(XMin) || double.IsNaN(YMin) ||
                                    double.IsNaN(XMax) || double.IsNaN(YMax);

    /// <inheritdoc />
    public override int Dimension => 2;

    /// <summary>
    ///   Gets the width of the envelope (XMax - XMin).
    /// </summary>
    /// <remarks>
    ///   Returns 0 for empty envelopes.
    ///   Can be 0 for degenerate envelopes (vertical line).
    /// </remarks>
    public double Width => IsEmpty ? 0 : XMax - XMin;

    /// <summary>
    ///   Gets the height of the envelope (YMax - YMin).
    /// </summary>
    /// <remarks>
    ///   Returns 0 for empty envelopes.
    ///   Can be 0 for degenerate envelopes (horizontal line).
    /// </remarks>
    public double Height => IsEmpty ? 0 : YMax - YMin;

    /// <summary>
    ///   Gets the center point of the envelope.
    /// </summary>
    /// <remarks>
    ///   Calculated as the midpoint: ((XMin+XMax)/2, (YMin+YMax)/2).
    ///   Returns an empty point for empty envelopes.
    /// </remarks>
    public Point Center => IsEmpty ? new Point() : new Point((XMin + XMax) / 2, (YMin + YMax) / 2);

    /// <summary>
    ///   Gets the area of the envelope (Width × Height).
    /// </summary>
    /// <remarks>
    ///   Returns 0 for empty or degenerate envelopes.
    ///   Always non-negative.
    /// </remarks>
    public double Area => IsEmpty ? 0 : Width * Height;

    /// <inheritdoc />
    public override Envelope GetEnvelope()
  {
    return this;
  }

    /// <summary>
    ///   Determines whether this envelope contains a point.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point lies within or on the boundary of the envelope, false otherwise.</returns>
    /// <remarks>
    ///   A point is considered contained if:
    ///   XMin ≤ point.X ≤ XMax AND YMin ≤ point.Y ≤ YMax
    ///   Returns false for null, empty points, or empty envelopes.
    /// </remarks>
    public bool Contains(Point point)
  {
    if (point == null || point.IsEmpty || IsEmpty) return false;

    return point.X >= XMin && point.X <= XMax && point.Y >= YMin && point.Y <= YMax;
  }

    /// <summary>
    ///   Determines whether this envelope intersects another envelope.
    /// </summary>
    /// <param name="other">The other envelope.</param>
    /// <returns>True if the envelopes intersect, false otherwise.</returns>
    public bool Intersects(Envelope other)
  {
    if (other == null || IsEmpty || other.IsEmpty) return false;

    return !(XMax < other.XMin || XMin > other.XMax || YMax < other.YMin || YMin > other.YMax);
  }

    /// <summary>
    ///   Merges this envelope with another point.
    /// </summary>
    /// <param name="point">The point to merge.</param>
    public void Merge(Point point)
  {
    if (point == null || point.IsEmpty) return;

    if (IsEmpty)
    {
      XMin = XMax = point.X;
      YMin = YMax = point.Y;
    }
    else
    {
      XMin = Math.Min(XMin, point.X);
      YMin = Math.Min(YMin, point.Y);
      XMax = Math.Max(XMax, point.X);
      YMax = Math.Max(YMax, point.Y);
    }
  }

    /// <summary>
    ///   Merges this envelope with another envelope.
    /// </summary>
    /// <param name="other">The envelope to merge.</param>
    public void Merge(Envelope other)
  {
    if (other == null || other.IsEmpty) return;

    if (IsEmpty)
    {
      XMin = other.XMin;
      YMin = other.YMin;
      XMax = other.XMax;
      YMax = other.YMax;
    }
    else
    {
      XMin = Math.Min(XMin, other.XMin);
      YMin = Math.Min(YMin, other.YMin);
      XMax = Math.Max(XMax, other.XMax);
      YMax = Math.Max(YMax, other.YMax);
    }
  }
}