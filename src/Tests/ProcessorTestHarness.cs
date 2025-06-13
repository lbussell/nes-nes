using System.Text.Json;
using NesNes.Core;
using Shouldly;

namespace NesNes.Tests;

/// <summary>
/// Test harness for running 6502 processor tests using JSON test data.
/// </summary>
public class ProcessorTestHarness
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new BusCycleConverter() }
    };

    /// <summary>
    /// Gets test data for all JSON files in the TestData/json directory.
    /// Each item represents one JSON file containing processor tests.
    /// </summary>
    public static IEnumerable<object[]> GetTestFiles()
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
            var fileName = Path.GetFileNameWithoutExtension(file);
            yield return new object[] { fileName, file };
        }
    }

    /// <summary>
    /// Runs all processor tests from a single JSON file.
    /// </summary>
    /// <param name="opcode">The opcode being tested (for display purposes)</param>
    /// <param name="filePath">Path to the JSON test file</param>
    [Theory]
    [MemberData(nameof(GetTestFiles))]
    public void RunProcessorTests(string opcode, string filePath)
    {        // Skip tests that don't exist or are empty
        if (!File.Exists(filePath))
        {
            Assert.Fail($"Test file not found: {filePath}");
            return;
        }

        var jsonContent = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            Assert.Fail($"Test file is empty: {filePath}");
            return;
        }

        ProcessorTestCase[] testCases;
        try
        {
            testCases = JsonSerializer.Deserialize<ProcessorTestCase[]>(jsonContent, JsonOptions)
                ?? Array.Empty<ProcessorTestCase>();
        }
        catch (JsonException ex)
        {
            Assert.Fail($"Failed to deserialize test file {filePath}: {ex.Message}");
            return;
        }

        var failedTests = new List<string>();
        var totalTests = testCases.Length;
        var passedTests = 0;

        foreach (var testCase in testCases)
        {
            try
            {
                RunSingleTest(testCase);
                passedTests++;
            }
            catch (NotImplementedException exception)
            {
                Assert.Fail($"Not implemented: {exception.Message} in test '{testCase.Name}'");
            }
            catch (Exception exception)
            {
                failedTests.Add($"Test '{testCase.Name}': {exception.Message}");
            }
        }

        // Report results
        if (failedTests.Count > 0)
        {
            var failureMessage = $"Opcode {opcode}: {failedTests.Count}/{totalTests} tests failed:\n" +
                               string.Join("\n", failedTests.Take(10)); // Limit to first 10 failures

            if (failedTests.Count > 10)
            {
                failureMessage += $"\n... and {failedTests.Count - 10} more failures";
            }

            Assert.Fail(failureMessage);
        }

        // All tests passed
        Assert.True(true, $"Opcode {opcode}: All {totalTests} tests passed!");
    }

    /// <summary>
    /// Runs a single processor test case.
    /// </summary>
    /// <param name="testCase">The test case to run</param>
    private static void RunSingleTest(ProcessorTestCase testCase)
    {
        // Setup test memory and CPU
        var memory = new TestMemory();
        memory.Initialize(testCase.Initial.Ram);

        var initialRegisters = new Registers
        {
            PC = testCase.Initial.PC,
            SP = testCase.Initial.S,
            A = testCase.Initial.A,
            X = testCase.Initial.X,
            Y = testCase.Initial.Y,
            P = (Flags)testCase.Initial.P
        };

        var cpu = new Cpu(initialRegisters, memory);
        cpu.RunSteps(1);
        VerifyFinalState(cpu.Registers, memory, testCase.Final, testCase.Name);
    }

    /// <summary>
    /// Verifies that the CPU and memory state matches the expected final state.
    /// </summary>
    private static void VerifyFinalState(Registers actualRegisters, TestMemory actualMemory,
        ProcessorState expectedState, string testName)
    {
        // Verify registers
        actualRegisters.PC.ShouldBe(expectedState.PC, $"PC mismatch in test {testName}");
        actualRegisters.SP.ShouldBe(expectedState.S, $"SP mismatch in test {testName}");
        actualRegisters.A.ShouldBe(expectedState.A, $"A mismatch in test {testName}");
        actualRegisters.X.ShouldBe(expectedState.X, $"X mismatch in test {testName}");
        actualRegisters.Y.ShouldBe(expectedState.Y, $"Y mismatch in test {testName}");
        ((byte)actualRegisters.P).ShouldBe(expectedState.P, $"P mismatch in test {testName}");

        // Verify memory
        var actualMemorySnapshot = actualMemory.GetMemorySnapshot();
        var expectedMemorySnapshot = expectedState.Ram.OrderBy(entry => entry[0]).ToArray();

        actualMemorySnapshot.Length.ShouldBe(expectedMemorySnapshot.Length,
            $"Memory snapshot length mismatch in test {testName}");

        for (int i = 0; i < actualMemorySnapshot.Length; i++)
        {
            var actual = actualMemorySnapshot[i];
            var expected = expectedMemorySnapshot[i];

            actual[0].ShouldBe(expected[0], $"Memory address mismatch at index {i} in test {testName}");
            actual[1].ShouldBe(expected[1], $"Memory value mismatch at address 0x{expected[0]:X4} in test {testName}");
        }
    }
}
