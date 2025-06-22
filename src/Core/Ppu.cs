// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public delegate void RenderPixel(ushort x, ushort y, byte r, byte g, byte b);

public class Ppu
{
    /// <summary>
    /// Each scanline lasts for 341 PPU clock cycles. Each cycle produces one
    /// pixel. The first 256 pixels are visible, while the rest is horizontal
    /// overscan.
    /// </summary>
    public const int CyclesPerScanline = 341;

    /// <summary>
    /// The PPU runs 3 cycles for every 1 CPU cycle. Thus, it is more accurate
    /// to describe timing in terms of PPU cycles rather than CPU cycles.
    /// </summary>
    public const int CyclesPerCpuCycle = 3;

    /// <summary>
    /// The PPU renders 262 horizontal scanlines per frame.
    /// </summary>
    public const int Scanlines = 262;

    /// <summary>
    /// Pattern table height in terms of 16x16 tiles.
    /// </summary>
    public const int PatternTableHeight = 16;

    /// <summary>
    /// Pattern table width in terms of 16x16 tiles.
    /// </summary>
    public const int PatternTableWidth = 16;

    /// <summary>
    /// Number of bytes per tile in the pattern table. Each tile is 16x16
    /// pixels and is represented by 16 bytes.
    /// </summary>
    public const int BytesPerTile = 16;

    /// <summary>
    /// The first 240 scanlines are visible on the screen. Scanlines 241-261
    /// are "overscan" and not visible. Upon entering the 241st scanline, the
    /// PPU triggers the VBlank NMI (non-maskable interrupt) on the CPU. The
    /// PPU does not make any memory accesses during the VBlank period.
    /// </summary>
    public const int DisplayHeight = 240;

    /// <summary>
    /// The width of the display in pixels.
    /// </summary>
    public const int DisplayWidth = 256;

    // This is called whenever a pixel is rendered.
    private readonly RenderPixel? _renderPixelCallback;

    /// Temporary - only used for drawing static/noise for now. Can be removed
    /// once the PPU is actually drawing game data to the screen.
    private readonly Random _random = new();

    /// <summary>
    /// Cartridge data which contains the CHR_ROM which is used for tilesets
    /// </summary>
    private CartridgeData? _cartridge;

    // The current PPU cycle (0-340). This also roughly corresponds to which
    // pixel is being drawn on the current scanline.
    private ushort _cycle = 0;

    // The current scanline (0-261).
    private ushort _scanline = 0;

    /// <summary>
    /// Create a new instance of the PPU.
    /// </summary>
    /// <param name="renderPixelCallback">
    /// This callback will be called whenever the PPU renders a pixel.
    /// </param>
    public Ppu(RenderPixel? renderPixelCallback = null)
    {
        _renderPixelCallback = renderPixelCallback;
    }

    /// <summary>
    /// Run the PPU for given number of cycles.
    /// </summary>
    /// <param name="cycles">Number of PPU cycles to advance.</param>
    public void Step(int cycles)
    {
        for (int i = 0; i < cycles; i += 1)
        {
            Step();
        }
    }

    /// <summary>
    /// Load a ROM into the PPU. The PPU needs a reference to the cartridge
    /// since it needs to read CHR_ROM in order to render tile data.
    /// </summary>
    public void LoadRom(CartridgeData cartridge)
    {
        _cartridge = cartridge;
    }

    /// <summary>
    /// Advance the PPU by one cycle.
    /// </summary>
    private void Step()
    {
        _cycle += 1;
        if (_cycle >= CyclesPerScanline)
        {
            _cycle = 0;
            _scanline += 1;
            if (_scanline >= Scanlines)
            {
                _scanline = 0;
            }
        }

        // For now, just draw a random color (white or black).
        // Should look like static/noise.
        var color = _random.Next(0, 2) == 0 ? (byte)0 : (byte)255;

        _renderPixelCallback?.Invoke(_cycle, _scanline, color, color, color);
    }
}
