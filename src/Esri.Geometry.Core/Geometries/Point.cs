using System;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   Represents a point geometry with X and Y coordinates.
/// </summary>
public class Point : Geometry
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="Point" /> class.
    /// </summary>
    public Point()
  {
    X = double.NaN;
    Y = double.NaN;
  }

    /// <summary>
    ///   Initializes a new instance of the <see cref="Point" /> class with specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Point(double x, double y)
  {
    X = x;
    Y = y;
  }

    /// <summary>
    ///   Initializes a new instance of the <see cref="Point" /> class with specified coordinates and Z value.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    public Point(double x, double y, double z)
  {
    X = x;
    Y = y;
    Z = z;
  }

    /// <summary>
    ///   Gets or sets the X coordinate.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    ///   Gets or sets the Y coordinate.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    ///   Gets or sets the Z coordinate (optional).
    /// </summary>
    public double? Z { get; set; }

    /// <summary>
    ///   Gets or sets the M value (measure, optional).
    /// </summary>
    public double? M { get; set; }

    /// <inheritdoc />
    public override GeometryType Type => GeometryType.Point;

    /// <inheritdoc />
    public override bool IsEmpty => double.IsNaN(X) || double.IsNaN(Y);

    /// <inheritdoc />
    public override int Dimension => 0;

    /// <inheritdoc />
    public override Envelope GetEnvelope()
  {
    if (IsEmpty) return new Envelope();
    return new Envelope(X, Y, X, Y);
  }

    /// <summary>
    ///   Calculates the distance to another point.
    /// </summary>
    /// <param name="other">The other point.</param>
    /// <returns>The distance between the two points.</returns>
    public double Distance(Point other)
  {
    if (other == null) throw new ArgumentNullException(nameof(other));

    var dx = X - other.X;
    var dy = Y - other.Y;
    return Math.Sqrt(dx * dx + dy * dy);
  }

    /// <summary>
    ///   Determines whether this point is equal to another point.
    /// </summary>
    /// <param name="other">The other point.</param>
    /// <param name="tolerance">The tolerance for comparison.</param>
    /// <returns>True if the points are equal within the tolerance, false otherwise.</returns>
    public bool Equals(Point other, double tolerance = GeometryConstants.DefaultTolerance)
  {
    if (other == null) return false;

    return Math.Abs(X - other.X) <= tolerance && Math.Abs(Y - other.Y) <= tolerance;
  }
}