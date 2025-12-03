using System;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Core.Operators;

/// <summary>
///   Result of a 2D proximity operation.
///   Contains information about the nearest coordinate or vertex found.
/// </summary>
public class Proximity2DResult
{
  private readonly Point _coordinate;
  private readonly double _distance;
  private readonly int _vertexIndex;

  /// <summary>
  ///   Creates an empty Proximity2DResult.
  /// </summary>
  public Proximity2DResult()
  {
    _coordinate = new Point();
    _vertexIndex = -1;
    _distance = 0;
    IsRightSide = false;
    IsEmpty = true;
  }

  /// <summary>
  ///   Creates a Proximity2DResult with the specified values.
  /// </summary>
  /// <param name="coordinate">The nearest coordinate.</param>
  /// <param name="vertexIndex">The vertex index.</param>
  /// <param name="distance">The distance to the nearest point.</param>
  public Proximity2DResult(Point coordinate, int vertexIndex, double distance)
  {
    _coordinate = coordinate;
    _vertexIndex = vertexIndex;
    _distance = distance;
    IsRightSide = false;
    IsEmpty = false;
  }

  /// <summary>
  ///   Gets whether this result is empty.
  ///   This only happens if the geometry passed to the proximity operator is empty.
  /// </summary>
  public bool IsEmpty { get; }

  /// <summary>
  ///   Gets the closest coordinate for getNearestCoordinate or the vertex coordinates
  ///   for getNearestVertex and getNearestVertices.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown when the result is empty.</exception>
  public Point Coordinate
  {
    get
    {
      if (IsEmpty)
        throw new InvalidOperationException("Cannot get coordinate from empty Proximity2DResult");
      return _coordinate;
    }
  }

  /// <summary>
  ///   Gets the vertex index.
  ///   For getNearestCoordinate the behavior is:
  ///   - When the input is a polygon or envelope and bTestPolygonInterior is true, the value is zero.
  ///   - When the input is a polygon or envelope and bTestPolygonInterior is false,
  ///   the value is the start vertex index of a segment with the closest coordinate.
  ///   - When the input is a polyline, the value is the start vertex index of a segment with the closest coordinate.
  ///   - When the input is a point, the value is 0.
  ///   - When the input is a multipoint, the value is the closest vertex.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown when the result is empty.</exception>
  public int VertexIndex
  {
    get
    {
      if (IsEmpty)
        throw new InvalidOperationException("Cannot get vertex index from empty Proximity2DResult");
      return _vertexIndex;
    }
  }

  /// <summary>
  ///   Gets the distance to the closest vertex or coordinate.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown when the result is empty.</exception>
  public double Distance
  {
    get
    {
      if (IsEmpty)
        throw new InvalidOperationException("Cannot get distance from empty Proximity2DResult");
      return _distance;
    }
  }

  /// <summary>
  ///   Gets whether the closest coordinate is to the right of the geometry.
  ///   This is only meaningful when the proximity operator was called with
  ///   bCalculateLeftRightSide set to true for MultiPath geometries.
  /// </summary>
  public bool IsRightSide { get; private set; }

  /// <summary>
  ///   Sets whether the coordinate is on the right side.
  /// </summary>
  /// <param name="isRight">True if on the right side, false otherwise.</param>
  internal void SetRightSide(bool isRight)
  {
    IsRightSide = isRight;
  }
}