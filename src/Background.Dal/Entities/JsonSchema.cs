using System.Text.Json;
using System.Text.Json.Serialization;

namespace Background.Dal.Entities;

[JsonConverter(typeof(JsonSchemaConverter))]
public readonly record struct JsonSchema
{
    public string Raw { get; }

    public JsonSchema(string raw) => Raw = raw;

    public JsonSchema(JsonDocument doc) => Raw = doc.RootElement.GetRawText();

    public override string ToString() => Raw;

    public string? Validate()
    {
        try
        {
            JsonDocument.Parse(Raw);
            return null;
        }
        catch (JsonException ex)
        {
            return $"ResponseSchema is not valid JSON: {ex.Message}";
        }
    }
}

internal sealed class JsonSchemaConverter : JsonConverter<JsonSchema>
{

    public override JsonSchema Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new JsonSchema(reader.GetString()!);

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return new JsonSchema(doc);
        }

        throw new JsonException("ResponseSchema must be a JSON string or object");
    }

    public override void Write(Utf8JsonWriter writer, JsonSchema value, JsonSerializerOptions options)
    {
        try
        {
            using var doc = JsonDocument.Parse(value.Raw);
            doc.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            writer.WriteStringValue(value.Raw);
        }
    }
}
