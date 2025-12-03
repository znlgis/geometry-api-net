using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   Represents a collection of points.
/// </summary>
public class MultiPoint : Geometry
{
  private readonly List<Point> _points;

  /// <summary>
  ///   Initializes a new instance of the <see cref="MultiPoint" /> class.
  /// </summary>
  public MultiPoint()
  {
    _points = new List<Point>();
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="MultiPoint" /> class with specified points.
  /// </summary>
  /// <param name="points">The collection of points.</param>
  public MultiPoint(IEnumerable<Point> points)
  {
    _points = points?.ToList() ?? new List<Point>();
  }

  /// <inheritdoc />
  public override GeometryType Type => GeometryType.MultiPoint;

  /// <inheritdoc />
  public override bool IsEmpty => _points.Count == 0;

  /// <inheritdoc />
  public override int Dimension => 0;

  /// <summary>
  ///   Gets the number of points in the multi-point.
  /// </summary>
  public int Count => _points.Count;

  /// <summary>
  ///   Gets the point at the specified index.
  /// </summary>
  /// <param name="index">The index of the point.</param>
  /// <returns>The point at the specified index.</returns>
  public Point GetPoint(int index)
  {
    if (index < 0 || index >= _points.Count) throw new ArgumentOutOfRangeException(nameof(index));
    return _points[index];
  }

  /// <summary>
  ///   Adds a point to the multi-point.
  /// </summary>
  /// <param name="point">The point to add.</param>
  public void Add(Point point)
  {
    if (point == null) throw new ArgumentNullException(nameof(point));
    _points.Add(point);
  }

  /// <summary>
  ///   Gets all points in the multi-point.
  /// </summary>
  /// <returns>An enumerable collection of points.</returns>
  public IEnumerable<Point> GetPoints()
  {
    return _points;
  }

  /// <inheritdoc />
  public override Envelope GetEnvelope()
  {
    if (IsEmpty) return new Envelope();

    var envelope = new Envelope();
    foreach (var point in _points) envelope.Merge(point);
    return envelope;
  }
}