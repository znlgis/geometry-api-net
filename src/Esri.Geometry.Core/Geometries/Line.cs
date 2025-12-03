using System;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   表示两点之间的线段。
/// </summary>
public class Line : Geometry
{
    /// <summary>
    ///   初始化 <see cref="Line" /> 类的新实例。
    /// </summary>
    public Line()
  {
    Start = new Point();
    End = new Point();
  }

    /// <summary>
    ///   使用指定的端点初始化 <see cref="Line" /> 类的新实例。
    /// </summary>
    /// <param name="start">起点。</param>
    /// <param name="end">终点。</param>
    public Line(Point start, Point end)
  {
    Start = start ?? throw new ArgumentNullException(nameof(start));
    End = end ?? throw new ArgumentNullException(nameof(end));
  }

    /// <summary>
    ///   获取或设置线段的起点。
    /// </summary>
    public Point Start { get; set; }

    /// <summary>
    ///   获取或设置线段的终点。
    /// </summary>
    public Point End { get; set; }

    /// <inheritdoc />
    public override GeometryType Type => GeometryType.Line;

    /// <inheritdoc />
    public override bool IsEmpty => Start?.IsEmpty != false || End?.IsEmpty != false;

    /// <inheritdoc />
    public override int Dimension => 1;

    /// <summary>
    ///   获取线段的长度。
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