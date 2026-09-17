namespace OpenGIS.Esri.Geometry.Core.Internal;

// 包络 → 合法线性环：简单多边形要求边界自交点处绕数恒为 ±1（双绕自切非法）。
// 五点矩形环并入 MULTIPOLYGON 环列表时会与相邻部件形成 winding=2 非法体；
// 带缝矩形以 (xmin,ymin) 为缝点：底→右→顶→缝上行→下行→左，逐点各出现两次、
// 从内部任一点出发的射线与环相交恰一次（winding 恒 ±1），且几何仍精确覆盖矩形。
internal static class EnvelopeUtil
{
    public static double[][] SlitRing(double xmin, double ymin, double xmax, double ymax) =>
        new[]
        {
            new[] { xmin, ymin }, new[] { xmax, ymin }, new[] { xmax, ymax },
            new[] { xmin, ymax }, new[] { xmin, ymin },
            new[] { xmin, ymax }, new[] { xmin, ymin },
        };
}
