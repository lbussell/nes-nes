// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using NesNes.Tests.Model.JsonTests;
using Shouldly;

namespace NesNes.Tests;

/// <summary>
/// Test harness for running 6502 processor tests using JSON test data.
/// </summary>
public class CpuJsonTests
{
    private static readonly string s_testDataDirectory = Path.Combine("TestData", "json");

    /// <summary>
    /// Gets test data for all JSON files in the TestData/json directory.
    /// Each item represents one JSON file containing processor tests.
    /// </summary>
    public static IEnumerable<TheoryDataRow<string>> GetTestData()
    {
        if (!Directory.Exists(s_testDataDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Cannot find test data directory '{s_testDataDirectory}'."
            );
        }

        var opcodes = new Cpu(new Registers(), new DictionaryMemory())
            .GetSupportedOpcodes()
            .Select(o => o.ToString("X2"));

        var jsonOpcodes = Directory
            .GetFiles(s_testDataDirectory, "*.json")
            .Select(filePath => Path.GetFileNameWithoutExtension(filePath));

        var opcodePaths = opcodes.Select(opcode =>
            Path.Combine(s_testDataDirectory, $"{opcode}.json")
        );

        var unsupportedOpcodes = jsonOpcodes.Except(opcodes, StringComparer.OrdinalIgnoreCase);
        if (unsupportedOpcodes.Any())
        {
            TestContext.Current.AddWarning(
                $"""
                Unsupported opcodes:

                {string.Join(Environment.NewLine, unsupportedOpcodes)}

                """
            );
        }

        return opcodePaths.Select(filePath => new TheoryDataRow<string>(filePath));
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
        var memory = new DictionaryMemory();
        memory.Initialize(testCase.Initial.Ram);

        var initialRegisters = testCase.Initial.GetRegisters();
        var cpu = new Cpu(initialRegisters, memory);

        try
        {
            cpu.Step();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"""
                Test case '{testCase.Name}' failed: {e.Message}

                {testCase}

                """,
                e
            );
        }

        cpu.Registers.ShouldBe(
            testCase.Final.GetRegisters(),
            $"""
            Test case '{testCase.Name}' failed: Registers do not match.
            """
        );

        memory
            .GetMemorySnapshot()
            .ShouldBe(
                testCase.Final.Ram,
                $"Test case '{testCase.Name}' failed: Memory does not match."
            );
    }
}
