using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.Geometry.Core.Geometries
{
    /// <summary>
    /// Represents a polygon geometry consisting of one or more rings.
    /// </summary>
    public class Polygon : Geometry
    {
        private readonly List<List<Point>> _rings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        public Polygon()
        {
            _rings = new List<List<Point>>();
        }

        /// <inheritdoc/>
        public override GeometryType Type => GeometryType.Polygon;

        /// <inheritdoc/>
        public override bool IsEmpty => _rings.Count == 0 || _rings.All(r => r.Count == 0);

        /// <inheritdoc/>
        public override int Dimension => 2;

        /// <summary>
        /// Gets the number of rings in the polygon.
        /// </summary>
        public int RingCount => _rings.Count;

        /// <summary>
        /// Adds a new ring to the polygon.
        /// </summary>
        /// <param name="points">The points that make up the ring.</param>
        public void AddRing(IEnumerable<Point> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }
            _rings.Add(points.ToList());
        }

        /// <summary>
        /// Gets the ring at the specified index.
        /// </summary>
        /// <param name="index">The index of the ring.</param>
        /// <returns>The ring at the specified index.</returns>
        public IReadOnlyList<Point> GetRing(int index)
        {
            if (index < 0 || index >= _rings.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _rings[index].AsReadOnly();
        }

        /// <summary>
        /// Gets all rings in the polygon.
        /// </summary>
        /// <returns>An enumerable collection of rings.</returns>
        public IEnumerable<IReadOnlyList<Point>> GetRings()
        {
            return _rings.Select(r => r.AsReadOnly());
        }

        /// <summary>
        /// Calculates the area of the polygon using the shoelace formula.
        /// Note: This is a simple implementation that works for simple polygons.
        /// </summary>
        public double Area
        {
            get
            {
                double area = 0;
                foreach (var ring in _rings)
                {
                    if (ring.Count < 3)
                    {
                        continue;
                    }

                    double ringArea = 0;
                    for (int i = 0; i < ring.Count - 1; i++)
                    {
                        ringArea += ring[i].X * ring[i + 1].Y - ring[i + 1].X * ring[i].Y;
                    }
                    // Close the ring
                    ringArea += ring[ring.Count - 1].X * ring[0].Y - ring[0].X * ring[ring.Count - 1].Y;
                    area += Math.Abs(ringArea) / 2.0;
                }
                return area;
            }
        }

        /// <inheritdoc/>
        public override Envelope GetEnvelope()
        {
            if (IsEmpty)
            {
                return new Envelope();
            }

            var envelope = new Envelope();
            foreach (var ring in _rings)
            {
                foreach (var point in ring)
                {
                    envelope.Merge(point);
                }
            }
            return envelope;
        }
    }
}
