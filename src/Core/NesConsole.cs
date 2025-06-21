// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class NesConsole(Cpu cpu, Ppu ppu, Memory memory)
{
    public const decimal CpuCyclesPerFrame = 29780.5m;
    public const decimal CpuCyclesPerScanLine = 113.667m;

    private readonly Cpu _cpu = cpu;
    private readonly Ppu _ppu = ppu;
    private readonly Memory _memory = memory;

    /// <summary>
    /// Create a new instance of <see cref="NesConsole"/>.
    /// </summary>
    public static NesConsole Create(CpuCallback? onCpuInstructionCompleted = null)
    {
        var memory = new Memory();
        var registers = Registers.Initial;
        var cpu = new Cpu(registers, memory, onCpuInstructionCompleted);
        var ppu = new Ppu();
        return new NesConsole(cpu, ppu, memory);
    }

    public void InsertCartridge(CartridgeData cart)
    {
        _memory.LoadRom(cart);
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
