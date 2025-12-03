using System;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   Represents a line segment between two points.
/// </summary>
public class Line : Geometry
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="Line" /> class.
    /// </summary>
    public Line()
  {
    Start = new Point();
    End = new Point();
  }

    /// <summary>
    ///   Initializes a new instance of the <see cref="Line" /> class with specified endpoints.
    /// </summary>
    /// <param name="start">The start point.</param>
    /// <param name="end">The end point.</param>
    public Line(Point start, Point end)
  {
    Start = start ?? throw new ArgumentNullException(nameof(start));
    End = end ?? throw new ArgumentNullException(nameof(end));
  }

    /// <summary>
    ///   Gets or sets the start point of the line.
    /// </summary>
    public Point Start { get; set; }

    /// <summary>
    ///   Gets or sets the end point of the line.
    /// </summary>
    public Point End { get; set; }

    /// <inheritdoc />
    public override GeometryType Type => GeometryType.Line;

    /// <inheritdoc />
    public override bool IsEmpty => Start?.IsEmpty != false || End?.IsEmpty != false;

    /// <inheritdoc />
    public override int Dimension => 1;

    /// <summary>
    ///   Gets the length of the line.
    /// </summary>
    public double Length
  {
    get
    {
      if (IsEmpty) return 0;
      return Start.Distance(End);
    }
  }

    /// <inheritdoc />
    public override Envelope GetEnvelope()
  {
    if (IsEmpty) return new Envelope();

    var envelope = new Envelope();
    envelope.Merge(Start);
    envelope.Merge(End);
    return envelope;
  }
}