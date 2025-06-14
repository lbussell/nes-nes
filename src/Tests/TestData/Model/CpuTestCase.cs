using System.Text.Json;

namespace NesNes.Tests.TestData.Model;

/// <summary>
/// Represents a single processor test case from the JSON test data.
/// </summary>
public record CpuTestCase(
    string Name,
    CpuState Initial,
    CpuState Final)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static CpuTestCase[] FromJson(string json)
    {
        return JsonSerializer.Deserialize<CpuTestCase[]>(json, s_jsonOptions) ?? [];
    }
}
