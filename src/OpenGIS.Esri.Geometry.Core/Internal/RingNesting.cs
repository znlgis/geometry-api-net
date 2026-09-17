using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenGIS.Esri.Geometry.Core.Internal;

/// <summary>
/// 环嵌套结构分析：把平面环集合分组为「壳 + 其洞」。
/// 约定内部代表点试探 + 射线法包含判断（与环方向无关）。
/// </summary>
internal static class RingNesting
{
    public sealed class Part
    {
        public List<double[]> Shell = new();
        public readonly List<List<double[]>> Holes = new();
    }

    /// <summary>输入闭合或开放环坐标串（自动去重闭合点）。返回壳/洞分组（按输入顺序稳定）。</summary>
    public static List<Part> Group(IEnumerable<IEnumerable<double[]>> rings)
    {
        var cleaned = new List<List<double[]>>();
        foreach (var r in rings)
        {
            var ring = r.Select(p => new[] { p[0], p[1] }).ToList();
            if (ring.Count >= 2 && ring[0][0] == ring[ring.Count - 1][0] && ring[0][1] == ring[ring.Count - 1][1]) ring.RemoveAt(ring.Count - 1);
            if (ring.Count >= 3) cleaned.Add(ring);
        }

        var reps = cleaned.Select(Representative).ToList();
        var absArea = cleaned.Select(r => Math.Abs(SignedArea(r))).ToList();
        var order = Enumerable.Range(0, cleaned.Count).OrderByDescending(i => absArea[i]).ToList();
        var depth = new int[cleaned.Count];
        var parent = new int[cleaned.Count];
        for (int i = 0; i < cleaned.Count; i++) parent[i] = -1;
        foreach (var i in order)
        {
            if (reps[i] is null) continue;
            int par = -1;
            foreach (var j in order)
            {
                if (j == i || absArea[j] <= absArea[i]) break;
                if (ContainsPoint(cleaned[j], reps[i]![0], reps[i]![1]) && (par < 0 || absArea[j] < absArea[par]))
                    par = j;
            }

            parent[i] = par;
            depth[i] = par < 0 ? 0 : depth[par] + 1;
        }

        var parts = new List<Part>();
        var shellIndex = new Dictionary<int, Part>();
        for (int k = 0; k < order.Count; k++)
        {
            var i = order[k];
            if (depth[i] % 2 != 0) continue;
            var part = new Part { Shell = cleaned[i] };
            shellIndex[i] = part;
            parts.Add(part);
        }

        for (int k = 0; k < order.Count; k++)
        {
            var i = order[k];
            if (depth[i] % 2 == 0) continue;
            // 向上找偶数深度祖先（父环的壳）
            int anc = parent[i];
            while (anc >= 0 && depth[anc] % 2 != 0) anc = parent[anc];
            if (anc >= 0 && shellIndex.TryGetValue(anc, out var part))
                part.Holes.Add(cleaned[i]);
            else
                parts.Add(new Part { Shell = cleaned[i] });
        }

        return parts;
    }

    public static bool ContainsPoint(List<double[]> ring, double x, double y)
    {
        var inside = false;
        int n = ring.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = ring[i][0], yi = ring[i][1], xj = ring[j][0], yj = ring[j][1];
            if (yi > y != yj > y && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
        }
        return inside;
    }

    public static double[]? Representative(List<double[]> ring)
    {
        int n = ring.Count;
        for (int i = 0; i < n; i++)
        {
            double mx = (ring[(i - 1 + n) % n][0] + ring[(i + 1) % n][0]) / 2;
            double my = (ring[(i - 1 + n) % n][1] + ring[(i + 1) % n][1]) / 2;
            if (ContainsPoint(ring, mx, my)) return new[] { mx, my };
        }
        double minX = ring.Min(p => p[0]), maxX = ring.Max(p => p[0]);
        double minY = ring.Min(p => p[1]), maxY = ring.Max(p => p[1]);
        for (int gx = 0; gx < 16; gx++)
            for (int gy = 0; gy < 16; gy++)
            {
                double x = minX + (gx + 0.5) * (maxX - minX) / 16;
                double y = minY + (gy + 0.5) * (maxY - minY) / 16;
                if (ContainsPoint(ring, x, y)) return new[] { x, y };
            }
        return null;
    }

    private static double SignedArea(List<double[]> ring)
    {
        double s = 0;
        int n = ring.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
            s += ring[j][0] * ring[i][1] - ring[i][0] * ring[j][1];
        return s;
    }
}
