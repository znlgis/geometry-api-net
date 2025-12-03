using System.Text.Json;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Json.Converters;

namespace OpenGIS.Esri.Geometry.Tests.Json;

public class JsonSerializationTests
{
    [Fact]
    public void Point_Serialize_ProducesCorrectJson()
    {
        var point = new Point(10.5, 20.7);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PointJsonConverter());

        var json = JsonSerializer.Serialize(point, options);

        Assert.Contains("\"x\":10.5", json);
        Assert.Contains("\"y\":20.7", json);
    }

    [Fact]
    public void Point_Deserialize_CreatesCorrectPoint()
    {
        var json = "{\"x\":10.5,\"y\":20.7}";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PointJsonConverter());

        var point = JsonSerializer.Deserialize<Point>(json, options);

        Assert.NotNull(point);
        Assert.Equal(10.5, point!.X);
        Assert.Equal(20.7, point.Y);
    }

    [Fact]
    public void Point_WithZ_SerializeAndDeserialize_PreservesZ()
    {
        var originalPoint = new Point(10, 20, 30);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PointJsonConverter());

        var json = JsonSerializer.Serialize(originalPoint, options);
        var deserializedPoint = JsonSerializer.Deserialize<Point>(json, options);

        Assert.NotNull(deserializedPoint);
        Assert.Equal(30, deserializedPoint!.Z);
    }

    [Fact]
    public void Point_RoundTrip_PreservesAllValues()
    {
        var originalPoint = new Point(10.5, 20.7) { Z = 30.1, M = 40.2 };
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PointJsonConverter());

        var json = JsonSerializer.Serialize(originalPoint, options);
        var deserializedPoint = JsonSerializer.Deserialize<Point>(json, options);

        Assert.NotNull(deserializedPoint);
        Assert.Equal(originalPoint.X, deserializedPoint!.X);
        Assert.Equal(originalPoint.Y, deserializedPoint.Y);
        Assert.Equal(originalPoint.Z, deserializedPoint.Z);
        Assert.Equal(originalPoint.M, deserializedPoint.M);
    }
}