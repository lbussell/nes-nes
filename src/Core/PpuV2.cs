// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public interface IPpu : ICpuReadable, ICpuWritable
{
    long Frame { get; }
    int Scanline { get; }
    int Cycle { get; }

    bool NonMaskableInterruptPin { get; }

    IMapper? Mapper { get; set; }

    public RenderPixel? OnRenderPixel { get; set; }

    void WriteOam(byte address, byte value);

    void Step();

    public void Step(int cycles)
    {
        for (int i = 0; i < cycles; i += 1)
        {
            Step();
        }
    }
}

public class PpuV2 : IPpu
{
    public const int NumScanlines = 262;
    public const int NumCycles = 341;

    private long _frame;
    private int _scanline;
    private int _cycle;

    public long Frame => _frame;
    public int Scanline => _scanline;
    public int Cycle => _cycle;

    public IMapper? Mapper { get; set; } = null;
    public bool NonMaskableInterruptPin { get; }

    public RenderPixel? OnRenderPixel { get; set; }

    public byte CpuRead(ushort address)
    {
        return 0;
    }

    public void CpuWrite(ushort address, byte value)
    {
    }

    public void WriteOam(byte address, byte value)
    {
    }

    public void Step()
    {
        var debugColor = DebugColors.Blank;

        // This method should closely follow the PPU timing diagram at
        // https://www.nesdev.org/w/images/default/4/4f/Ppu.svg

        if ((_frame & 0b1) == 0 && _cycle == 0 && _scanline == 0)
        {
            // On even frames, skip the first pixel of the first scanline.
            _cycle += 1;
            debugColor = DebugColors.Misc;
        }

        if (_scanline == 261 || _scanline < 240)
        {
            if ((_cycle > 0 && _cycle < 261) || (_cycle >= 321 && _cycle < 341))
            {
                switch (_cycle % 8)
                {
                    case 0:
                        debugColor = DebugColors.VUpdate;
                        break;
                    case <= 2:
                        debugColor = DebugColors.NameTableFetch;
                        break;
                    case <= 4:
                        debugColor = DebugColors.AttributeFetch;
                        break;
                    case <= 6:
                        debugColor = DebugColors.BackgroundLowFetch;
                        break;
                    case 7:
                        debugColor = DebugColors.BackgroundHighFetch;
                        break;
                }

                if (_cycle == 256)
                {
                    // Increment vertical scroll
                    debugColor = DebugColors.VUpdate;
                }

                if (_cycle % 8 == 0)
                {
                    // Increment coarse X scroll
                    debugColor = DebugColors.VUpdate;
                }

                if (_cycle == 257)
                {
                    // Transfer horizontal components of T to V
                    debugColor = DebugColors.VUpdate;
                }
            }
        }

        if (_scanline == 241 && _cycle == 1)
        {
            // Set VBlank flag
            debugColor = DebugColors.Flag;
        }

        if (_scanline == 261)
        {
            if (_cycle == 1)
            {
                // Clear VBlank flag and sprite 0 overflow
                debugColor = DebugColors.Flag;
            }

            if (_cycle >= 280 && _cycle <= 304)
            {
                // Copy vertical components of T to V
                debugColor = DebugColors.VUpdate;
            }
        }

        Output(debugColor);

        // Move on to the next cycle/scanline
        _cycle += 1;
        if (_cycle >= NumCycles)
        {
            _cycle = 0;
            _scanline += 1;
            if (_scanline >= NumScanlines)
            {
                _scanline = 0;
            }
        }
    }

    private void Output(Color color)
    {
        OnRenderPixel?.Invoke(
            (ushort)_cycle,
            (ushort)_scanline,
            color.R,
            color.G,
            color.B
        );
    }
}
