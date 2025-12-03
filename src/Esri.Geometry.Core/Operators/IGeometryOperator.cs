namespace Esri.Geometry.Core.Operators;

/// <summary>
///   几何操作符的接口。
/// </summary>
/// <typeparam name="TResult">操作的结果类型。</typeparam>
public interface IGeometryOperator<TResult>
{
    /// <summary>
    ///   对几何对象执行操作。
    /// </summary>
    /// <param name="geometry">要操作的几何对象。</param>
    /// <param name="spatialRef">空间参考（可选）。</param>
    /// <returns>操作的结果。</returns>
    TResult Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null);
}

/// <summary>
///   二元几何操作符的接口。
/// </summary>
/// <typeparam name="TResult">操作的结果类型。</typeparam>
public interface IBinaryGeometryOperator<TResult>
{
    /// <summary>
    ///   对两个几何对象执行操作。
    /// </summary>
    /// <param name="geometry1">第一个几何对象。</param>
    /// <param name="geometry2">第二个几何对象。</param>
    /// <param name="spatialRef">空间参考（可选）。</param>
    /// <returns>操作的结果。</returns>
    TResult Execute(Geometries.Geometry geometry1, Geometries.Geometry geometry2,
    SpatialReference.SpatialReference? spatialRef = null);
}