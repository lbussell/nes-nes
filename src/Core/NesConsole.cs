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
    private readonly Bus _bus;
    private readonly Controllers _controllers = new();

    private CartridgeData? _cartridge = null;

    private int _ppuCyclesForThisScanline = 0;

    public Cpu Cpu => _cpu;

    public Ppu Ppu => _ppu;

    public NesConsole(Cpu cpu, Ppu ppu, Bus bus)
    {
        _cpu = cpu;
        _ppu = ppu;
        _bus = bus;
    }

    /// <summary>
    /// Create a new instance of <see cref="NesConsole"/>.
    /// </summary>
    public NesConsole(
        RenderPixel? renderPixelCallback = null,
        CpuCallback? logCpuState = null,
        ReadControllers? readControllers = null
    )
    {
        _ppu = new Ppu();

        _bus = new Bus()
        {
            Ppu = _ppu,
        };

        var registers = Registers.Initial;

        _cpu = new Cpu(
            registers: registers,
            bus: _bus,
            logCpuState: logCpuState,
            tickCallback: () => Tick()
        );
        _bus.TickCpu = _cpu.Tick;

        // The CPU checks some pins on the PPU to determine if an NMI is pending.
        _cpu.CheckNmiPins = () => _ppu.NmiInterrupt;

        // This function is called whenever the PPU wants to render a pixel.
        _ppu.RenderPixelCallback = renderPixelCallback;

        if (readControllers is not null)
        {
            _controllers.ReadControllers = readControllers;
        }
    }

    public int CpuCycles => _cpu.Cycles;

    public CartridgeData? Cartridge => _cartridge;

    public bool HasCartridge => _cartridge is not null;

    public void InsertCartridge(CartridgeData cartridge)
    {
        _cartridge = cartridge;
        var mapper = MapperFactory.Create(_cartridge);

        _ppu.Mapper = mapper;
        _bus.Mapper = mapper;

        Reset();
    }

    public void Reset()
    {
        _cpu.Reset();

        // The CPU spends a number of cycles during reset, so the PPU needs to
        // catch up. The PPU also spends some extra cycles somewhere during
        // reset, but who knows where that's from.
        _ppu.Step(7 * PpuConsts.CyclesPerCpuCycle);
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
        var elapsedPpuCycles = elapsedCpuCycles * PpuConsts.CyclesPerCpuCycle;

        _ppuCyclesForThisScanline += elapsedPpuCycles;
        if (_ppuCyclesForThisScanline >= PpuConsts.CyclesPerScanline)
        {
            // We have completed a scanline, so we should reset the cycle count
            // for the next scanline.
            _ppuCyclesForThisScanline -= PpuConsts.CyclesPerScanline;
        }
    }

    public void StepScanline()
    {
        int cpuCyclesSinceLastScanline = 0;

        while (_ppuCyclesForThisScanline < PpuConsts.CyclesPerScanline)
        {
            int elapsedCpuCycles = _cpu.Step();
            cpuCyclesSinceLastScanline += elapsedCpuCycles;

            int elapsedPpuCycles = elapsedCpuCycles * PpuConsts.CyclesPerCpuCycle;
            _ppuCyclesForThisScanline += elapsedPpuCycles;
        }

        // We completed one scanline, but we may have over-shot the target
        // number of cycles. If we did any extra work, we should make sure not
        // to count that towards the next scanline.
        _ppuCyclesForThisScanline -= PpuConsts.CyclesPerScanline;
    }

    public void StepFrame()
    {
        for (int i = 0; i < PpuConsts.Scanlines; i += 1)
        {
            StepScanline();
        }
    }

    private void Tick(int cycles = 1)
    {
        _ppu.Step(cycles * PpuConsts.CyclesPerCpuCycle);
    }
}
