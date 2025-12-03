using System;

namespace Esri.Geometry.Core.Geometries;

/// <summary>
///   表示具有 X 和 Y 坐标的点几何对象。
/// </summary>
public class Point : Geometry
{
    /// <summary>
    ///   初始化 <see cref="Point" /> 类的新实例。
    /// </summary>
    public Point()
  {
    X = double.NaN;
    Y = double.NaN;
  }

    /// <summary>
    ///   使用指定的坐标初始化 <see cref="Point" /> 类的新实例。
    /// </summary>
    /// <param name="x">X 坐标。</param>
    /// <param name="y">Y 坐标。</param>
    public Point(double x, double y)
  {
    X = x;
    Y = y;
  }

    /// <summary>
    ///   使用指定的坐标和 Z 值初始化 <see cref="Point" /> 类的新实例。
    /// </summary>
    /// <param name="x">X 坐标。</param>
    /// <param name="y">Y 坐标。</param>
    /// <param name="z">Z 坐标。</param>
    public Point(double x, double y, double z)
  {
    X = x;
    Y = y;
    Z = z;
  }

    /// <summary>
    ///   获取或设置 X 坐标。
    /// </summary>
    public double X { get; set; }

    /// <summary>
    ///   获取或设置 Y 坐标。
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    ///   获取或设置 Z 坐标（可选）。
    /// </summary>
    public double? Z { get; set; }

    /// <summary>
    ///   获取或设置 M 值（测量值，可选）。
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
    ///   计算到另一个点的距离。
    /// </summary>
    /// <param name="other">另一个点。</param>
    /// <returns>两点之间的距离。</returns>
    public double Distance(Point other)
  {
    if (other == null) throw new ArgumentNullException(nameof(other));

    var dx = X - other.X;
    var dy = Y - other.Y;
    return Math.Sqrt(dx * dx + dy * dy);
  }

    /// <summary>
    ///   判断此点是否等于另一个点。
    /// </summary>
    /// <param name="other">另一个点。</param>
    /// <param name="tolerance">比较容差。</param>
    /// <returns>如果点在容差范围内相等则返回 true，否则返回 false。</returns>
    public bool Equals(Point other, double tolerance = GeometryConstants.DefaultTolerance)
  {
    if (other == null) return false;

    return Math.Abs(X - other.X) <= tolerance && Math.Abs(Y - other.Y) <= tolerance;
  }
}