// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public static class PpuConsts
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

    public const int PatternTableTilesHeight = 16;
    public const int PatternTableTilesWidth = 16;
    public const int PatternTableTilesCount = PatternTableTilesHeight * PatternTableTilesWidth;
    public const int PatternTablePixelWidth = PatternTableTilesWidth * PatternSize;
    public const int PatternTablePixelHeight = PatternTableTilesHeight * PatternSize;
    public const int PatternTableSizeBytes = PatternTableTilesWidth * PatternTableTilesHeight * BytesPerTile;

    /// <summary>
    /// Size of a single pattern in the pattern table, in pixels. Patterns are
    /// square.
    /// </summary>
    public const int PatternSize = 8;

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

    /// <summary>
    /// When the PPU enters this scanline, it triggers an NMI (non-masking
    /// interrupt)
    /// </summary>
    public const int VblankScanline = 241;

    // https://www.nesdev.org/wiki/PPU_memory_map
    public const int PatternTablesEnd = 0x2000;
    public const int NameTablesStart = 0x2000;
    public const int NameTablesEnd = 0x3000;
    public const int NameTableSize = 0x400;
    public const int PaletteRamStart = 0x3F00;
    public const int PaletteRamEnd = 0x4000;

    public const int ChrRomSize = 0x2000; // 8KB
}
