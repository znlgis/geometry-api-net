using System;
using System.Collections.Generic;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Operator for clipping geometries to an envelope using the Cohen-Sutherland algorithm.
///   Clipping removes geometry portions outside the specified envelope boundary.
/// </summary>
/// <remarks>
///   Implements the Cohen-Sutherland line clipping algorithm for efficient line segment clipping:
///   1. Assigns region codes to endpoints (inside/left/right/top/bottom)
///   2. Trivially accepts/rejects lines based on region codes
///   3. Iteratively clips lines at envelope boundaries
///   
///   Supported geometry types:
///   - Point: Returns point if inside, empty otherwise
///   - MultiPoint: Returns only points inside the envelope
///   - Envelope: Returns intersection of envelopes
///   - Line: Returns clipped line segment(s)
///   - Polyline: Returns clipped paths
///   - Polygon: Not yet implemented (requires Sutherland-Hodgman algorithm)
///   
///   Time Complexity: O(n) where n is the number of line segments
///   Space Complexity: O(n) for storing clipped geometry
/// </remarks>
public class ClipOperator : IGeometryOperator<Geometries.Geometry>
{
  private static readonly Lazy<ClipOperator> _instance = new(() => new ClipOperator());

  private ClipOperator()
  {
  }

  /// <summary>
  ///   Gets the singleton instance of the clip operator.
  /// </summary>
  public static ClipOperator Instance => _instance.Value;

  /// <inheritdoc />
  public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
  {
    throw new NotImplementedException(
      "Clip operator requires an envelope parameter. Use Execute(geometry, clipEnvelope, spatialRef) instead.");
  }

  /// <summary>
  ///   Clips a geometry to the specified envelope, removing portions outside the boundary.
  /// </summary>
  /// <param name="geometry">The geometry to clip. Supports Point, MultiPoint, Envelope, Line, and Polyline.</param>
  /// <param name="clipEnvelope">The envelope defining the clipping boundary.</param>
  /// <param name="spatialRef">Optional spatial reference (currently not used).</param>
  /// <returns>
  ///   The clipped geometry. Returns:
  ///   - Original geometry if completely inside the envelope
  ///   - Empty geometry if completely outside the envelope
  ///   - Clipped geometry for partial intersections
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when geometry or clipEnvelope is null.</exception>
  /// <exception cref="NotImplementedException">Thrown for unsupported geometry types (Polygon).</exception>
  /// <example>
  ///   <code>
  ///   var line = new Line(new Point(-10, 0), new Point(10, 0));
  ///   var clipEnv = new Envelope(0, -5, 5, 5);
  ///   var clipped = ClipOperator.Instance.Execute(line, clipEnv);
  ///   // Result: Line from (0, 0) to (5, 0)
  ///   </code>
  /// </example>
  public Geometries.Geometry Execute(Geometries.Geometry geometry, Envelope clipEnvelope,
    SpatialReference.SpatialReference? spatialRef = null)
  {
    if (geometry == null) throw new ArgumentNullException(nameof(geometry));

    if (clipEnvelope == null) throw new ArgumentNullException(nameof(clipEnvelope));

    if (geometry.IsEmpty || clipEnvelope.IsEmpty) return CreateEmptyGeometry(geometry.Type);

    // Get the geometry's envelope
    var geomEnv = geometry.GetEnvelope();

    // If geometry doesn't intersect clip envelope, return empty
    if (!geomEnv.Intersects(clipEnvelope)) return CreateEmptyGeometry(geometry.Type);

    // If geometry is completely within clip envelope, return original
    if (geomEnv.XMin >= clipEnvelope.XMin && geomEnv.XMax <= clipEnvelope.XMax &&
        geomEnv.YMin >= clipEnvelope.YMin && geomEnv.YMax <= clipEnvelope.YMax)
      return geometry;

    // Perform clipping based on geometry type
    if (geometry is Point point) return ClipPoint(point, clipEnvelope);

    if (geometry is MultiPoint multiPoint) return ClipMultiPoint(multiPoint, clipEnvelope);

    if (geometry is Envelope envelope) return ClipEnvelope(envelope, clipEnvelope);

    if (geometry is Line line) return ClipLine(line, clipEnvelope);

    if (geometry is Polyline polyline) return ClipPolyline(polyline, clipEnvelope);

    // For polygon, this would require complex polygon clipping (Sutherland-Hodgman or similar)
    throw new NotImplementedException($"Clip operation for {geometry.Type} is not yet implemented.");
  }

  private Geometries.Geometry CreateEmptyGeometry(GeometryType type)
  {
    return type switch
    {
      GeometryType.Point => new Point(),
      GeometryType.MultiPoint => new MultiPoint(),
      GeometryType.Line => new Line(new Point(), new Point()),
      GeometryType.Polyline => new Polyline(),
      GeometryType.Polygon => new Polygon(),
      GeometryType.Envelope => new Envelope(),
      _ => throw new NotSupportedException($"Unknown geometry type: {type}")
    };
  }

  private Point ClipPoint(Point point, Envelope clipEnvelope)
  {
    if (clipEnvelope.Contains(point))
    {
      if (point.Z.HasValue) return new Point(point.X, point.Y, point.Z.Value);
      return new Point(point.X, point.Y);
    }

    return new Point();
  }

  private MultiPoint ClipMultiPoint(MultiPoint multiPoint, Envelope clipEnvelope)
  {
    var clipped = new MultiPoint();
    foreach (var point in multiPoint.GetPoints())
      if (clipEnvelope.Contains(point))
      {
        if (point.Z.HasValue)
          clipped.Add(new Point(point.X, point.Y, point.Z.Value));
        else
          clipped.Add(new Point(point.X, point.Y));
      }

    return clipped;
  }

  private Envelope ClipEnvelope(Envelope envelope, Envelope clipEnvelope)
  {
    var xMin = Math.Max(envelope.XMin, clipEnvelope.XMin);
    var yMin = Math.Max(envelope.YMin, clipEnvelope.YMin);
    var xMax = Math.Min(envelope.XMax, clipEnvelope.XMax);
    var yMax = Math.Min(envelope.YMax, clipEnvelope.YMax);

    if (xMin > xMax || yMin > yMax) return new Envelope();

    return new Envelope(xMin, yMin, xMax, yMax);
  }

  private Geometries.Geometry ClipLine(Line line, Envelope clipEnvelope)
  {
    // Cohen-Sutherland line clipping algorithm
    var clipped = CohenSutherlandClip(line.Start, line.End, clipEnvelope);

    if (clipped == null || clipped.Count == 0) return new Polyline();

    if (clipped.Count == 2) return new Line(clipped[0], clipped[1]);

    // Return as polyline if multiple segments
    var polyline = new Polyline();
    polyline.AddPath(clipped);
    return polyline;
  }

  private Polyline ClipPolyline(Polyline polyline, Envelope clipEnvelope)
  {
    var result = new Polyline();

    foreach (var path in polyline.GetPaths())
    {
      var clippedSegments = new List<Point>();

      for (var i = 0; i < path.Count - 1; i++)
      {
        var segment = CohenSutherlandClip(path[i], path[i + 1], clipEnvelope);

        if (segment != null && segment.Count >= 2)
        {
          if (clippedSegments.Count > 0 &&
              clippedSegments[clippedSegments.Count - 1].Equals(segment[0]))
          {
            // Connect to previous segment
            clippedSegments.Add(segment[1]);
          }
          else
          {
            // Start new path if we have accumulated points
            if (clippedSegments.Count >= 2)
            {
              result.AddPath(clippedSegments);
              clippedSegments = new List<Point>();
            }

            clippedSegments.AddRange(segment);
          }
        }
      }

      if (clippedSegments.Count >= 2) result.AddPath(clippedSegments);
    }

    return result;
  }

  /// <summary>
  ///   Implements the Cohen-Sutherland line clipping algorithm.
  ///   This algorithm efficiently clips line segments to a rectangular envelope.
  /// </summary>
  /// <param name="p1">Start point of the line segment.</param>
  /// <param name="p2">End point of the line segment.</param>
  /// <param name="env">The clipping envelope.</param>
  /// <returns>
  ///   A list of 2 points representing the clipped line segment if it intersects the envelope,
  ///   null if the line is completely outside the envelope.
  /// </returns>
  /// <remarks>
  ///   Algorithm steps:
  ///   1. Compute region codes for both endpoints (9-region division)
  ///   2. If both points are inside (code 0), accept the line
  ///   3. If both points share an outside region, reject the line
  ///   4. Otherwise, clip at envelope boundary and repeat with new endpoint
  /// </remarks>
  private List<Point>? CohenSutherlandClip(Point p1, Point p2, Envelope env)
  {
    // Region codes for Cohen-Sutherland algorithm
    const int INSIDE = 0; // 0000 - inside the envelope
    const int LEFT = 1;   // 0001 - left of envelope
    const int RIGHT = 2;  // 0010 - right of envelope
    const int BOTTOM = 4; // 0100 - below envelope
    const int TOP = 8;    // 1000 - above envelope

    // Compute region code for a point relative to the envelope
    int ComputeOutCode(double x, double y)
    {
      var code = INSIDE;
      if (x < env.XMin) code |= LEFT;
      else if (x > env.XMax) code |= RIGHT;
      if (y < env.YMin) code |= BOTTOM;
      else if (y > env.YMax) code |= TOP;
      return code;
    }

    double x1 = p1.X, y1 = p1.Y;
    double x2 = p2.X, y2 = p2.Y;

    var outcode1 = ComputeOutCode(x1, y1);
    var outcode2 = ComputeOutCode(x2, y2);
    var accept = false;

    while (true)
    {
      if ((outcode1 | outcode2) == 0)
      {
        // Both points inside the envelope - accept entire line
        accept = true;
        break;
      }

      if ((outcode1 & outcode2) != 0)
        // Both points share an outside region - line is completely outside
        break;

      // Line crosses boundary - clip at intersection point
      double x = 0, y = 0;
      var outcodeOut = outcode1 != 0 ? outcode1 : outcode2;

      // Find intersection point with envelope boundary
      if ((outcodeOut & TOP) != 0)
      {
        // Intersection with top boundary
        x = x1 + (x2 - x1) * (env.YMax - y1) / (y2 - y1);
        y = env.YMax;
      }
      else if ((outcodeOut & BOTTOM) != 0)
      {
        // Intersection with bottom boundary
        x = x1 + (x2 - x1) * (env.YMin - y1) / (y2 - y1);
        y = env.YMin;
      }
      else if ((outcodeOut & RIGHT) != 0)
      {
        // Intersection with right boundary
        y = y1 + (y2 - y1) * (env.XMax - x1) / (x2 - x1);
        x = env.XMax;
      }
      else if ((outcodeOut & LEFT) != 0)
      {
        // Intersection with left boundary
        y = y1 + (y2 - y1) * (env.XMin - x1) / (x2 - x1);
        x = env.XMin;
      }

      // Replace outside point with intersection point
      if (outcodeOut == outcode1)
      {
        x1 = x;
        y1 = y;
        outcode1 = ComputeOutCode(x1, y1);
      }
      else
      {
        x2 = x;
        y2 = y;
        outcode2 = ComputeOutCode(x2, y2);
      }
    }

    if (accept) return new List<Point> { new(x1, y1), new(x2, y2) };

    return null;
  }
}