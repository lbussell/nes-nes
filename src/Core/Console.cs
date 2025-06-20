// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class Console(Cpu cpu, Memory memory)
{
    public const decimal CpuCyclesPerFrame = 29780.5m;
    public const decimal CpuCyclesPerScanLine = 113.667m;

    private readonly Cpu _cpu = cpu;
    private readonly Memory _memory = memory;

    /// <summary>
    /// Create a new instance of <see cref="Console"/>.
    /// </summary>
    public static Console Create(Action<Cpu, IMemory>? onCpuInstructionCompleted = null)
    {
        var memory = new Memory();
        var cpu = new Cpu(Registers.Initial, memory, onCpuInstructionCompleted);
        return new Console(cpu, memory);
    }

    public void InsertCartridge(Cartridge rom)
    {
        _memory.LoadRom(rom.Data);
        Reset();
    }

    public void Reset()
    {
        _cpu.Reset();
    }

    /// <summary>
    /// Executes one CPU instruction.
    /// </summary>
    /// <returns>
    /// The number of CPU cycles elapsed. Most instructions take 2-7 cycles.
    /// </returns>
    public int StepCpu()
    {
        return _cpu.Step();
    }
}
