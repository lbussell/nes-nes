using System.Text.Json;
using System.Text.Json.Serialization;

namespace NesNes.Tests;

/// <summary>
/// Represents a single processor test case from the JSON test data.
/// </summary>
public class ProcessorTestCase
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("initial")]
    public ProcessorState Initial { get; set; } = new();

    [JsonPropertyName("final")]
    public ProcessorState Final { get; set; } = new();

    [JsonPropertyName("cycles")]
    public BusCycle[] Cycles { get; set; } = Array.Empty<BusCycle>();
}

/// <summary>
/// Represents the state of the processor (registers and memory).
/// </summary>
public class ProcessorState
{
    [JsonPropertyName("pc")]
    public ushort PC { get; set; }

    [JsonPropertyName("s")]
    public byte S { get; set; }

    [JsonPropertyName("a")]
    public byte A { get; set; }

    [JsonPropertyName("x")]
    public byte X { get; set; }

    [JsonPropertyName("y")]
    public byte Y { get; set; }

    [JsonPropertyName("p")]
    public byte P { get; set; }

    [JsonPropertyName("ram")]
    public int[][] Ram { get; set; } = Array.Empty<int[]>();
}

/// <summary>
/// Represents a single bus cycle operation.
/// </summary>
public class BusCycle
{
    [JsonPropertyName("address")]
    public ushort Address { get; set; }

    [JsonPropertyName("value")]
    public byte Value { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Custom JSON converter for BusCycle to handle the array format
/// [address, value, type].
/// </summary>
public class BusCycleConverter : JsonConverter<BusCycle>
{
    public override BusCycle Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for BusCycle");

        reader.Read(); // Read address
        var address = (ushort)reader.GetInt32();

        reader.Read(); // Read value
        var value = (byte)reader.GetInt32();

        reader.Read(); // Read type
        var type = reader.GetString() ?? string.Empty;

        reader.Read(); // Read end array
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for BusCycle");

        return new BusCycle { Address = address, Value = value, Type = type };
    }

    public override void Write(
        Utf8JsonWriter writer,
        BusCycle value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Address);
        writer.WriteNumberValue(value.Value);
        writer.WriteStringValue(value.Type);
        writer.WriteEndArray();
    }
}
