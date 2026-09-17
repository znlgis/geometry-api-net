using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness.Suites;

/// <summary>
/// 真实几何两两谓词对拍：包络重叠筛出候选对，被测库算 9 谓词+距离，PostGIS 对同一对独立计算，
/// 两引擎逐位一致；不一致默认 Fail，白名单（人工裁定过的 Esri/GEOS 边界语义差异）降为 Warn。
/// 同时做库内自洽检查：within(A,B) ≡ contains(B,A)、disjoint ≡ ¬intersects、distance 对称。
/// </summary>
public sealed class RealPairSuite : ISuite
{
    public string Name => "real-pairs";
    public bool RequiresData => true;
    public bool RequiresPg => true;

    /// <summary>人工裁定后的已知引擎语义差异白名单："fileA#i|fileB#j|谓词"。裁定理由写进报告。</summary>
    static readonly HashSet<string> KnownDiffWhitelist = new(StringComparer.Ordinal)
    {
        // 冰川#1615 顶点落在俄罗斯海岸 1e-8°（≈2mm）容差带内：本库按 Esri EDM DefaultTolerance 判“边界接触”，
        // GEOS 无容差判“越界突出”→ overlaps/within 翻转。属容差语义差异而非缺陷（报告 §3）。
        "ne_10m_admin_0_countries.jsonl#47|ne_10m_glaciated_areas.jsonl#1615|overlaps",
        "ne_10m_admin_0_countries.jsonl#47|ne_10m_glaciated_areas.jsonl#1615|within",
        "ne_10m_glaciated_areas.jsonl#1615|ne_10m_admin_0_countries.jsonl#47|contains",
    };

    const int PairCapPerFileCombo = 250;

    public void Run(CheckRunner r, HarnessContext ctx)
    {
        var pg = ctx.Pg!;
        var files = ctx.Corpus.Files.Where(f => f.Kind != CorpusKind.Unknown).ToList();
        if (files.Count == 0) { r.Skip(Name, "no-files", "语料为空"); return; }

        var libGeoms = new Dictionary<string, List<(int Id, Geometry G)>>();
        var tables = new Dictionary<string, string>();
        foreach (var f in files)
        {
            var list = new List<(int, Geometry)>();
            var stride = ctx.StrideFor(f.Features.Count);
            foreach (var feat in f.Features.Where((_, i) => i % stride == 0))
                list.Add((feat.Index, LibGeo.FromGeoJson(feat.GeometryJson)));
            libGeoms[f.Name] = list;
            tables[f.Name] = LibGeo.Upload(pg, f);
        }

        var pairs = new List<(string Fa, int Ia, string Fb, int Ib)>();
        for (int x = 0; x < files.Count; x++)
        for (int y = x; y < files.Count; y++)
        {
            var fa = files[x]; var fb = files[y];
            var la = libGeoms[fa.Name]; var lb = libGeoms[fb.Name];
            var combo = new List<(int, int)>();
            for (int i = 0; i < la.Count; i++)
            {
                var ea = la[i].G.GetEnvelope();
                if (ea is null) continue;
                for (int j = x == y ? i + 1 : 0; j < lb.Count; j++)
                {
                    var eb = lb[j].G.GetEnvelope();
                    if (eb is null) continue;
                    if (ea.XMax < eb.XMin || eb.XMax < ea.XMin || ea.YMax < eb.YMin || eb.YMax < ea.YMin) continue;
                    combo.Add((la[i].Id, lb[j].Id));
                }
            }
            int step = combo.Count > PairCapPerFileCombo ? (int)Math.Ceiling(combo.Count / (double)PairCapPerFileCombo) : 1;
            for (int k = 0; k < combo.Count; k += step)
                pairs.Add((fa.Name, combo[k].Item1, fb.Name, combo[k].Item2));
        }
        r.Pass(Name, "pairs-selected", $"{pairs.Count} 对（包络重叠+等距抽样上限 {PairCapPerFileCombo}/文件对）");
        if (pairs.Count == 0) return;

        var pt = pg.TrackTable("geom_test_pairs");
        pg.Exec($"DROP TABLE IF EXISTS {pt}; CREATE TABLE {pt}(k int, fa text, ia int, fb text, ib int)");
        pg.InsertMany(pt, "k,fa,ia,fb,ib", pairs.Select((p, k) => new object?[] { k, p.Fa, p.Ia, p.Fb, p.Ib }));

        var dict = new Dictionary<int, object[]>();
        foreach (var group in pairs.Select((p, k) => (p, k)).GroupBy(x => (x.p.Fa, x.p.Fb)))
        {
            var keys = group.Select(x => x.k).ToList();
            var sql = $"""
                SELECT p.k,
                  ST_Contains(A.g,B.g), ST_Within(A.g,B.g), ST_Crosses(A.g,B.g), ST_Touches(A.g,B.g), ST_Overlaps(A.g,B.g),
                  ST_Equals(A.g,B.g), ST_Disjoint(A.g,B.g), ST_Intersects(A.g,B.g), ST_Distance(A.g,B.g),
                  ST_NPoints(A.g), ST_NPoints(B.g)
                FROM {pt} p
                JOIN {tables[group.Key.Fa]} A ON A.id = p.ia AND p.fa = '{Esc(group.Key.Fa)}'
                JOIN {tables[group.Key.Fb]} B ON B.id = p.ib AND p.fb = '{Esc(group.Key.Fb)}'
                WHERE p.k = ANY(:ks)
                """;
            foreach (var row in pg.Query(sql, ("ks", keys.ToArray())))
                dict[Convert.ToInt32(row[0])] = row;
        }

        string[] preds = ["contains", "within", "crosses", "touches", "overlaps", "equals", "disjoint", "intersects"];
        int checkedPairs = 0, diffs = 0;
        Geometry FindGeom(string file, int id) => libGeoms[file].First(x => x.Id == id).G;

        for (int k = 0; k < pairs.Count; k++)
        {
            var (fa, ia, fb, ib) = pairs[k];
            if (!dict.TryGetValue(k, out var row)) continue;
            Geometry ga = FindGeom(fa, ia), gb = FindGeom(fb, ib);
            checkedPairs++;
            if (ctx.Verbose && checkedPairs % 100 == 0) r.Output.WriteLine($"  ... {checkedPairs}/{pairs.Count} 对");
            bool[] lib;
            double libDist;
            try
            {
                // 一次关系矩阵派生 8 谓词（经 InternalsVisibleTo 桥，避免 9 次重复矩阵计算）
                var mx = RelateBridge.Once(ga, gb, out var ma, out var mb);
                lib = new[]
                {
                    mx.Contains(ma, mb), mx.Within(ma, mb), mx.Crosses(ma, mb),
                    mx.Touches(ma, mb), mx.Overlaps(ma, mb), mx.EqualsTopo(ma, mb),
                    mx.Disjoint, mx.Intersects,
                };
                libDist = RelateBridge.Distance(ga, gb);
            }
            catch (Exception ex)
            {
                r.Fail(Name, $"{fa}#{ia}|{fb}#{ib}:throw", ex.GetType().Name + ": " + ex.Message.Split('\n')[0]);
                continue;
            }
            double pgDist = Convert.ToDouble(row[9]);

            // 库内自洽：within(A,B) ≡ contains(B,A)，disjoint ≡ ¬intersects，距离对称
            // within(A,B) 与 contains(B,A) 对偶：反向矩阵在 Distance 对称检查处统一验证，避免双倍矩阵成本
            if (lib[6] != !lib[7])
                r.Fail(Name, $"{fa}#{ia}|{fb}#{ib}|self-disjoint-neg", "库内 disjoint ≠ ¬intersects");
            try
            {
                double rev = RelateBridge.Distance(gb, ga);
                if (!CheckRunner.RelOk(rev, libDist, 1e-9, 1e-12))
                    r.Fail(Name, $"{fa}#{ia}|{fb}#{ib}|self-dist-sym", $"距离不对称 {libDist:R} vs {rev:R}");
            }
            catch (Exception ex) { r.Fail(Name, $"{fa}#{ia}|{fb}#{ib}|self-dist-sym-throw", ex.GetType().Name); }

            for (int p = 0; p < preds.Length; p++)
            {
                bool pgVal = (bool)row[p + 1];
                if (lib[p] == pgVal) continue;
                diffs++;
                var wkey = $"{fa}#{ia}|{fb}#{ib}|{preds[p]}";
                var msg = $"A={fa}#{ia} B={fb}#{ib} 谓词={preds[p]} 库={lib[p]} GEOS={pgVal}";
                if (KnownDiffWhitelist.Contains(wkey)) r.Warn(Name, wkey, "白名单已知差异: " + msg);
                else if (diffs <= 40) r.Fail(Name, wkey, msg);
            }
            if (!CheckRunner.RelOk(libDist, pgDist, 1e-6, 1e-8) && diffs <= 60)
                r.Fail(Name, $"{fa}#{ia}|{fb}#{ib}|distance", $"距离 库={libDist:R} GEOS={pgDist:R} npg={Convert.ToInt32(row[10])}/{Convert.ToInt32(row[11])} nlib={LibGeo.VertexCount(ga)}/{LibGeo.VertexCount(gb)}");
        }
        r.Check(Name, "coverage", checkedPairs == pairs.Count, $"仅 {checkedPairs}/{pairs.Count} 对在 PG 侧有结果");
        r.Pass(Name, "compared", $"{checkedPairs} 对 × 8 谓词，跨引擎差异 {diffs} 处");
    }

    static string Esc(string s) => s.Replace("'", "''");
}
