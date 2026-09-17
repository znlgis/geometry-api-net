using System.Diagnostics;

namespace GeoHarness;

/// <summary>单项检查结果。</summary>
public enum CheckStatus { Pass, Fail, Warn, Skip }

public sealed record CheckResult(string Suite, string Name, CheckStatus Status, string Detail, double? ElapsedMs = null);

/// <summary>
/// 检查记录器：收集 Pass/Fail/Warn/Skip，输出汇总；存在 Fail 时进程退出码为 1（可接 CI）。
/// Warn 仅用于"方法学差异已文档化"的场景，不影响退出码。
/// </summary>
public sealed class CheckRunner(TextWriter output)
{
    private readonly List<CheckResult> _results = new();
    public TextWriter Output { get; } = output;

    public IReadOnlyList<CheckResult> Results => _results;

    public void Pass(string suite, string name, string detail = "", double? ms = null)
        => _results.Add(new CheckResult(suite, name, CheckStatus.Pass, detail, ms));

    public void Fail(string suite, string name, string detail)
    {
        _results.Add(new CheckResult(suite, name, CheckStatus.Fail, detail));
        Output.WriteLine($"  [FAIL] {suite}/{name}: {detail}");
    }

    public void Warn(string suite, string name, string detail)
    {
        _results.Add(new CheckResult(suite, name, CheckStatus.Warn, detail));
        Output.WriteLine($"  [WARN] {suite}/{name}: {detail}");
    }

    public void Skip(string suite, string name, string detail)
        => _results.Add(new CheckResult(suite, name, CheckStatus.Skip, detail));

    /// <summary>断言式检查：cond 为假则记 Fail，返回 cond。</summary>
    public bool Check(string suite, string name, bool cond, string failDetail, string? passDetail = null)
    {
        if (cond) { if (passDetail != null) Pass(suite, name, passDetail); else _results.Add(new CheckResult(suite, name, CheckStatus.Pass, "")); }
        else Fail(suite, name, failDetail);
        return cond;
    }

    public static bool RelOk(double actual, double expected, double rel, double abs = 1e-12)
        => Math.Abs(actual - expected) <= Math.Max(abs, rel * Math.Abs(expected));

    public bool CheckRel(string suite, string name, double actual, double expected, double rel, double abs = 1e-12, double? ms = null)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual) || double.IsNaN(expected) || double.IsInfinity(expected))
        {
            // 双方同为 +∞ 视为一致（退化场景），否则 Fail
            if (double.IsPositiveInfinity(actual) && double.IsPositiveInfinity(expected))
            {
                _results.Add(new CheckResult(suite, name, CheckStatus.Pass, "inf==inf", ms));
                return true;
            }
            Fail(suite, name, $"非有限值 actual={actual} expected={expected}");
            return false;
        }
        return CheckRelInner(suite, name, actual, expected, rel, abs, ms);
    }

    private bool CheckRelInner(string suite, string name, double actual, double expected, double rel, double abs, double? ms)
    {
        if (RelOk(actual, expected, rel, abs))
        {
            _results.Add(new CheckResult(suite, name, CheckStatus.Pass, "", ms));
            return true;
        }
        Fail(suite, name, $"数值不一致 actual={actual:R} expected={expected:R} rel={Math.Abs(actual - expected) / Math.Max(1e-300, Math.Abs(expected)):E} (容差 rel={rel})");
        return false;
    }

    /// <summary>带计时执行：耗时超过 thresholdMs 记 Fail（性能回归断言）。</summary>
    public T Timed<T>(string suite, string name, double thresholdMs, Func<T> action)
    {
        var sw = Stopwatch.StartNew();
        var ret = action();
        sw.Stop();
        if (sw.ElapsedMilliseconds > thresholdMs)
            Fail(suite, name, $"超时 {sw.ElapsedMilliseconds}ms > 阈值 {thresholdMs}ms");
        else
            _results.Add(new CheckResult(suite, name + ":timing", CheckStatus.Pass, $"{sw.ElapsedMilliseconds}ms", sw.ElapsedMilliseconds));
        return ret;
    }

    /// <summary>异常保护执行：抛异常记 Fail，不中断整套。</summary>
    public bool Try(string suite, string name, Action action)
    {
        try { action(); return true; }
        catch (Exception ex)
        {
            Fail(suite, name, $"抛出 {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
            return false;
        }
    }

    public int Summary()
    {
        int pass = _results.Count(r => r.Status == CheckStatus.Pass);
        int fail = _results.Count(r => r.Status == CheckStatus.Fail);
        int warn = _results.Count(r => r.Status == CheckStatus.Warn);
        int skip = _results.Count(r => r.Status == CheckStatus.Skip);
        Output.WriteLine();
        Output.WriteLine($"==== 汇总: Pass={pass} Fail={fail} Warn={warn} Skip={skip} 总计={_results.Count} ====");
        foreach (var g in _results.GroupBy(r => r.Suite).OrderBy(g => g.Key))
            Output.WriteLine($"  {g.Key,-24} P={g.Count(r => r.Status == CheckStatus.Pass)} F={g.Count(r => r.Status == CheckStatus.Fail)} W={g.Count(r => r.Status == CheckStatus.Warn)} S={g.Count(r => r.Status == CheckStatus.Skip)}");
        var failed = _results.Where(r => r.Status == CheckStatus.Fail).ToList();
        if (failed.Count > 0)
        {
            Output.WriteLine();
            Output.WriteLine($"---- Fail 明细 ({failed.Count}) ----");
            foreach (var f in failed.Take(200))
                Output.WriteLine($"  {f.Suite}/{f.Name}: {f.Detail}");
            if (failed.Count > 200) Output.WriteLine($"  ... 其余 {failed.Count - 200} 条略");
        }
        var warns = _results.Where(r => r.Status == CheckStatus.Warn).ToList();
        if (warns.Count > 0)
        {
            Output.WriteLine();
            Output.WriteLine($"---- Warn 明细 ({warns.Count}) ----");
            foreach (var w in warns.Take(100))
                Output.WriteLine($"  {w.Suite}/{w.Name}: {w.Detail}");
        }
        return fail == 0 ? 0 : 1;
    }
}
