// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public delegate void RenderPixel(ushort x, ushort y, byte r, byte g, byte b);

internal static class PpuConsts
{
    public const int ChrRomSize = 0x2000; // 8KB
}

/// <summary>
/// The NES's PPU is what renders the graphics on the screen.
/// </summary>
/// <remarks>
/// The PPU has its own memory space separate from the CPU's memory. The CPU
/// does not have direct read/write access to the PPU's memory. Instead, the
/// CPU can read/write to the PPU's memory by talking to the PPU's registers.
/// The PPU's registers are mapped to $2000-$2007 (and mirrored all the way up
/// to $4000) on the main memory bus.
/// <remarks/>
public class Ppu : IMemoryListener
{
    #region Constants

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

    // The following are indices into the _registers memory array
    private const int PpuCtrl = 0;
    private const int PpuStatus = 2;
    private const int OamAddress = 3;
    private const int OamData = 4;
    private const int PpuAddress = 6;
    private const int PpuData = 7;

    // https://www.nesdev.org/wiki/PPU_memory_map
    private const int PatternTablesEnd = 0x2000;
    private const int NameTablesEnd = 0x3000;
    private const int NameTableSize = 0x400;
    private const int PaletteRamStart = 0x3F00;
    private const int PaletteRamEnd = 0x4000;

    #endregion

    private readonly PpuAddrRegister _addressRegister = new();
    private readonly byte[] _registers = new byte[MemoryRegions.PpuRegistersSize];
    private readonly IMemory _nameTables;
    private ReadOnlySpan<byte> _patternTables =>
        _cartridge is not null ? _cartridge.ChrRom : throw new InvalidOperationException();
    private readonly byte[] _paletteRam = new byte[0x20];
    private readonly byte[] _oamData = new byte[0x100];

    // Cartridge data which contains the CHR_ROM which is used for tilesets
    private CartridgeData? _cartridge;

    // The current PPU cycle (0-340). This also roughly corresponds to which
    // pixel is being drawn on the current scanline.
    private ushort _cycle = 0;

    // The current scanline (0-261).
    private ushort _scanline = 0;

    // Data buffer used for delaying reads of PPU memory
    private byte _dataBuffer;

    // The open bus is loaded with data whenever reads or writes are made to
    // the PPU's registers. It is also mapped to bits 0-4 of the PPU status
    // register.
    private byte _openBus;

    /// <summary>
    /// Creates a new instance of the PPU.
    /// </summary>
    /// <param name="initialNametables"></param>
    public Ppu(IMemory? initialNametables = null)
    {
        _nameTables = initialNametables ?? new SimpleMemory(2 * NameTableSize);
    }

    public int Scanline => _scanline;

    public int Cycle => _cycle;

    public ReadOnlySpan<byte> PatternTables => _patternTables;

    public IMemory NameTables => _nameTables;

    /// <summary>
    /// This is called whenever a pixel is rendered.
    /// </summary>
    public RenderPixel? RenderPixelCallback { get; set; }

    /// <summary>
    /// The memory range that the PPU is interested in intercepting reads and
    /// writes to. While the PPU is interested in a large range of memory
    /// (0x2000 to 0x3FFF), it only uses 8 bytes for PPU registers. The rest of
    /// the PPU's memory just repeats the PPU registers every 8 bytes.
    /// </summary>
    public MemoryRange MemoryRange { get; } =
        new(MemoryRegions.PpuRegisters, MemoryRegions.PpuRegistersEnd);

    private bool NmiEnabled
    {
        get => GetRegisterBit(PpuCtrl, 1 << 7);
        set => SetRegisterBit(PpuCtrl, 1 << 7, value);
    }

    public bool VblankFlag
    {
        get => GetRegisterBit(PpuStatus, 1 << 7);
        set => SetRegisterBit(PpuStatus, 1 << 7, value);
    }

    public bool NmiInterrupt => NmiEnabled && VblankFlag;

    /// <summary>
    /// Indicates whether the PPU should increment the address register by 1
    /// byte or by 32 bytes (one row of the nametable) when the address
    /// register is read. True = increment by 32, false = increment by 1.
    /// </summary>
    private bool IncrementMode
    {
        get => GetRegisterBit(PpuCtrl, 1 << 2);
        set => SetRegisterBit(PpuCtrl, 1 << 2, value);
    }

    /// <inheritdoc/>
    public bool ListenRead(ushort address, out byte value)
    {
        address = MapToMirroredRegisterAddress(address);
        value = ReadInternalRegister(address);
        return true;
    }

    /// <inheritdoc/>
    public bool ListenWrite(ushort address, byte value)
    {
        address = MapToMirroredRegisterAddress(address);
        WriteInternalRegister(address, value);
        return true;
    }

    public void WriteOam(byte address, byte value)
    {
        _oamData[address] = value;
    }

    /// <summary>
    /// Maps the given address to the appropriate PPU register address, taking
    /// into account the mirroring of PPU registers as well as the location of
    /// the PPU registers in memory.
    /// </summary>
    /// <param name="address">
    /// The address to map, in NES memory space
    /// </param>
    /// <returns>
    /// The address mapped to PPU register memory space (0x0 to 0x7)
    /// </returns>
    private ushort MapToMirroredRegisterAddress(ushort address)
    {
        // Map address back to the range of _registers.
        address = (ushort)(address - MemoryRange.Start);

        // Mirror PPU registers every 8 bytes
        address = (ushort)(address % MemoryRegions.PpuRegistersSize);
        return address;
    }

    /// <summary>
    /// Internal method for directly reading from PPU registers with no
    /// mirroring.
    /// </summary>
    /// <param name="address">
    /// Should always be in the range of 0x0 to 0x7
    /// </param>
    private byte ReadInternalRegister(ushort address)
    {
        switch (address)
        {
            case PpuStatus:
                // The lower 5 bits of the status register are unused. These
                // have been set to match mesen. No games should rely on this
                // behavior, but it is more accurate to the hardware.
                _openBus = (byte)(_registers[address] | (_openBus & 0x1F));

                // Reading from the status register resets the address latch
                // and clears the vertical blank flag.
                _addressRegister.ResetLatch();
                VblankFlag = false;
                break;

            case PpuData:
                _openBus = _dataBuffer;
                _dataBuffer = ReadMemory(_addressRegister.Value);

                // Palette RAM can be read immediately without going through the data buffer
                if (_addressRegister.Value >= PaletteRamStart && _addressRegister.Value < PaletteRamEnd)
                {
                    _openBus = _dataBuffer;
                }

                _addressRegister.Increment(IncrementMode);
                break;
            default:
                _openBus = _registers[address];
                break;
        }

        return _openBus;
    }

    /// <summary>
    /// Internal method for directly writing to PPU registers with no
    /// mirroring.
    /// </summary>
    private void WriteInternalRegister(ushort address, byte value)
    {
        switch (address)
        {
            case PpuAddress:
                _addressRegister.Write(value);
                break;
            case PpuData:
                _openBus = value;
                WriteMemory(_addressRegister.Value, _openBus);
                _addressRegister.Increment(IncrementMode);
                break;
            default:
                _openBus = value;
                _registers[address] = _openBus;
                break;
        }
    }

    private byte ReadMemory(ushort address)
    {
        if (address < PatternTablesEnd)
        {
            return _cartridge?.ChrRom[address] ?? 0;
        }
        if (address < NameTablesEnd)
        {
            // TODO: Implement nametable mirroring
            // For now, just read/write to the first nametable only
            var nameTableAddress = address - 0x2000;
            nameTableAddress %= NameTableSize;
            return _nameTables[(ushort)nameTableAddress];
        }
        if (address < PaletteRamStart)
        {
            // Unused memory region
            return 0;
        }
        if (address < PaletteRamEnd)
        {
            var paletteAddress = address - 0x3F00;
            paletteAddress %= 0x20;
            return _paletteRam[paletteAddress];
        }

        throw new ArgumentOutOfRangeException(
            nameof(address),
            "Address out of range for PPU memory"
        );
    }

    private void WriteMemory(ushort address, byte value)
    {
        if (address < PatternTablesEnd)
        {
            // Pattern tables are read-only for most games
        }
        else if (address < NameTablesEnd)
        {
            // TODO: Implement nametable mirroring
            // For now, just read/write to the first nametable only
            var nameTableAddress = address - 0x2000;
            nameTableAddress %= NameTableSize;
            _nameTables[(ushort)nameTableAddress] = value;
        }
        else if (address < PaletteRamStart)
        {
            // Unused memory region
        }
        else if (address < PaletteRamEnd)
        {
            var paletteAddress = address - 0x3F00;
            paletteAddress %= 0x20;
            _paletteRam[paletteAddress] = value;
        }
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
        // The vblank flag is set at the start of vblank (scanline 241).
        // Reading PPUSTATUS will return the current state of this flag and
        // then clear it. If the vblank flag is not cleared by reading, it
        // will be cleared automatically on dot 1 of the prerender
        // scanline.
        if (_scanline == VblankScanline && _cycle == 1)
        {
            VblankFlag = true;
        }

        if (_cycle < DisplayWidth && _scanline < DisplayHeight)
        {
            var nameTableIndex = PixelToNameTableIndex(_scanline, _cycle);
            var patternTableIndex = _nameTables[(ushort)nameTableIndex];

            var backgroundPatternTable = (_registers[PpuCtrl] & 0x10) > 0 ? 1 : 0;
            var pattern = GetPattern(patternTableIndex, backgroundPatternTable);

            var paletteNumber = GetAttributeTableValue(_scanline, _cycle);
            var colorIndex = GetBackgroundPixelColorIndex(pattern, _scanline % 8, _cycle % 8);
            var color = GetPaletteColor(paletteNumber, colorIndex);

            RenderPixelCallback?.Invoke(_cycle, _scanline, color.R, color.G, color.B);
        }

        _cycle += 1;
        if (_cycle >= CyclesPerScanline)
        {
            _cycle = 0;
            _scanline += 1;

            // VBlank is cleared on the first dot of scanline 261.
            if (_scanline == Scanlines - 1)
            {
                VblankFlag = false;
            }

            if (_scanline >= Scanlines)
            {
                _scanline = 0;
            }
        }
    }

    /// <summary>
    /// Gets the state of a specific bit in a PPU register.
    /// </summary>
    /// <param name="registerIndex">The index of the register (0-7)</param>
    /// <param name="bitMask">The bit mask to check</param>
    /// <returns>True if the bit is set, false otherwise</returns>
    private bool GetRegisterBit(int registerIndex, byte bitMask)
    {
        return (_registers[registerIndex] & bitMask) != 0;
    }

    /// <summary>
    /// Sets or clears a specific bit in a PPU register.
    /// </summary>
    /// <param name="registerIndex">The index of the register (0-7)</param>
    /// <param name="bitMask">The bit mask to set or clear</param>
    /// <param name="value">True to set the bit, false to clear it</param>
    private void SetRegisterBit(int registerIndex, byte bitMask, bool value)
    {
        if (value)
        {
            _registers[registerIndex] |= bitMask;
        }
        else
        {
            _registers[registerIndex] &= (byte)~bitMask;
        }
    }

    // Convert a pixel coordinate (scanline, cycle) to a name table index (32x30).
    private static int PixelToNameTableIndex(int scanline, int cycle)
    {
        return (scanline / 8) * 32 + (cycle / 8);
    }

    /// <summary>
    /// Returns all of the data for a single tile in the pattern table.
    /// </summary>
    public ReadOnlySpan<byte> GetPattern(int patternIndex, int table = 0)
    {
        var patternTableOffset = (patternIndex * BytesPerTile) + (table * PatternTableSizeBytes);
        return _patternTables.Slice(patternTableOffset, BytesPerTile);
    }

    /// <summary>
    /// Get a pattern based on row and column in pattern-table space (not pixel
    /// space).
    /// </summary>
    public ReadOnlySpan<byte> GetPattern(int pixelRow, int pixelCol, int table = 0)
    {
        int patternRow = pixelRow / PatternSize;
        int patternCol = pixelCol / PatternSize;
        int patternIndex = patternRow * PatternTableTilesWidth + patternCol;
        int patternTableOffset = patternIndex * BytesPerTile;
        patternTableOffset += table * PatternTableSizeBytes;

        var pattern = _patternTables.Slice(patternTableOffset, BytesPerTile);
        return pattern;
    }

    /// <summary>
    /// Temporary, used only to visualize the pattern table.
    /// </summary>
    public Color GetPatternTablePixel(int pixelRow, int pixelCol, bool useGrayscale = true)
    {
        // Display color palettes on top of the pattern table for now
        const int PaletteVisualizationSize = 4;
        const int TotalPaletteColors = 8 * 4;
        if (pixelRow < PaletteVisualizationSize && pixelCol < TotalPaletteColors * PaletteVisualizationSize)
        {
            pixelCol /= PaletteVisualizationSize;
            var paletteColor = GetPaletteColor(
                paletteNumber: pixelCol / 4,
                colorIndex: pixelCol % 4
            );

            return paletteColor;
        }

        // The second pattern table is located directly to the right of the first
        int patternTableNumber = pixelCol >= PatternTablePixelWidth ? 1 : 0;
        var pattern = GetPattern(pixelRow, pixelCol % PatternTablePixelWidth, patternTableNumber);

        // Use the first background palette for pattern table visualization
        var colorIndex = GetBackgroundPixelColorIndex(pattern, pixelRow % PatternSize, pixelCol % PatternSize);
        var color = GetPaletteColor(paletteNumber: 0, colorIndex);
        return color;
    }

    /// <summary>
    /// Get the attribute table value for a given pixel position.
    /// The attribute table determines which palette to use for each 2x2 tile area.
    /// </summary>
    private byte GetAttributeTableValue(int scanline, int cycle)
    {
        // Each attribute byte controls a 4x4 tile area (32x32 pixels)
        int attributeX = (cycle / 8) / 4;
        int attributeY = (scanline / 8) / 4;
        int attributeIndex = attributeY * 8 + attributeX;

        // Attribute table starts at offset 0x3C0 within the nametable (0x23C0 in PPU memory)
        int attributeAddress = 0x2000 + 0x3C0 + attributeIndex;
        var attributeByte = ReadMemory((ushort)attributeAddress);

        // Determine which 2-bit value within the attribute byte to use
        int quadrantX = ((cycle / 8) % 4) / 2;
        int quadrantY = ((scanline / 8) % 4) / 2;
        int quadrant = quadrantY * 2 + quadrantX;

        // Extract the 2-bit palette number from the attribute byte
        return (byte)((attributeByte >> (quadrant * 2)) & 0x03);
    }

    /// <summary>
    /// Get a color from the palette RAM, converting the NES palette index to RGB.
    /// Each palette has 4 colors.
    /// </summary>
    private Color GetPaletteColor(int paletteNumber, int colorIndex)
    {
        var paletteOffset = (paletteNumber * 4) + colorIndex;
        var paletteIndex = _paletteRam[paletteOffset];
        var color = Palette.Colors[paletteIndex];
        return color;
    }

    /// <summary>
    /// Bitwise zip of two bytes, at a given two-bit index
    /// </summary>
    internal static byte ZipBytes(byte lowByte, byte highByte, byte index)
    {
        // Get just one bit from the low byte
        byte lowBit = (byte)(lowByte & (1 << index));
        lowBit >>= index;

        // Get just one bit from the high byte
        byte highBit = (byte)(highByte & (1 << index));
        highBit >>= index;
        // Shift high bit left just once so we can combine it with the low bit
        highBit <<= 1;

        return (byte)(lowBit | highBit);
    }

    /// <summary>
    /// Get the color index of a background pixel.
    /// </summary>
    /// <returns>
    /// The color index into a single color palette. This is always a value of 0, 1, 2, or 3.
    /// </returns>
    private static byte GetBackgroundPixelColorIndex(
        ReadOnlySpan<byte> pattern,
        int pixelRow,
        int pixelCol
    )
    {
        // Get the 2-bit pixel value from the pattern
        byte pixelValue = ZipBytes(pattern[pixelRow], pattern[pixelRow + 8], (byte)(7 - pixelCol));
        return pixelValue;
    }
}
