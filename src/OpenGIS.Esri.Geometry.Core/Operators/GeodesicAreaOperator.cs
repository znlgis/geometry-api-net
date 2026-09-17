using System;
using System.Collections.Generic;
using System.Linq;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     使用球面过量公式计算 WGS84 椭球上的大地测量面积.
///     For accurate area calculations on Earth's surface.
/// </summary>
public class GeodesicAreaOperator : IGeometryOperator<double>
{
    // WGS84 ellipsoid parameters
    private const double WGS84_SEMI_MAJOR_AXIS = 6378137.0; // meters
    private const double WGS84_FLATTENING = 1.0 / 298.257223563;
    private const double WGS84_SEMI_MINOR_AXIS = WGS84_SEMI_MAJOR_AXIS * (1.0 - WGS84_FLATTENING);
    private static readonly Lazy<GeodesicAreaOperator> _instance = new(() => new GeodesicAreaOperator());

    private GeodesicAreaOperator()
    {
    }

    /// <summary>
    ///     Gets the singleton instance of the GeodesicAreaOperator.
    /// </summary>
    public static GeodesicAreaOperator Instance => _instance.Value;

    /// <inheritdoc />
    public double Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry == null || geometry.IsEmpty)
            return 0.0;

        switch (geometry)
        {
            case Polygon polygon:
                return CalculatePolygonGeodesicArea(polygon);
            case Envelope envelope:
                return CalculateEnvelopeGeodesicArea(envelope);
            default:
                // Points, lines, etc. have zero area
                return 0.0;
        }
    }

    private double CalculatePolygonGeodesicArea(Polygon polygon)
    {
        // 多部件多边形：按嵌套深度奇偶定符号（壳为正、洞为负），
        // 不能假定"第 0 环是壳、其余是洞"——MultiPolygon 部件会被误减。
        var parts = Internal.RingNesting.Group(polygon.GetRings()
            .Select(r => r.Select(pt => new[] { pt.X, pt.Y })));
        var totalArea = 0.0;
        foreach (var part in parts)
        {
            totalArea += CalculateRingGeodesicArea(toPoints(part.Shell));
            foreach (var hole in part.Holes) totalArea -= CalculateRingGeodesicArea(toPoints(hole));
        }

        return Math.Abs(totalArea);

        static System.Collections.Generic.List<Point> toPoints(System.Collections.Generic.List<double[]> ring)
            => ring.Select(c => new Point(c[0], c[1])).ToList();
    }

    private double CalculateRingGeodesicArea(IReadOnlyList<Point> ring)
    {
        if (ring.Count < 3)
            return 0.0;

        // Use spherical excess formula for geodesic area
        // This is a simplified approach using spherical approximation
        var area = 0.0;
        var n = ring.Count;

        // 模遍历闭合（环可能已被去重闭合点）；若源环自带闭合点，末边零长度无害
        for (var i = 0; i < n; i++)
        {
            var p1 = ring[i];
            var p2 = ring[(i + 1) % n];
            if (p1.X == p2.X && p1.Y == p2.Y) continue;

            var lon1 = p1.X * Math.PI / 180.0; // Convert to radians
            var lat1 = p1.Y * Math.PI / 180.0;
            var lon2 = p2.X * Math.PI / 180.0;
            var lat2 = p2.Y * Math.PI / 180.0;

            // 跨 ±180° 经度解缠：相邻点经度差按最短弧归一到 (-π, π]
            var dLon = lon2 - lon1;
            while (dLon > Math.PI) dLon -= 2 * Math.PI;
            while (dLon <= -Math.PI) dLon += 2 * Math.PI;

            // Calculate area contribution using spherical approximation
            area += dLon * (2.0 + Math.Sin(lat1) + Math.Sin(lat2));
        }

        // Convert to square meters using mean radius
        var meanRadius = (WGS84_SEMI_MAJOR_AXIS + WGS84_SEMI_MINOR_AXIS) / 2.0;
        area = Math.Abs(area * meanRadius * meanRadius / 2.0);

        return area;
    }

    private double CalculateEnvelopeGeodesicArea(Envelope envelope)
    {
        // Convert envelope to polygon and calculate
        var ring = new[]
        {
            new Point(envelope.XMin, envelope.YMin),
            new Point(envelope.XMax, envelope.YMin),
            new Point(envelope.XMax, envelope.YMax),
            new Point(envelope.XMin, envelope.YMax),
            new Point(envelope.XMin, envelope.YMin)
        };

        return CalculateRingGeodesicArea(ring);
    }
}