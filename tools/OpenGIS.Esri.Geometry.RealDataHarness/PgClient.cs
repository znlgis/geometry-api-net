using Npgsql;

namespace GeoHarness;

/// <summary>
/// PostGIS 客户端（本机/CI Docker 均可）。连接参数全部来自 GEOM_TEST_PG_* 环境变量，
/// 默认 127.0.0.1:5432 postgres/postgres@postgres；表统一 geom_test_ 前缀并登记，Finally 里 DropAll 清理。
/// </summary>
public sealed class PgClient : IDisposable
{
    private readonly NpgsqlConnection _conn;
    private readonly List<string> _tables = new();

    public string Description { get; }

    private PgClient(NpgsqlConnection conn, string description)
    {
        _conn = conn;
        Description = description;
    }

    public static PgClient? TryConnect(out string? error)
    {
        error = null;
        var host = Environment.GetEnvironmentVariable("GEOM_TEST_PG_HOST") ?? "127.0.0.1";
        var port = Environment.GetEnvironmentVariable("GEOM_TEST_PG_PORT") ?? "5432";
        var db = Environment.GetEnvironmentVariable("GEOM_TEST_PG_DB") ?? "postgres";
        var user = Environment.GetEnvironmentVariable("GEOM_TEST_PG_USER") ?? "postgres";
        var pass = Environment.GetEnvironmentVariable("GEOM_TEST_PG_PASSWORD") ?? "postgres";
        var connStr = $"Host={host};Port={port};Database={db};Username={user};Password={pass};Timeout=5;CommandTimeout=600";
        try
        {
            var conn = new NpgsqlConnection(connStr);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT postgis_version()", conn);
            var v = cmd.ExecuteScalar()?.ToString();
            if (string.IsNullOrEmpty(v)) { conn.Dispose(); error = "无 PostGIS"; return null; }
            return new PgClient(conn, $"{host}:{port}/{db} PostGIS {v.Split()[0]}");
        }
        catch (Exception ex)
        {
            error = ex.Message.Split('\n')[0];
            return null;
        }
    }

    /// <summary>长跑中断线自动重连（表登记在服务端仍存活）。</summary>
    private void EnsureOpen()
    {
        if (_conn.State != System.Data.ConnectionState.Open)
        {
            _conn.Close();
            _conn.Open();
        }
    }

    public string TrackTable(string name) { _tables.Add(name); return name; }

    public void DropTracked()
    {
        foreach (var t in _tables)
            try { Exec($"DROP TABLE IF EXISTS {t} CASCADE"); } catch { /* 尽力清理 */ }
        _tables.Clear();
    }

    public void Exec(string sql)
    {
        EnsureOpen();
        using var cmd = new NpgsqlCommand(sql, _conn);
        cmd.CommandTimeout = 600;
        cmd.ExecuteNonQuery();
    }

    public void Exec(string sql, (string, object)[] ps)
    {
        EnsureOpen();
        using var cmd = new NpgsqlCommand(sql, _conn);
        cmd.CommandTimeout = 600;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        cmd.ExecuteNonQuery();
    }

    public object? ScalarObj(string sql, params (string, object)[] ps)
    {
        EnsureOpen();
        using var cmd = new NpgsqlCommand(sql, _conn);
        cmd.CommandTimeout = 600;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        return cmd.ExecuteScalar();
    }

    public T Scalar<T>(string sql, params (string, object)[] ps)
    {
        var o = ScalarObj(sql, ps);
        if (o is null) throw new InvalidOperationException($"标量查询返回 NULL: {sql}");
        return (T)Convert.ChangeType(o, typeof(T));
    }

    public List<object[]> Query(string sql, params (string, object)[] ps)
    {
        EnsureOpen();
        using var cmd = new NpgsqlCommand(sql, _conn);
        cmd.CommandTimeout = 600;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        using var rd = cmd.ExecuteReader();
        var rows = new List<object[]>();
        while (rd.Read())
        {
            var row = new object[rd.FieldCount];
            for (int i = 0; i < rd.FieldCount; i++) row[i] = rd.IsDBNull(i) ? null! : rd.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }
    public List<(int, bool)> QueryBoolMap(string sql, params (string, object)[] ps)
        => Query(sql, ps).Select(r => (Convert.ToInt32(r[0]), (bool)r[1])).ToList();

    /// <summary>批量装载 GeoJSON 文本为 geometry 列（预处理语句循环，万级要素秒级）。</summary>
    public void UploadRows(string table, IEnumerable<(int Id, string GeoJson)> rows)
    {
        EnsureOpen();
        using var cmd = new NpgsqlCommand(
            $"INSERT INTO {table}(id, g) VALUES (:id, ST_SetSRID(ST_GeomFromGeoJSON(:gj), 4326))", _conn);
        cmd.CommandTimeout = 600;
        var pId = cmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Integer);
        var pGj = cmd.Parameters.Add("gj", NpgsqlTypes.NpgsqlDbType.Text);
        cmd.Prepare();
        foreach (var (id, gj) in rows)
        {
            pId.Value = id;
            pGj.Value = gj;
            cmd.ExecuteNonQuery();
        }
        cmd.Unprepare();
    }

    /// <summary>多值批量 INSERT（分块防包过大）。</summary>
    public void InsertMany(string table, string columns, IEnumerable<object?[]> rows, int batchSize = 200)
    {
        var batch = rows.ToList();
        int width = columns.Split(',').Length;
        EnsureOpen();
        for (int off = 0; off < batch.Count; off += batchSize)
        {
            var chunk = batch.Skip(off).Take(batchSize).ToList();
            var sb = new System.Text.StringBuilder($"INSERT INTO {table} ({columns}) VALUES ");
            using var cmd = new NpgsqlCommand { Connection = _conn, CommandTimeout = 600 };
            for (int i = 0; i < chunk.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('(').Append(string.Join(',', Enumerable.Range(0, width).Select(c => "$" + (i * width + c + 1)))).Append(')');
                for (int c = 0; c < width; c++)
                    cmd.Parameters.AddWithValue("", chunk[i][c] ?? DBNull.Value); // 空名=位置参数（$n 绑定要求按序，命名会丢参）
            }
            cmd.CommandText = sb.ToString();
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose() { _conn.Dispose(); }
}
