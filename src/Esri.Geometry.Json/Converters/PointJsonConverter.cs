using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Json.Converters;

/// <summary>
///   Point 几何对象的 JSON 转换器。
/// </summary>
public class PointJsonConverter : JsonConverter<Point>
{
    /// <inheritdoc />
    public override Point? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected start of object");

    var point = new Point();

    while (reader.Read())
    {
      if (reader.TokenType == JsonTokenType.EndObject) return point;

      if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected property name");

      var propertyName = reader.GetString();
      reader.Read();

      switch (propertyName?.ToLowerInvariant())
      {
        case "x":
          point.X = reader.GetDouble();
          break;
        case "y":
          point.Y = reader.GetDouble();
          break;
        case "z":
          if (reader.TokenType != JsonTokenType.Null) point.Z = reader.GetDouble();
          break;
        case "m":
          if (reader.TokenType != JsonTokenType.Null) point.M = reader.GetDouble();
          break;
      }
    }

    throw new JsonException("Unexpected end of JSON");
  }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();
    writer.WriteNumber("x", value.X);
    writer.WriteNumber("y", value.Y);

    if (value.Z.HasValue) writer.WriteNumber("z", value.Z.Value);

    if (value.M.HasValue) writer.WriteNumber("m", value.M.Value);

    writer.WriteEndObject();
  }
}