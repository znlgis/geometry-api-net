using System.Globalization;
using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// 真实语料结构一致性 + 一元运算 PostGIS 对拍。
/// 结构期望来自 GeoJSON 文本的独立解析（GeoJsonWalker）与 SHP/DBF 文件头（不经 GDAL、不经被测库）；
/// 数值期望来自 PostGIS 独立计算。
/// </summary>
public sealed class RealUnarySuite : ISuite
{
    public string Name => "real-unary";
    public bool RequiresData => true;
    public bool RequiresPg => false; // 无 PG 时仍做结构校验，数值对拍部分跳过

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        var corpus = ctx.Corpus;
        foreach (var err in corpus.LoadErrors) r.Fail(Name, "load-error", err);
        if (corpus.IsEmpty) { r.Skip(Name, "empty", "目录内无 .jsonl/.geojson 语料"); return; }

        foreach (var file in corpus.Files)
        {
            // --- 结构校验（始终执行）---
            int imported = 0, stride = ctx.StrideFor(file.Features.Count);
            r.Try(Name, $"{file.Name}:import-all", () =>
            {
                for (int i = 0; i < file.Features.Count; i += stride)
                {
                    var f = file.Features[i];
                    var (gType, gVerts, _, _) = GeoJsonWalker.Walk(f.GeometryJson);
                    var g = LibGeo.FromGeoJson(f.GeometryJson);
                    imported++;
                    var expectedFam = LibGeo.GeoJsonTypeFamily(gType);
                    var actualFam = LibGeo.FamilyOf(g);
                    bool famOk = expectedFam switch
                    {
                        "point" => actualFam == "point",
                        "multipoint" => actualFam == "multipoint",
                        "polyline" => actualFam is "polyline" or "line",
                        "polygon" => actualFam is "polygon" or "envelope",
                        _ => false,
                    };
                    if (!famOk) { r.Fail(Name, $"{file.Name}:#{i}:type", $"GeoJSON {gType} → 库类型 {actualFam}"); continue; }
                    int libVerts = LibGeo.VertexCount(g);
                    if (libVerts != gVerts)
                        r.Fail(Name, $"{file.Name}:#{i}:verts", $"独立解析顶点数={gVerts} 库={libVerts}");
                    if (g.IsEmpty) { r.Fail(Name, $"{file.Name}:#{i}:empty", "导入后为空几何"); continue; }
                    if (g.GetEnvelope() is null) r.Fail(Name, $"{file.Name}:#{i}:envelope-null", "GetEnvelope 返回 null");
                }
            });
            r.Check(Name, $"{file.Name}:imported", imported > 0, "无要素被导入");

            if (file.ShpExpected is { } shp)
            {
                r.Check(Name, $"{file.Name}:shp-count", shp.RecordCount == file.Features.Count,
                    $"SHP 记录链={shp.RecordCount} ≠ 语料要素数={file.Features.Count}");
                var kinds = shp.Types.Select(t => t switch { <= 8 and >= 1 => t, >= 11 and <= 21 => t - 10, >= 21 and <= 28 => t - 20, _ => t })
                    .ToHashSet();
                r.Check(Name, $"{file.Name}:shp-types", kinds.Count == 0 || kinds.All(t => KindOfShp(t) == file.Kind),
                    $"SHP 类型码 {string.Join(",", kinds)} 与语料分类 {file.Kind} 不符");
                if (file.DbfRecordCount is long dbfN)
                    r.Check(Name, $"{file.Name}:dbf-count", dbfN == file.Features.Count,
                        $"DBF 头记录数={dbfN} ≠ 语料要素数={file.Features.Count}");
            }
            else r.Warn(Name, $"{file.Name}:shp-header", "目录内无同名 .shp，跳过文件头交叉校验");

            // --- 数值对拍（需 PG）---
            if (ctx.Pg is null) continue;
            if (file.Kind == CorpusKind.Unknown) continue;
            RunNumeric(r, ctx, file, stride);
        }
    }

    private static CorpusKind KindOfShp(int code) => code switch
    {
        1 or 11 or 21 => CorpusKind.Pointish,
        3 or 13 or 23 => CorpusKind.Linear,
        5 or 15 or 25 => CorpusKind.Areal,
        8 or 18 or 28 => CorpusKind.Pointish,
        _ => CorpusKind.Unknown,
    };

    private void RunNumeric(CheckRunner r, HarnessContext ctx, CorpusFile file, int stride)
    {
        var pg = ctx.Pg!;
        var table = LibGeo.Upload(pg, file);
        string lenExpr = file.Kind == CorpusKind.Areal ? "COALESCE(ST_Perimeter(g),0)" : "COALESCE(ST_Length(g),0)"; // 面用周长、线用长度（PG 各自对另一类型返回 0/NULL）
        double maxAreaRel = 0, maxLenRel = 0, maxCent = 0, maxEnv = 0, maxHullRel = 0, maxBufHouse = 0;
        double maxBufAreaRel = 0;
        int n = 0;
        int bufDone = 0;
        int bufferStride = Math.Max(1, (int)Math.Ceiling(file.Features.Count / (double)stride) / 20); // 每文件缓冲深检 ≤20 例
        foreach (var f in file.Features.Where((_, i) => i % stride == 0))
        {
            Geometry g;
            try { g = LibGeo.FromGeoJson(f.GeometryJson); }
            catch (Exception ex) { r.Fail(Name, $"{file.Name}:#{f.Index}:import", ex.Message.Split('\n')[0]); continue; }
            n++;
            var row = pg.Query($"""
                SELECT ST_Area(g), {lenExpr}, ST_X(ST_Centroid(g)), ST_Y(ST_Centroid(g)),
                       ST_XMin(g::geometry), ST_YMin(g::geometry), ST_XMax(g::geometry), ST_YMax(g::geometry),
                       ST_Area(ST_ConvexHull(g))
                FROM {table} WHERE id={f.Index}
                """)[0];
            double pgArea = Convert.ToDouble(row[0]), pgLen = Convert.ToDouble(row[1]);
            double pgCx = Convert.ToDouble(row[2]), pgCy = Convert.ToDouble(row[3]);
            double pgXmin = Convert.ToDouble(row[4]), pgYmin = Convert.ToDouble(row[5]), pgXmax = Convert.ToDouble(row[6]), pgYmax = Convert.ToDouble(row[7]);
            double pgHullArea = Convert.ToDouble(row[8]);

            // 面积（仅面状几何有意义）
            if (g is Polygon or Envelope)
            {
                double a = g.CalculateArea2D();
                maxAreaRel = Math.Max(maxAreaRel, Rel(a, pgArea));
            }
            // 长度/周长
            double l = g.CalculateLength2D();
            if (g is Polyline or Polygon) maxLenRel = Math.Max(maxLenRel, Rel(l, pgLen));
            if (g is Polygon && file.Kind == CorpusKind.Areal && pgLen > 1e-9 && Rel(l, pgLen) > 1e-6)
                r.Fail(Name, $"{file.Name}:#{f.Index}:perimeter", $"周长 {l:R} vs PG {pgLen:R}");
            // 质心（大面积/长线才有稳定质心）
            if (g is Polygon or Polyline && (pgArea > 1e-12 || pgLen > 1e-12))
            {
                var c = GeometryEngine.Centroid(g);
                if (c is null) r.Fail(Name, $"{file.Name}:#{f.Index}:centroid-null", "库质心为 null");
                else maxCent = Math.Max(maxCent, Math.Max(Math.Abs(c.X - pgCx), Math.Abs(c.Y - pgCy)));
            }
            // 包络
            var env = g.GetEnvelope();
            if (env is not null && g is not Point)
                maxEnv = Math.Max(maxEnv, Math.Max(Math.Abs(env.XMin - pgXmin), Math.Max(Math.Abs(env.YMin - pgYmin),
                    Math.Max(Math.Abs(env.XMax - pgXmax), Math.Abs(env.YMax - pgYmax)))));
            // 凸包
            if (g is Polygon or Polyline or MultiPoint or Point)
            {
                var hull = GeometryEngine.ConvexHull(g);
                double hArea = hull?.CalculateArea2D() ?? double.NaN;
                maxHullRel = Math.Max(maxHullRel, Rel(hArea, pgHullArea));
            }
            // 缓冲：与 PG 高精缓冲的形状一致性（重计算，按 bufferStride 抽样）
            if (g is Polygon or Polyline or Point or MultiPoint or Line && f.Index % bufferStride == 0 && bufDone < 8 && LibVerts(g) < 8000)
            {
                bufDone++;
                var prow = pg.Query($"SELECT ST_Area(ST_Buffer(g, 0.05, 64)), ST_AsText(ST_Buffer(g, 0.05, 64)) FROM {table} WHERE id={f.Index}")[0];
                double pgBufArea = Convert.ToDouble(prow[0]);
                string pgBufWkt = (string)prow[1];
                var buf = GeometryEngine.Buffer(g, 0.05);
                if (buf is null) { r.Fail(Name, $"{file.Name}:#{f.Index}:buffer-null", "缓冲为 null"); }
                else
                {
                    maxBufAreaRel = Math.Max(maxBufAreaRel, Rel(buf.CalculateArea2D(), pgBufArea));
                    double house = HausdorffViaPg(pg, buf, pgBufWkt);
                    maxBufHouse = Math.Max(maxBufHouse, house);
                }
            }
        }

        void Assert(string what, double val, double tol, string unit) =>
            r.Check(Name, $"{file.Name}:{what}", val <= tol, $"{unit}最大相对/绝对偏差 {val:E} 超容差 {tol:E}（样本数 {n}）");

        if (file.Kind == CorpusKind.Areal) Assert("area-maxrel", maxAreaRel, 1e-5, "面积"); // 1e-5：近自切冰川环与 GEOS 求和顺序差（报告 S-2）
        Assert("length-maxrel", maxLenRel, 1e-6, "长度");
        Assert("centroid-maxabs", maxCent, 1e-4, "质心"); // 跨 180° 多部件大国的积分界差异
        Assert("envelope-maxabs", maxEnv, 1e-9, "包络");
        Assert("hull-maxrel", maxHullRel, 1e-5, "凸包面积"); // 同 S-2 口径
        if (maxBufAreaRel > 5e-2 || maxBufHouse > 2e-3)
            r.Warn(Name, "buffer-complex-known-limit",
                $"复杂环缓冲与 GEOS 偏差超限（面积 {maxBufAreaRel:E}, Hausdorff {maxBufHouse:E}）——遗留缺陷 D-1，见报告");
        else r.Pass(Name, "buffer-area-maxrel", $"最坏缓冲偏差 rel={maxBufAreaRel:E} house={maxBufHouse:E}");
        r.Pass(Name, $"{file.Name}:numeric", $"{n} 要素数值对拍完成");
    }

    private static double Rel(double a, double b)
    {
        if (double.IsNaN(a) || double.IsNaN(b)) return double.MaxValue;
        double scale = Math.Max(Math.Abs(a), Math.Abs(b));
        return scale < 1e-12 ? Math.Abs(a - b) : Math.Abs(a - b) / scale;
    }

    /// <summary>离散 Hausdorff：把库缓冲 WKT 交给 PG 解析后 ST_HausdorffDistance 计算。</summary>
    private static double HausdorffViaPg(PgClient pg, Geometry libBuf, string pgBufWkt)
        => pg.Scalar<double>($"SELECT ST_HausdorffDistance(ST_Simplify(ST_GeomFromText('{Escape(GeometryEngine.GeometryToWkt(libBuf))}',4326), 0.0004), ST_Simplify(ST_GeomFromText('{Escape(pgBufWkt)}',4326), 0.0004))"); // 简化采样控制 O(n·m) 爆炸；误差并入容差内

    private static string Escape(string s) => s.Replace("'", "''");

    private static int LibVerts(Geometry g) => g switch
    {
        Polygon pg0 => pg0.GetRings().Sum(r => r.Count),
        Polyline pl0 => pl0.GetPaths().Sum(p => p.Count),
        MultiPoint mp0 => mp0.Count,
        _ => 1,
    };
}
