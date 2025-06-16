// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Text.Json;
using NesNes.Core;

namespace NesNes.Tests.Model.JsonTests;

/// <summary>
/// Represents a single processor test case from the JSON test data.
/// </summary>
public record CpuTestCase(string Name, CpuState Initial, CpuState Final)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static CpuTestCase[] FromJson(string json)
    {
        return JsonSerializer.Deserialize<CpuTestCase[]>(json, s_jsonOptions) ?? [];
    }

    public override string ToString()
    {
        var cpu = new Cpu(new Registers(), new TestMemory());

        // Program is laid out like [[address, value], [address, value], ...],
        // so select the second item in each row for the program data.
        var program = Initial.Ram.Select(row => (byte)row[1]).ToArray();
        var disassembly = cpu.Disassemble(program);

        return $"""
            Test: {Name}
            ----

            Starting state
            ----
            Registers:
            {Initial.GetRegisters()}

            Memory:
            {MemoryToString(Initial.Ram)}

            Disassembly:
            {string.Join(Environment.NewLine, disassembly)}

            Expected state
            ----
            Registers:
            {Final.GetRegisters()}

            Memory:
            {MemoryToString(Final.Ram)}

            """;

        static string MemoryToString(int[][] memory)
        {
            static string FormatValue(int b) => b.ToString("X2");
            static string FormatAddress(int address) => address.ToString("X4");

            var lines = memory.Select(row => $"{FormatAddress(row[0])}: {FormatValue(row[1])}");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
