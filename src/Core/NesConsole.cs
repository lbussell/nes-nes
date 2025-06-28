// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class NesConsole(Cpu cpu, Ppu ppu, Memory memory)
{
    public const decimal CpuCyclesPerFrame = 29780.5m;
    public const decimal CpuCyclesPerScanline = 113.667m;
    public const int ApproxCpuCyclesPerFrame = 29781;
    public const int ApproxCpuCyclesPerScanline = 113;

    private readonly Cpu _cpu = cpu;
    private readonly Ppu _ppu = ppu;
    private readonly Memory _memory = memory;

    private int _cpuCycles = 0;
    private int _ppuCyclesToRun = 0;
    private int _cpuCyclesSinceLastScanline = 0;

    public Cpu Cpu => _cpu;

    /// <summary>
    /// Create a new instance of <see cref="NesConsole"/>.
    /// </summary>
    public static NesConsole Create(
        RenderPixel? renderPixelCallback = null,
        CpuCallback? onCpuInstructionCompleted = null
    )
    {
        var ppu = new Ppu();
        var memory = new Memory(listeners: [ppu]);
        var registers = Registers.Initial;
        var cpu = new Cpu(registers, memory, onCpuInstructionCompleted);

        ppu.RenderPixelCallback = renderPixelCallback;
        ppu.NmiCallback = cpu.QueueNonMaskableInterrupt;

        return new NesConsole(cpu, ppu, memory);
    }

    public int CpuCycles => _cpuCycles;

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
        var elapsedCycles = _cpu.Step();
        _cpuCycles += elapsedCycles;
        return elapsedCycles;
    }

    public void StepScanline()
    {
        int ppuCyclesToRun = 0;
        int cpuCyclesSinceLastScanline = 0;

        while (ppuCyclesToRun < Ppu.CyclesPerScanline)
        {
            int elapsedCpuCycles = StepCpu();
            _cpuCycles += elapsedCpuCycles;
            cpuCyclesSinceLastScanline += elapsedCpuCycles;
            ppuCyclesToRun += elapsedCpuCycles * Ppu.CyclesPerCpuCycle;
        }

        _cpuCycles += cpuCyclesSinceLastScanline;

        // Run the PPU for the number of cycles we calculated to catch up with
        // the CPU.
        _ppu.Step(ppuCyclesToRun);

        // We completed one scanline, but we may have over-shot the target
        // number of cycles. If we did any extra work, we should make sure not
        // to count that towards the next scanline.
        _ppuCyclesToRun -= Ppu.CyclesPerScanline;
    }
}
