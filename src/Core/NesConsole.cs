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
    private CartridgeData? _cartridge = null;

    private int _cpuCycles = 0;
    private int _ppuCyclesForThisScanline = 0;
    private int _cpuCyclesSinceLastScanline = 0;

    public Cpu Cpu => _cpu;

    public Ppu Ppu => _ppu;

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

    public bool HasCartridge => _cartridge is not null;

    public void InsertCartridge(CartridgeData cart)
    {
        _cartridge = cart;
        _memory.LoadRom(cart);
        _ppu.LoadRom(cart);
        Reset();
    }

    public void Reset()
    {
        _cpu.Reset();
    }

    /// <summary>
    /// Executes one CPU instruction without doing anything with the PPU.
    /// </summary>
    /// <returns>
    /// The number of CPU cycles elapsed. Most instructions take 2-7 cycles.
    /// </returns>
    public int StepCpuOnly()
    {
        var elapsedCycles = _cpu.Step();
        _cpuCycles += elapsedCycles;
        return elapsedCycles;
    }

    /// <summary>
    /// Executes one CPU instruction and runs the PPU to catch up.
    /// </summary>
    public void StepInstruction()
    {
        var elapsedCpuCycles = StepCpuOnly();
        var elapsedPpuCycles = elapsedCpuCycles * Ppu.CyclesPerCpuCycle;
        _ppu.Step(elapsedPpuCycles);

        _ppuCyclesForThisScanline += elapsedPpuCycles;
        if (_ppuCyclesForThisScanline >= Ppu.CyclesPerScanline)
        {
            // We have completed a scanline, so we should reset the cycle count
            // for the next scanline.
            _ppuCyclesForThisScanline -= Ppu.CyclesPerScanline;
        }
    }

    public void StepScanline()
    {
        int cpuCyclesSinceLastScanline = 0;

        while (_ppuCyclesForThisScanline < Ppu.CyclesPerScanline)
        {
            int elapsedCpuCycles = StepCpuOnly();
            cpuCyclesSinceLastScanline += elapsedCpuCycles;

            // Run the PPU for the number of cycles we calculated to catch up
            // with the CPU.
            int elapsedPpuCycles = elapsedCpuCycles * Ppu.CyclesPerCpuCycle;
            _ppu.Step(elapsedPpuCycles);
            _ppuCyclesForThisScanline += elapsedPpuCycles;
        }

        // We completed one scanline, but we may have over-shot the target
        // number of cycles. If we did any extra work, we should make sure not
        // to count that towards the next scanline.
        _ppuCyclesForThisScanline -= Ppu.CyclesPerScanline;
    }
}
