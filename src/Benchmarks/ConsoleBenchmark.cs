// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using BenchmarkDotNet.Attributes;
using NesNes.Core;

namespace NesNes.Benchmarks;

[MemoryDiagnoser]
public class ConsoleBenchmark
{
    private NesConsole? _console;
    private CartridgeData? _cartridge;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var file = Environment.GetEnvironmentVariable("ROM") ??
            throw new InvalidOperationException(
                "ROM environment variable not set"
            );

        var stream = File.OpenRead(file);
        _cartridge = new CartridgeData(stream);
        _console = new NesConsole();
        _console.InsertCartridge(_cartridge);
    }

    [Benchmark]
    public void RunFrame()
    {
        _console!.StepFrame();
    }
}
