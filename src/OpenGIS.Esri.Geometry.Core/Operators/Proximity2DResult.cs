using System;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     2D 邻近操作的结果。
///     包含找到的最近坐标或顶点的信息。
/// </summary>
public class Proximity2DResult
{
    private readonly Point _coordinate;
    private readonly double _distance;
    private readonly int _vertexIndex;

    /// <summary>
    ///     创建空的 Proximity2DResult。
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
    ///     使用指定的值创建 Proximity2DResult。
    /// </summary>
    /// <param name="coordinate">最近的坐标。</param>
    /// <param name="vertexIndex">顶点索引。</param>
    /// <param name="distance">到最近点的距离。</param>
    public Proximity2DResult(Point coordinate, int vertexIndex, double distance)
    {
        _coordinate = coordinate;
        _vertexIndex = vertexIndex;
        _distance = distance;
        IsRightSide = false;
        IsEmpty = false;
    }

    /// <summary>
    ///     获取此结果是否为空。
    ///     仅当传递给邻近操作符的几何对象为空时才会发生这种情况。
    /// </summary>
    public bool IsEmpty { get; }

    /// <summary>
    ///     获取 getNearestCoordinate 的最近坐标，或 getNearestVertex 和 getNearestVertices 的顶点坐标。
    /// </summary>
    /// <exception cref="InvalidOperationException">当结果为空时抛出。</exception>
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
    ///     获取顶点索引。
    ///     对于 getNearestCoordinate，行为如下：
    ///     - 当输入是多边形或包络且 bTestPolygonInterior 为 true 时，值为零。
    ///     - 当输入是多边形或包络且 bTestPolygonInterior 为 false 时，
    ///     值是具有最近坐标的线段的起始顶点索引。
    ///     - 当输入是折线时，值是具有最近坐标的线段的起始顶点索引。
    ///     - 当输入是点时，值为 0。
    ///     - 当输入是多点时，值是最近的顶点。
    /// </summary>
    /// <exception cref="InvalidOperationException">当结果为空时抛出。</exception>
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
    ///     获取到最近顶点或坐标的距离。
    /// </summary>
    /// <exception cref="InvalidOperationException">当结果为空时抛出。</exception>
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
    ///     获取最近的坐标是否在几何对象的右侧。
    ///     仅当对 MultiPath 几何对象调用邻近操作符时，bCalculateLeftRightSide 设置为 true 时，此值才有意义。
    /// </summary>
    public bool IsRightSide { get; private set; }

    /// <summary>
    ///     设置坐标是否在右侧。
    /// </summary>
    /// <param name="isRight">如果在右侧则为 true，否则为 false。</param>
    internal void SetRightSide(bool isRight)
    {
        IsRightSide = isRight;
    }
}