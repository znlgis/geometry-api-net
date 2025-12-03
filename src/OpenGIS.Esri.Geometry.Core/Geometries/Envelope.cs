using System;

namespace OpenGIS.Esri.Geometry.Core.Geometries;

/// <summary>
///     表示轴对齐的边界矩形。
/// </summary>
public class Envelope : Geometry
{
  /// <summary>
  ///     初始化 <see cref="Envelope" /> 类的新实例。
  /// </summary>
  public Envelope()
    {
        XMin = double.NaN;
        YMin = double.NaN;
        XMax = double.NaN;
        YMax = double.NaN;
    }

  /// <summary>
  ///     使用指定的边界初始化 <see cref="Envelope" /> 类的新实例。
  /// </summary>
  /// <param name="xMin">最小 X 坐标。</param>
  /// <param name="yMin">最小 Y 坐标。</param>
  /// <param name="xMax">最大 X 坐标。</param>
  /// <param name="yMax">最大 Y 坐标。</param>
  public Envelope(double xMin, double yMin, double xMax, double yMax)
    {
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
    }

  /// <summary>
  ///     获取或设置最小 X 坐标。
  /// </summary>
  public double XMin { get; set; }

  /// <summary>
  ///     获取或设置最小 Y 坐标。
  /// </summary>
  public double YMin { get; set; }

  /// <summary>
  ///     获取或设置最大 X 坐标。
  /// </summary>
  public double XMax { get; set; }

  /// <summary>
  ///     获取或设置最大 Y 坐标。
  /// </summary>
  public double YMax { get; set; }

    /// <inheritdoc />
    public override GeometryType Type => GeometryType.Envelope;

    /// <inheritdoc />
    public override bool IsEmpty => double.IsNaN(XMin) || double.IsNaN(YMin) ||
                                    double.IsNaN(XMax) || double.IsNaN(YMax);

    /// <inheritdoc />
    public override int Dimension => 2;

    /// <summary>
    ///     获取包络的宽度。
    /// </summary>
    public double Width => IsEmpty ? 0 : XMax - XMin;

    /// <summary>
    ///     获取包络的高度。
    /// </summary>
    public double Height => IsEmpty ? 0 : YMax - YMin;

    /// <summary>
    ///     获取包络的中心点。
    /// </summary>
    public Point Center => IsEmpty ? new Point() : new Point((XMin + XMax) / 2, (YMin + YMax) / 2);

    /// <summary>
    ///     获取包络的面积。
    /// </summary>
    public double Area => IsEmpty ? 0 : Width * Height;

    /// <inheritdoc />
    public override Envelope GetEnvelope()
    {
        return this;
    }

    /// <summary>
    ///     判断此包络是否包含某个点。
    /// </summary>
    /// <param name="point">要测试的点。</param>
    /// <returns>如果包络包含该点则返回 true，否则返回 false。</returns>
    public bool Contains(Point point)
    {
        if (point == null || point.IsEmpty || IsEmpty) return false;

        return point.X >= XMin && point.X <= XMax && point.Y >= YMin && point.Y <= YMax;
    }

    /// <summary>
    ///     判断此包络是否与另一个包络相交。
    /// </summary>
    /// <param name="other">另一个包络。</param>
    /// <returns>如果包络相交则返回 true，否则返回 false。</returns>
    public bool Intersects(Envelope other)
    {
        if (other == null || IsEmpty || other.IsEmpty) return false;

        return !(XMax < other.XMin || XMin > other.XMax || YMax < other.YMin || YMin > other.YMax);
    }

    /// <summary>
    ///     将此包络与另一个点合并。
    /// </summary>
    /// <param name="point">要合并的点。</param>
    public void Merge(Point point)
    {
        if (point == null || point.IsEmpty) return;

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
    ///     将此包络与另一个包络合并。
    /// </summary>
    /// <param name="other">要合并的包络。</param>
    public void Merge(Envelope other)
    {
        if (other == null || other.IsEmpty) return;

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