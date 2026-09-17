using System;
using System.Collections.Generic;
using System.Linq;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.Internal;

namespace OpenGIS.Esri.Geometry.Core.Operators;

/// <summary>
///     用于计算几何对象质心（质量中心）的操作符.
/// </summary>
public class CentroidOperator : IGeometryOperator<Point>
{
    private static readonly Lazy<CentroidOperator> _instance = new(() => new CentroidOperator());

    private CentroidOperator()
    {
    }

    /// <summary>
    ///     获取 CentroidOperator 的单例实例.
    /// </summary>
    public static CentroidOperator Instance => _instance.Value;

    /// <inheritdoc />
    public Point Execute(Geometries.Geometry geometry, SpatialReference.SpatialReference? spatialRef = null)
    {
        if (geometry == null) throw new ArgumentNullException(nameof(geometry));

        if (geometry.IsEmpty) return new Point();

        // Point centroid is itself
        if (geometry is Point point) return new Point(point.X, point.Y);

        // MultiPoint centroid is average of all points
        if (geometry is MultiPoint multiPoint) return CalculateMultiPointCentroid(multiPoint);

        // Envelope centroid is its center
        if (geometry is Envelope envelope) return envelope.Center;

        // Line centroid is midpoint
        if (geometry is Line line)
            return new Point(
                (line.Start.X + line.End.X) / 2,
                (line.Start.Y + line.End.Y) / 2
            );

        // Polyline centroid is weighted average by segment length
        if (geometry is Polyline polyline) return CalculatePolylineCentroid(polyline);

        // Polygon centroid is area-weighted centroid
        if (geometry is Polygon polygon) return CalculatePolygonCentroid(polygon);

        throw new NotSupportedException($"Centroid calculation for {geometry.Type} is not yet implemented.");
    }

    private Point CalculateMultiPointCentroid(MultiPoint multiPoint)
    {
        var points = multiPoint.GetPoints().ToList();
        if (points.Count == 0) return new Point();

        double sumX = 0;
        double sumY = 0;
        foreach (var point in points)
        {
            sumX += point.X;
            sumY += point.Y;
        }

        return new Point(sumX / points.Count, sumY / points.Count);
    }

    private Point CalculatePolylineCentroid(Polyline polyline)
    {
        double totalLength = 0;
        double weightedX = 0;
        double weightedY = 0;

        foreach (var path in polyline.GetPaths())
            for (var i = 0; i < path.Count - 1; i++)
            {
                var p1 = path[i];
                var p2 = path[i + 1];
                var segmentLength = p1.Distance(p2);

                if (segmentLength > 0)
                {
                    var midX = (p1.X + p2.X) / 2;
                    var midY = (p1.Y + p2.Y) / 2;

                    weightedX += midX * segmentLength;
                    weightedY += midY * segmentLength;
                    totalLength += segmentLength;
                }
            }

        if (totalLength == 0) return new Point();

        return new Point(weightedX / totalLength, weightedY / totalLength);
    }

    private Point CalculatePolygonCentroid(Polygon polygon)
    {
        // 多部件/带洞质心：环按嵌套树归一方向（壳 CCW、洞 CW）后做带符号面积积分求和，
        // 洞以负面积参与加权 —— 与 GEOS ST_Centroid 语义一致。
        if (polygon.RingCount == 0) return new Point();

        var rings = new List<PolygonClipper.Ring>();
        foreach (var ring in polygon.GetRings())
        {
            var pts = ring.Select(p => new[] { p.X, p.Y }).ToList();
            if (pts.Count >= 2 && pts[0][0] == pts[pts.Count - 1][0] && pts[0][1] == pts[pts.Count - 1][1])
                pts.RemoveAt(pts.Count - 1);
            if (pts.Count >= 3)
                rings.Add(new PolygonClipper.Ring(pts));
        }

        if (rings.Count == 0) return new Point();
        rings = PolygonClipper.OrientRings(rings);

        double area2 = 0, cx = 0, cy = 0;
        foreach (var ring in rings)
        {
            int n = ring.Count;
            for (var i = 0; i < n; i++)
            {
                var x0 = ring[i][0]; var y0 = ring[i][1];
                var x1 = ring[(i + 1) % n][0]; var y1 = ring[(i + 1) % n][1];
                var cross = x0 * y1 - x1 * y0;
                area2 += cross;
                cx += (x0 + x1) * cross;
                cy += (y0 + y1) * cross;
            }
        }

        if (Math.Abs(area2) < GeometryConstants.Epsilon)
        {
            double sumX = 0, sumY = 0; int cnt = 0;
            foreach (var ring in rings)
                foreach (var p in ring) { sumX += p[0]; sumY += p[1]; cnt++; }
            return cnt == 0 ? new Point() : new Point(sumX / cnt, sumY / cnt);
        }

        // cx 已累计 3·cross·(x0+x1)/2 形式：Cx = cx / (3·area2)，Cy 同理
        return new Point(cx / (3.0 * area2), cy / (3.0 * area2));
    }
}