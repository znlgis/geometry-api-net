using System;
using System.Collections.Generic;
using OpenGIS.Esri.Geometry.Core.Operators;

namespace OpenGIS.Esri.Geometry.Core.Geometries;

/// <summary>
///     所有几何类型的抽象基类。
/// </summary>
public abstract class Geometry
{
  /// <summary>
  ///     获取几何对象的类型。
  /// </summary>
  public abstract GeometryType Type { get; }

  /// <summary>
  ///     获取一个值，指示此几何对象是否为空。
  /// </summary>
  public abstract bool IsEmpty { get; }

  /// <summary>
  ///     获取几何对象的维度。
  ///     点为 0，线为 1，多边形为 2。
  /// </summary>
  public abstract int Dimension { get; }

  /// <summary>
  ///     获取一个值，指示此几何对象是否为点类型。
  /// </summary>
  public bool IsPoint => Type == GeometryType.Point || Type == GeometryType.MultiPoint;

  /// <summary>
  ///     获取一个值，指示此几何对象是否为线类型。
  /// </summary>
  public bool IsLinear => Type == GeometryType.Line || Type == GeometryType.Polyline;

  /// <summary>
  ///     获取一个值，指示此几何对象是否为面类型。
  /// </summary>
  public bool IsArea => Type == GeometryType.Polygon || Type == GeometryType.Envelope;

  /// <summary>
  ///     获取此几何对象的包络（边界矩形）。
  /// </summary>
  /// <returns>表示边界矩形的 Envelope 对象。</returns>
  public abstract Envelope GetEnvelope();

  /// <summary>
  ///     计算几何对象的 2D 面积。
  ///     对于非面类型几何对象（点、线）返回 0。
  /// </summary>
  public virtual double CalculateArea2D()
    {
        if (this is Polygon polygon)
            return AreaOperator.Instance.Execute(polygon);

        if (this is Envelope envelope)
            return envelope.Width * envelope.Height;

        return 0;
    }

  /// <summary>
  ///     计算几何对象的 2D 长度或周长。
  ///     对于点几何对象返回 0。
  /// </summary>
  public virtual double CalculateLength2D()
    {
        if (this is Polyline polyline)
            return LengthOperator.Instance.Execute(polyline);

        if (this is Polygon polygon)
            return LengthOperator.Instance.Execute(polygon);

        if (this is Line line)
            return line.Length;

        if (this is Envelope envelope)
            return 2 * (envelope.Width + envelope.Height);

        return 0;
    }

  /// <summary>
  ///     创建此几何对象的深层副本。
  /// </summary>
  public virtual Geometry Copy()
    {
        // 默认实现 - 子类可以重写以获得更好的性能
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
                    var pathCopy = new List<Point>();
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
                    var ringCopy = new List<Point>();
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
                throw new NotSupportedException($"Copy not supported for geometry type {Type}");
        }
    }

  /// <summary>
  ///     检查几何对象是否有效（不为 null 且不为空）。
  /// </summary>
  public virtual bool IsValid()
    {
        return this != null && !IsEmpty;
    }
}