using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace GeoHarness;

/// <summary>被测库 internal 关系引擎桥（InternalsVisibleTo 已开放给本 harness 程序集）。</summary>
internal static class RelateBridge
{
    public static RelateMatrix Once(Geometry a, Geometry b, out ShapeModel ma, out ShapeModel mb)
        => RelateOps.RelateOnce(a, b, out ma, out mb);

    public static double Distance(Geometry a, Geometry b) => RelateOps.DistanceGeom(a, b);
}
