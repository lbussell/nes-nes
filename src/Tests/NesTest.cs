// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Globalization;
using NesNes.Core;
using Shouldly;

namespace NesNes.Tests;

public class NesTest
{
    public const string TestRom = "TestData/nestest/nestest.nes";

    public const string TestLog = "TestData/nestest/nestest.log.txt";

    private int _cpuCycles = 0;

    private readonly List<LogLine> _log = [];

    /// <summary>
    /// Load nestest.nes. Run tests in "automated mode" by starting execution
    /// at address $C000. Compare the resulting traces to nestest.log.txt.
    /// </summary>
    [Fact]
    public async Task NesTestLog()
    {
        // Stop at the first illegal instruction, because we don't care about
        // those for the time being.
        const ushort StoppingPoint = 5004;

        // Load the test ROM
        var nesTestRomData = await File.ReadAllBytesAsync(
            TestRom,
            TestContext.Current.CancellationToken
        );

        // Read and parse nestest log file
        var testLogText = await File.ReadAllLinesAsync(
            TestLog,
            TestContext.Current.CancellationToken
        );
        var expected = TextToLogLines(testLogText).ToArray();

        // Final setup
        var console = CreateConsole(nesTestRomData, initialPc: 0xC000);
        var comparer = new LogLineEqualityComparer();

        // Execute the ROM up to the stopping point, checking CPU logs as we
        // go. If the CPU log fails to match the expected output, the test will
        // fail immediately.
        for (int i = 0; i < StoppingPoint - 1; i += 1)
        {
            _cpuCycles += console.StepCpuOnly();

            _log.Last()
                .ShouldBe(
                    expected[i],
                    comparer,
                    $"""

                    Mismatch at line {i + 1}

                    """
                );
        }
    }

    private NesConsole CreateConsole(byte[] nesTestRom, ushort initialPc)
    {
        // Construct the console
        var ppu = new PpuV2();
        var memory = new Bus()
        {
            Ppu = ppu
        };
        var cpu = new Cpu(Registers.Initial, memory, Trace);
        var console = new NesConsole(cpu, ppu, memory);

        var cartridge = new CartridgeData(new MemoryStream(nesTestRom));
        console.InsertCartridge(cartridge);

        cpu.Registers = cpu.Registers with { PC = initialPc };

        return console;
    }

    /// <summary>
    /// CPU instruction trace callback. This is used to build up the test log
    /// while we run the nestest rom.
    /// </summary>
    private void Trace(ushort PC, Registers registers)
    {
        var logLine = new LogLine(
            PC: PC,
            Bytes: [],
            Instruction: "",
            A: registers.A,
            X: registers.X,
            Y: registers.Y,
            P: (byte)registers.P,
            SP: registers.SP,
            CpuCycles: _cpuCycles
        );

        _log.Add(logLine);
    }

    /// <summary>
    /// Convert the whole test log into a LogLine collection.
    /// </summary>
    private IEnumerable<LogLine> TextToLogLines(IEnumerable<string> textLog) =>
        textLog.Select(TextToLogLine);

    /// <summary>
    /// Convert a single line of text from nestest.log.txt into a LogLine
    /// object
    /// </summary>
    private LogLine TextToLogLine(string textLine)
    {
        /*
        Log lines look like this:
        C000  4C F5 C5  JMP $C5F5                       A:00 X:00 Y:00 P:24 SP:FD PPU:  0, 21 CYC:7
        C5F5  A2 00     LDX #$00                        A:00 X:00 Y:00 P:24 SP:FD PPU:  0, 30 CYC:10
        C5F7  86 00     STX $00 = 00                    A:00 X:00 Y:00 P:26 SP:FD PPU:  0, 36 CYC:12
        C5F9  86 10     STX $10 = 00                    A:00 X:00 Y:00 P:26 SP:FD PPU:  0, 45 CYC:15
        C5FB  86 11     STX $11 = 00                    A:00 X:00 Y:00 P:26 SP:FD PPU:  0, 54 CYC:18
        */

        var textSpan = textLine.AsSpan();

        // TODO: Parse the instruction bytes.
        byte[] bytes = [];

        return new LogLine(
            PC: ushort.Parse(textSpan[..4], NumberStyles.HexNumber),
            Bytes: bytes,
            Instruction: textSpan[17..48].Trim().ToString(),
            A: byte.Parse(textSpan[50..52], NumberStyles.HexNumber),
            X: byte.Parse(textSpan[55..57], NumberStyles.HexNumber),
            Y: byte.Parse(textSpan[60..62], NumberStyles.HexNumber),
            P: byte.Parse(textSpan[65..67], NumberStyles.HexNumber),
            SP: byte.Parse(textSpan[71..73], NumberStyles.HexNumber),
            CpuCycles: int.Parse(textSpan[90..])
        );
    }

    /// <summary>
    /// Represents a single line of a nestest log.
    /// </summary>
    private readonly record struct LogLine(
        ushort PC,
        byte[] Bytes,
        string Instruction,
        byte A,
        byte X,
        byte Y,
        byte P,
        byte SP,
        // TODO: Add PPU
        int CpuCycles
    )
    {
        public override string ToString() =>
            $"{PC:X4}  "
            +
            // $"{string.Join(" ", Bytes.Select(b => b.ToString("X2"))),-8}  " +
            // $"{Instruction,-30}  " +
            $"A:{A:X2} X:{X:X2} Y:{Y:X2} P:{P:X2} SP:{SP:X2} CYC:{CpuCycles}";
    }

    /// <summary>
    /// Custom comparer for <see cref="LogLine"/> objects. This is useful to
    /// compare specific properties of the Log without comparing the whole
    /// thing, since we are implementing only one part of the system at a time.
    /// </summary>
    private class LogLineEqualityComparer : IEqualityComparer<LogLine>
    {
        public bool Equals(LogLine object1, LogLine object2) =>
            object1.PC == object2.PC
            && object1.A == object2.A
            && object1.X == object2.X
            && object1.Y == object2.Y
            && object1.P == object2.P
            && object1.SP == object2.SP;

        public int GetHashCode(LogLine logLine)
        {
            var hash = new HashCode();

            hash.Add(logLine.PC);
            hash.Add(logLine.A);
            hash.Add(logLine.X);
            hash.Add(logLine.Y);
            hash.Add(logLine.P);
            hash.Add(logLine.SP);

            return hash.ToHashCode();
        }
    }
}
