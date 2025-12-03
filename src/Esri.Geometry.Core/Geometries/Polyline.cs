using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.Geometry.Core.Geometries
{
    /// <summary>
    /// Represents a polyline geometry consisting of one or more paths.
    /// </summary>
    public class Polyline : Geometry
    {
        private readonly List<List<Point>> _paths;

        /// <summary>
        /// Initializes a new instance of the <see cref="Polyline"/> class.
        /// </summary>
        public Polyline()
        {
            _paths = new List<List<Point>>();
        }

        /// <inheritdoc/>
        public override GeometryType Type => GeometryType.Polyline;

        /// <inheritdoc/>
        public override bool IsEmpty => _paths.Count == 0 || _paths.All(p => p.Count == 0);

        /// <inheritdoc/>
        public override int Dimension => 1;

        /// <summary>
        /// Gets the number of paths in the polyline.
        /// </summary>
        public int PathCount => _paths.Count;

        /// <summary>
        /// Adds a new path to the polyline.
        /// </summary>
        /// <param name="points">The points that make up the path.</param>
        public void AddPath(IEnumerable<Point> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }
            _paths.Add(points.ToList());
        }

        /// <summary>
        /// Gets the path at the specified index.
        /// </summary>
        /// <param name="index">The index of the path.</param>
        /// <returns>The path at the specified index.</returns>
        public IReadOnlyList<Point> GetPath(int index)
        {
            if (index < 0 || index >= _paths.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _paths[index].AsReadOnly();
        }

        /// <summary>
        /// Gets all paths in the polyline.
        /// </summary>
        /// <returns>An enumerable collection of paths.</returns>
        public IEnumerable<IReadOnlyList<Point>> GetPaths()
        {
            return _paths.Select(p => p.AsReadOnly());
        }

        /// <summary>
        /// Calculates the total length of all paths in the polyline.
        /// </summary>
        public double Length
        {
            get
            {
                double length = 0;
                foreach (var path in _paths)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        length += path[i].Distance(path[i + 1]);
                    }
                }
                return length;
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
            foreach (var path in _paths)
            {
                foreach (var point in path)
                {
                    envelope.Merge(point);
                }
            }
            return envelope;
        }
    }
}
