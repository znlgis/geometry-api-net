using System;

namespace Esri.Geometry.Core.Geometries
{
    /// <summary>
    /// Represents an axis-aligned bounding rectangle.
    /// </summary>
    public class Envelope : Geometry
    {
        /// <summary>
        /// Gets or sets the minimum X coordinate.
        /// </summary>
        public double XMin { get; set; }

        /// <summary>
        /// Gets or sets the minimum Y coordinate.
        /// </summary>
        public double YMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum X coordinate.
        /// </summary>
        public double XMax { get; set; }

        /// <summary>
        /// Gets or sets the maximum Y coordinate.
        /// </summary>
        public double YMax { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class.
        /// </summary>
        public Envelope()
        {
            XMin = double.NaN;
            YMin = double.NaN;
            XMax = double.NaN;
            YMax = double.NaN;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with specified bounds.
        /// </summary>
        /// <param name="xMin">The minimum X coordinate.</param>
        /// <param name="yMin">The minimum Y coordinate.</param>
        /// <param name="xMax">The maximum X coordinate.</param>
        /// <param name="yMax">The maximum Y coordinate.</param>
        public Envelope(double xMin, double yMin, double xMax, double yMax)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
        }

        /// <inheritdoc/>
        public override GeometryType Type => GeometryType.Envelope;

        /// <inheritdoc/>
        public override bool IsEmpty => double.IsNaN(XMin) || double.IsNaN(YMin) || double.IsNaN(XMax) || double.IsNaN(YMax);

        /// <inheritdoc/>
        public override int Dimension => 2;

        /// <inheritdoc/>
        public override Envelope GetEnvelope()
        {
            return this;
        }

        /// <summary>
        /// Gets the width of the envelope.
        /// </summary>
        public double Width => IsEmpty ? 0 : XMax - XMin;

        /// <summary>
        /// Gets the height of the envelope.
        /// </summary>
        public double Height => IsEmpty ? 0 : YMax - YMin;

        /// <summary>
        /// Gets the center point of the envelope.
        /// </summary>
        public Point Center => IsEmpty ? new Point() : new Point((XMin + XMax) / 2, (YMin + YMax) / 2);

        /// <summary>
        /// Gets the area of the envelope.
        /// </summary>
        public double Area => IsEmpty ? 0 : Width * Height;

        /// <summary>
        /// Determines whether this envelope contains a point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the envelope contains the point, false otherwise.</returns>
        public bool Contains(Point point)
        {
            if (point == null || point.IsEmpty || IsEmpty)
            {
                return false;
            }

            return point.X >= XMin && point.X <= XMax && point.Y >= YMin && point.Y <= YMax;
        }

        /// <summary>
        /// Determines whether this envelope intersects another envelope.
        /// </summary>
        /// <param name="other">The other envelope.</param>
        /// <returns>True if the envelopes intersect, false otherwise.</returns>
        public bool Intersects(Envelope other)
        {
            if (other == null || IsEmpty || other.IsEmpty)
            {
                return false;
            }

            return !(XMax < other.XMin || XMin > other.XMax || YMax < other.YMin || YMin > other.YMax);
        }

        /// <summary>
        /// Merges this envelope with another point.
        /// </summary>
        /// <param name="point">The point to merge.</param>
        public void Merge(Point point)
        {
            if (point == null || point.IsEmpty)
            {
                return;
            }

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
        /// Merges this envelope with another envelope.
        /// </summary>
        /// <param name="other">The envelope to merge.</param>
        public void Merge(Envelope other)
        {
            if (other == null || other.IsEmpty)
            {
                return;
            }

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
}
