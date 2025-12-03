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

        /// <summary>
        /// Calculates the 2D area of the geometry.
        /// Returns 0 for non-area geometries (points, lines).
        /// </summary>
        public virtual double CalculateArea2D()
        {
            if (this is Polygon polygon)
                return Operators.AreaOperator.Instance.Execute(polygon);
            
            if (this is Envelope envelope)
                return envelope.Width * envelope.Height;
            
            return 0;
        }

        /// <summary>
        /// Calculates the 2D length or perimeter of the geometry.
        /// Returns 0 for point geometries.
        /// </summary>
        public virtual double CalculateLength2D()
        {
            if (this is Polyline polyline)
                return Operators.LengthOperator.Instance.Execute(polyline);
            
            if (this is Polygon polygon)
                return Operators.LengthOperator.Instance.Execute(polygon);
            
            if (this is Line line)
                return line.Length;
            
            if (this is Envelope envelope)
                return 2 * (envelope.Width + envelope.Height);
            
            return 0;
        }

        /// <summary>
        /// Creates a deep copy of this geometry.
        /// </summary>
        public virtual Geometry Copy()
        {
            // Default implementation - subclasses can override for better performance
            switch (this)
            {
                case Point point:
                    {
                        var copy = new Point(point.X, point.Y);
                        if (point.Z.HasValue)
                            copy.Z = point.Z;
                        if (point.M.HasValue)
                            copy.M = point.M;
                        return copy;
                    }
                
                case Envelope envelope:
                    return new Envelope(envelope.XMin, envelope.YMin, envelope.XMax, envelope.YMax);
                
                case Line line:
                    return new Line(
                        new Point(line.Start.X, line.Start.Y), 
                        new Point(line.End.X, line.End.Y));
                
                case MultiPoint multiPoint:
                    {
                        var copy = new MultiPoint();
                        foreach (var pt in multiPoint.GetPoints())
                        {
                            var ptCopy = new Point(pt.X, pt.Y);
                            if (pt.Z.HasValue)
                                ptCopy.Z = pt.Z;
                            if (pt.M.HasValue)
                                ptCopy.M = pt.M;
                            copy.Add(ptCopy);
                        }
                        return copy;
                    }
                
                case Polyline polyline:
                    {
                        var copy = new Polyline();
                        foreach (var path in polyline.GetPaths())
                        {
                            var pathCopy = new System.Collections.Generic.List<Point>();
                            foreach (var pt in path)
                            {
                                var ptCopy = new Point(pt.X, pt.Y);
                                if (pt.Z.HasValue)
                                    ptCopy.Z = pt.Z;
                                if (pt.M.HasValue)
                                    ptCopy.M = pt.M;
                                pathCopy.Add(ptCopy);
                            }
                            copy.AddPath(pathCopy);
                        }
                        return copy;
                    }
                
                case Polygon polygon:
                    {
                        var copy = new Polygon();
                        foreach (var ring in polygon.GetRings())
                        {
                            var ringCopy = new System.Collections.Generic.List<Point>();
                            foreach (var pt in ring)
                            {
                                var ptCopy = new Point(pt.X, pt.Y);
                                if (pt.Z.HasValue)
                                    ptCopy.Z = pt.Z;
                                if (pt.M.HasValue)
                                    ptCopy.M = pt.M;
                                ringCopy.Add(ptCopy);
                            }
                            copy.AddRing(ringCopy);
                        }
                        return copy;
                    }
                
                default:
                    throw new System.NotSupportedException($"Copy not supported for geometry type {this.Type}");
            }
        }

        /// <summary>
        /// Checks if the geometry is valid (not null and not empty).
        /// </summary>
        public virtual bool IsValid()
        {
            return this != null && !IsEmpty;
        }

        /// <summary>
        /// Gets a value indicating whether this geometry represents a point type.
        /// </summary>
        public bool IsPoint => Type == GeometryType.Point || Type == GeometryType.MultiPoint;

        /// <summary>
        /// Gets a value indicating whether this geometry represents a linear type.
        /// </summary>
        public bool IsLinear => Type == GeometryType.Line || Type == GeometryType.Polyline;

        /// <summary>
        /// Gets a value indicating whether this geometry represents an area type.
        /// </summary>
        public bool IsArea => Type == GeometryType.Polygon || Type == GeometryType.Envelope;
    }
}
