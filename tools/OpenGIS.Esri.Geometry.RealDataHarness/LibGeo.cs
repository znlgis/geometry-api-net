using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;

namespace GeoHarness;

/// <summary>被测库辅助：导入、顶点计数、类型名映射、PG 表装载。</summary>
public static class LibGeo
{
    public static Geometry FromGeoJson(string geometryJson) => GeometryEngine.GeometryFromGeoJson(geometryJson);
    public static Geometry FromWkt(string wkt) => GeometryEngine.GeometryFromWkt(wkt);
    public static string ToWkt(Geometry g) => GeometryEngine.GeometryToWkt(g);
    public static string ToGeoJson(Geometry g) => GeometryEngine.GeometryToGeoJson(g);
    public static byte[] ToWkb(Geometry g, bool bigEndian) => GeometryEngine.GeometryToWkb(g, bigEndian);

    /// <summary>几何总顶点数（含环闭合作为独立顶点计）。Polygon 多部分经 MultiPolygon GeoJSON 导入。</summary>
    public static int VertexCount(Geometry g) => g switch
    {
        Point => 1,
        MultiPoint mp => mp.Count,
        Line l => 2,
        Polyline pl => pl.GetPaths().Sum(p => p.Count),
        Polygon pg => pg.GetRings().Sum(r => r.Count),
        Envelope e => 5,
        _ => throw new NotSupportedException($"未知几何类型 {g.GetType().Name}"),
    };

    /// <summary>库几何类型 → GeoJSON 类型族（Polygon 覆盖 Polygon/MultiPolygon，Polyline 覆盖两种线，等）。</summary>
    public static string FamilyOf(Geometry g) => g switch
    {
        Point => "point",
        MultiPoint => "multipoint",
        Line => "line",
        Polyline => "polyline",
        Polygon => "polygon",
        Envelope => "envelope",
        _ => "unknown",
    };

    public static string GeoJsonTypeFamily(string gjType) => gjType switch
    {
        "Point" => "point",
        "MultiPoint" => "multipoint",
        "LineString" or "MultiLineString" => "polyline",
        "Polygon" or "MultiPolygon" => "polygon",
        _ => "unknown",
    };

    /// <summary>独立坐标抽样遍历（用于 envelope/坐标统计），直接读 GeoJSON 文本，不经被测库。</summary>
    public static IEnumerable<(double X, double Y)> EnumerateCoordsFromJson(string geometryJson)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(geometryJson)!.AsObject();
        var type = node["type"]!.GetValue<string>();
        foreach (var v in Coords(node["coordinates"]!, type))
        {
            var a = v.AsArray();
            yield return (a[0]!.GetValue<double>(), a[1]!.GetValue<double>());
        }
    }

    private static IEnumerable<System.Text.Json.Nodes.JsonArray> Coords(System.Text.Json.Nodes.JsonNode? c, string type)
    {
        var arr = c!.AsArray();
        switch (type)
        {
            case "Point": yield return arr; break;
            case "MultiPoint":
            case "LineString":
                foreach (var e in arr) yield return e!.AsArray();
                break;
            case "Polygon":
            case "MultiLineString":
                foreach (var ring in arr) foreach (var e in ring!.AsArray()) yield return e!.AsArray();
                break;
            case "MultiPolygon":
                foreach (var poly in arr) foreach (var ring in poly!.AsArray()) foreach (var e in ring!.AsArray()) yield return e!.AsArray();
                break;
            default: throw new NotSupportedException(type);
        }
    }

    /// <summary>把语料文件批量装进 PostGIS 表（geom_test_ 前缀，登记待清理）。返回表名。</summary>
    public static string Upload(PgClient pg, CorpusFile file)
    {
        var table = pg.TrackTable($"geom_test_{Sanitize(file.Name)}");
        pg.Exec($"DROP TABLE IF EXISTS {table}; CREATE TABLE {table}(id int PRIMARY KEY, g geometry(Geometry,4326))");
        pg.UploadRows(table, file.Features.Select(f => (f.Index, f.GeometryJson)));
        pg.Exec($"CREATE INDEX ON {table} USING gist(g)");
        return table;
    }

    public static string Sanitize(string name)
    {
        var stem = Path.GetFileNameWithoutExtension(name);
        return new string(stem.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray()).ToLowerInvariant();
    }
}
