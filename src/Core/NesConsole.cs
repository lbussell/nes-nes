// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class NesConsole
{
    public const decimal CpuCyclesPerFrame = 29780.5m;
    public const decimal CpuCyclesPerScanline = 113.667m;
    public const int ApproxCpuCyclesPerFrame = 29781;
    public const int ApproxCpuCyclesPerScanline = 113;

    private readonly Cpu _cpu;
    private readonly Ppu _ppu;
    private readonly Memory _memory;
    private CartridgeData? _cartridge = null;

    private int _ppuCyclesForThisScanline = 0;

    public Cpu Cpu => _cpu;

    public Ppu Ppu => _ppu;

    public NesConsole(Cpu cpu, Ppu ppu, Memory memory)
    {
        _cpu = cpu;
        _ppu = ppu;
        _memory = memory;
    }

    /// <summary>
    /// Create a new instance of <see cref="NesConsole"/>.
    /// </summary>
    public NesConsole(
        RenderPixel? renderPixelCallback = null,
        CpuCallback? logCpuState = null
    )
    {
        _ppu = new Ppu();
        _memory = new Memory([_ppu]);
        var registers = Registers.Initial;
        _cpu = new Cpu(registers, _memory, logCpuState, () => Tick());

        _ppu.RenderPixelCallback = renderPixelCallback;
        _ppu.NmiCallback = _cpu.QueueNonMaskableInterrupt;
    }

    public int CpuCycles => _cpu.Cycles;

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

        // The CPU spends a number of cycles during reset, so the PPU needs to
        // catch up. The PPU also spends some extra cycles somewhere during
        // reset, but who knows where that's from.
        _ppu.Step(7 * Ppu.CyclesPerCpuCycle);
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
        return elapsedCycles;
    }

    /// <summary>
    /// Executes one CPU instruction and runs the PPU to catch up.
    /// </summary>
    public void StepInstruction()
    {
        var elapsedCpuCycles = _cpu.Step();
        var elapsedPpuCycles = elapsedCpuCycles * Ppu.CyclesPerCpuCycle;

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
            int elapsedCpuCycles = _cpu.Step();
            cpuCyclesSinceLastScanline += elapsedCpuCycles;

            int elapsedPpuCycles = elapsedCpuCycles * Ppu.CyclesPerCpuCycle;
            _ppuCyclesForThisScanline += elapsedPpuCycles;
        }

        // We completed one scanline, but we may have over-shot the target
        // number of cycles. If we did any extra work, we should make sure not
        // to count that towards the next scanline.
        _ppuCyclesForThisScanline -= Ppu.CyclesPerScanline;
    }

    private void Tick(int cycles = 1)
    {
        _ppu.Step(cycles * Ppu.CyclesPerCpuCycle);
    }
}
