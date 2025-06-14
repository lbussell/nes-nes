using NesNes.Core;
using NesNes.Tests.TestData.Model;
using Shouldly;

namespace NesNes.Tests;

/// <summary>
/// Test harness for running 6502 processor tests using JSON test data.
/// </summary>
public class CpuJsonTests
{
    /// <summary>
    /// Gets test data for all JSON files in the TestData/json directory.
    /// Each item represents one JSON file containing processor tests.
    /// </summary>
    public static IEnumerable<TheoryDataRow<string>> GetTestData()
    {
        var testDataPath = Path.Combine("TestData", "json");
        if (!Directory.Exists(testDataPath))
        {
            yield break;
        }

        var jsonFiles = Directory.GetFiles(testDataPath, "*.json")
            .OrderBy(f => f)
            .ToArray();

        foreach (var file in jsonFiles)
        {
            yield return new(file);
        }
    }

    /// <summary>
    /// Runs all processor tests from a single JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON test file</param>
    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task OpcodeTest(string filePath)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var testsJson = await File.ReadAllTextAsync(filePath, cancellationToken);
        var testCases = CpuTestCase.FromJson(testsJson);

        foreach (var testCase in testCases)
        {
            RunTestCase(testCase);
        }
    }

    /// <summary>
    /// Runs a single processor test case.
    /// </summary>
    /// <param name="testCase">The test case to run</param>
    private static void RunTestCase(CpuTestCase testCase)
    {
        // Setup test memory and CPU
        var memory = new TestMemory();
        memory.Initialize(testCase.Initial.Ram);

        var initialRegisters = testCase.Initial.GetRegisters();
        var cpu = new Cpu(initialRegisters, memory);

        cpu.RunSteps(1);

        cpu.Registers.ShouldBe(testCase.Final.GetRegisters(),
            $"Test case '{testCase.Name}' failed: Registers do not match.");
        memory.GetMemorySnapshot().ShouldBe(testCase.Final.Ram);
    }
}
