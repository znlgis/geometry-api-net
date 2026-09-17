using System;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     OGC Simple Feature Access（1.2.1 / 06-103r4）语义下的几何合法化与简单性判定。
///     SimplifyOGC = makeValid：多边形在自交点处按非零绕数重组（消除自交/重叠），
///     折线在自交点断开重排，多点去重；IsSimpleOGC 为对应的简单性检测。
/// </summary>
public class SimplifyOGCOperator : IGeometryOperator<Geometries.Geometry>
{
    private static readonly Lazy<SimplifyOGCOperator> _instance = new(() => new SimplifyOGCOperator());

    private SimplifyOGCOperator()
    {
    }

    /// <summary>
    ///     获取 SimplifyOGCOperator 的单例实例。
    /// </summary>
    public static SimplifyOGCOperator Instance => _instance.Value;

    /// <summary>
    ///     将几何对象合法化为 OGC 简单几何（视觉上等效）。
    /// </summary>
    /// <param name="geometry">要处理的几何对象。</param>
    /// <param name="spatialRef">空间参考（保留参数；当前实现为平面语义）。</param>
    public Geometries.Geometry Execute(Geometries.Geometry geometry,
        SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));
        return MakeValidOps.Execute(geometry, spatialRef);
    }

    /// <summary>
    ///     forceSimplify 参数保留以兼容旧签名；makeValid 语义下行为一致。
    /// </summary>
    public Geometries.Geometry Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef,
        bool forceSimplify)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));
        return MakeValidOps.Execute(geometry, spatialRef);
    }

    /// <summary>
    ///     按 OGC 规范测试几何对象是否简单（无自交、无重叠、点不重复）。
    /// </summary>
    public bool IsSimpleOGC(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry == null) return false;
        if (geometry.IsEmpty) return true;
        return MakeValidOps.IsSimple(geometry);
    }
}
