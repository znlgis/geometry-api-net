namespace OpenGIS.Esri.Geometry.Core.Internal;

/// <summary>netstandard2.0 缺失的 Clamp 等数值小工具。</summary>
internal static class Nums
{
    public static double ClampD(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
    public static int ClampD(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
}
