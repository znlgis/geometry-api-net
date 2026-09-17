namespace GeoHarness;

/// <summary>
/// geometry-api-net 真实数据测试 harness（控制台）。
///
/// 数据无关约定：
///   - 语料目录仅经 --data 或环境变量 GEOM_REAL_DATA_DIR 注入；缺失时依赖语料的套件整体 Skip。
///   - PostGIS 经 --pg 或 GEOM_TEST_PG_HOST/PORT/DB/USER/PASSWORD 注入；不可达时交叉验证套件 Skip。
///   - 期望值独立推导：OGC 谓词用例为人工按 9-intersection 推导的权威期望（TSV），
///     PostGIS 作第二独立引擎三方对拍；结构期望由 JSON 文本独立解析 + SHP/DBF 文件头交叉校验（不经被测库）。
///   - 任一 Fail → 退出码 1；Warn 不影响退出码（方法学已文档化的差异）。
///
/// 用法: RealDataHarness [--data DIR] [--pg] [--cases FILE] [--only s1,s2] [--sample N] [--list]
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string? dataDir = null, only = null, cases = null;
        bool usePg = false, verbose = false;
        int sample = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--data": dataDir = args[++i]; break;
                case "--pg": usePg = true; break;
                case "--cases": cases = args[++i]; break;
                case "--only": only = args[++i]; break;
                case "--sample": sample = int.Parse(args[++i]); break;
                case "--verbose": verbose = true; break;
                case "--list":
                    foreach (var s in HarnessContext.AllSuites()) Console.WriteLine(s.Name);
                    return 0;
                default:
                    Console.Error.WriteLine($"未知参数: {args[i]}");
                    return 2;
            }
        }

        dataDir ??= Environment.GetEnvironmentVariable("GEOM_REAL_DATA_DIR");
        cases ??= Environment.GetEnvironmentVariable("GEOM_TEST_CASES") ?? LocateDefaultCasesFile();

        PgClient? pg = null;
        string? pgErr = null;
        if (usePg || Environment.GetEnvironmentVariable("GEOM_TEST_PG_HOST") is { Length: > 0 })
            pg = PgClient.TryConnect(out pgErr);

        var ctx = new HarnessContext { DataDir = dataDir, Pg = pg, CasesFile = cases, Sample = sample, Verbose = verbose };

        Console.WriteLine($"data-dir : {dataDir ?? "<未提供 → 真实数据套件 Skip>"}");
        Console.WriteLine($"pg       : {pg?.Description ?? (usePg ? $"不可用: {pgErr}" : "<未启用 → 交叉验证套件 Skip>")}");
        Console.WriteLine($"cases    : {cases ?? "<未找到>"}");
        Console.WriteLine();

        var runner = new CheckRunner(Console.Out);
        var suites = HarnessContext.AllSuites().ToList();
        if (only is not null)
        {
            var names = only.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            suites = suites.Where(s => names.Contains(s.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            if (suites.Count == 0)
            {
                Console.Error.WriteLine($"--only 未匹配任何套件。可用: {string.Join(", ", HarnessContext.AllSuites().Select(s => s.Name))}");
                return 2;
            }
        }

        foreach (var suite in suites)
        {
            if ((suite.RequiresData && ctx.DataDir is null) || (suite.RequiresPg && ctx.Pg is null))
            {
                var why = suite.RequiresData && ctx.DataDir is null ? "无语料目录" : "无 PostGIS 连接";
                runner.Skip(suite.Name, "precondition", $"{why}，整套跳过");
                Console.WriteLine($"---- {suite.Name} [SKIP: {why}] ----");
                continue;
            }
            Console.WriteLine($"---- {suite.Name} ----");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try { suite.Run(runner, ctx); }
            catch (Exception ex) { runner.Fail(suite.Name, "suite-exception", ex.ToString().Split('\n')[0]); }
            sw.Stop();
            Console.WriteLine($"({suite.Name} 用时 {sw.Elapsed.TotalSeconds:F1}s)");
        }

        return runner.Summary();
    }

    /// <summary>自当前目录向上找 testcases/ogc-sf-relate-cases.tsv（仓库内标准用例语料）。</summary>
    private static string? LocateDefaultCasesFile()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "testcases", "ogc-sf-relate-cases.tsv");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
