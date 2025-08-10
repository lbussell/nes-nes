// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public delegate void RenderPixel(ushort x, ushort y, byte r, byte g, byte b);

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
public class Ppu : ICpuReadable, ICpuWritable
{
    // The following are indices into the _registers memory array
    private const int PpuCtrl = 0;
    private const int PpuMask = 1;
    private const int PpuStatus = 2;
    private const int OamAddress = 3;
    private const int OamData = 4;
    private const int PpuAddress = 6;
    private const int PpuData = 7;

    private readonly PpuAddrRegister _addressRegister = new();

    private readonly byte[] _registers = new byte[MemoryRegions.PpuRegistersSize];
    private readonly byte[] _paletteRam = new byte[0x20];
    private readonly byte[] _oam = new byte[0x100];
    private readonly byte[] _secondaryOam = new byte[0x20];

    // The following fields are data that hold information about sprites that
    // will be drawn on the current scanline. This array holds the X
    // coordinates of sprites to draw on the current scanline. For each sprite,
    // this value will be decremented each cycle/pixel until it reaches 0, at
    // which point we'll start shifting the sprite's pixels out of the shift
    // registers onto the screen.
    private readonly byte[] _spriteXCoordinates = new byte[8];

    // This array contains whether the sprite is flipped horizontally or not.
    private readonly bool[] _spriteFlippedHorizontally = new bool[8];

    // Whether or not sprites should be drawn **behind** the background.
    private readonly bool[] _spritePriority = new bool[8];

    // This array contains the palette number for each sprite to be drawn on
    // the current scanline.
    private readonly byte[] _spritePalette = new byte[8];

    // These are the shift registers for the sprites that will be drawn on the
    // current scanline. Each sprite pixel's high and low bit are stored
    // separately and get ORed together to get the final pixel's color index.
    private readonly byte[] _spritePatternHigh = new byte[8];
    private readonly byte[] _spritePatternLow = new byte[8];

    // The current PPU cycle [0,340]. This corresponds to which pixel is being
    // drawn on the current scanline.
    private ushort _cycle = 0;

    // The current scanline (0-261).
    private ushort _scanline = 0;

    // How many sprites are on this scanline.
    private byte _spritesOnScanline = 0;

    // Data buffer used for delaying reads of PPU memory
    private byte _dataBuffer;

    // The open bus is loaded with data whenever reads or writes are made to
    // the PPU's registers. It is also mapped to bits 0-4 of the PPU status
    // register.
    private byte _openBus;

    /// <summary>
    /// The PPU accesses cartridge data and CHR ROM through the mapper.
    /// </summary>
    public IMapper? Mapper { get; set; } = null;

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

    public bool BackgroundEnabled => GetRegisterBit(PpuMask, 1 << 3);
    public bool SpritesEnabled => GetRegisterBit(PpuMask, 1 << 4);

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

    private int SpritePatternTableAddress => GetRegisterBit(PpuCtrl, 1 << 3) ? 0x1000 : 0x0000;

    /// <summary>
    /// Object attribute memory (OAM) is used to store data about which sprites
    /// should be rendered on the screen.
    /// </summary>
    public Span<byte> Oam => _oam;

    /// <summary>
    /// This method should be called by the CPU to interact with the PPU.
    /// </summary>
    /// <param name="address">
    /// The address in CPU memory space to read from.
    /// </param>
    /// <returns>
    /// The data from that address/pin returned by the PPU.
    /// </returns>
    public byte CpuRead(ushort address)
    {
        address = MapToMirroredRegisterAddress(address);
        return ReadInternalRegister(address);
    }

    /// <summary>
    /// This method should be called by the CPU to write to the PPU's registers.
    /// </summary>
    /// <param name="address">
    /// The address in CPU memory space to write to.
    /// </param>
    /// <param name="value">
    /// This value will be given to the PPU to do whatever it wants with.
    /// </param>
    public void CpuWrite(ushort address, byte value)
    {
        address = MapToMirroredRegisterAddress(address);
        WriteInternalRegister(address, value);
    }

    public void WriteOam(byte address, byte value)
    {
        _oam[address] = value;
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
                if (_addressRegister.Value >= PpuConsts.PaletteRamStart
                    && _addressRegister.Value < PpuConsts.PaletteRamEnd)
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
        if (address < 0x3000)
        {
            return Mapper?.PpuRead(address) ?? 0;
        }
        if (address < PpuConsts.PaletteRamStart)
        {
            // Unused memory region
            return 0;
        }
        if (address < PpuConsts.PaletteRamEnd)
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
        if (address < PpuConsts.PatternTablesEnd)
        {
            // Pattern tables are read-only for most games
        }
        else if (address < PpuConsts.NameTablesEnd)
        {
            Mapper?.PpuWrite(address, value);
        }
        else if (address < PpuConsts.PaletteRamStart)
        {
            // Unused memory region
        }
        else if (address < PpuConsts.PaletteRamEnd)
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
    /// Advance the PPU by one cycle.
    /// </summary>
    private void Step()
    {
        // Frame timing diagram: https://www.nesdev.org/w/images/default/4/4f/Ppu.svg

        var isVisibleScanline = _scanline < PpuConsts.DisplayHeight;
        var isVisibleCycle = _cycle < PpuConsts.DisplayWidth;

        if (isVisibleScanline)
        {
            // Do OAM/sprite operations
            switch (_cycle)
            {
                case 1:
                    ClearSecondaryOam();
                    break;
                case 256:
                    EvaluateSprites();
                    break;
                case 340:
                    FetchSprites();
                    break;
            }

            if (isVisibleCycle)
            {
                // If we update shift registers on the first cycle, then
                // sprites will be shifted one pixel too far to the left.
                if (_cycle != 0)
                {
                    UpdateShiftRegisters();
                }

                var (spritePixel, spritePalette, spriteBehindBackground) = GetSpritePixel();
                var (backgroundPixel, backgroundPalette) = GetBackgroundPixel();

                var (colorIndex, palette) = (spritePixel, backgroundPixel, spriteBehindBackground) switch
                {
                    // No sprite pixel
                    (0, _, _) => (backgroundPixel, backgroundPalette),

                    // No background pixel
                    (_, 0, _) => (spritePixel, spritePalette),

                    // If both pixels are present, the sprite pixel is drawn
                    // over the background only if its priority flag is set
                    // to false.
                    (> 0, > 0, false) => (spritePixel, spritePalette),

                    // Otherwise the background pixel is drawn over sprite
                    (> 0, > 0, true) => (backgroundPixel, backgroundPalette),
                };

                var color = GetPaletteColor(palette, colorIndex);
                RenderPixelCallback?.Invoke(_cycle, _scanline, color.R, color.G, color.B);
            }
        }

        // The vblank flag is set at the start of vblank (scanline 241).
        // Reading PPUSTATUS will return the current state of this flag and
        // then clear it. If the vblank flag is not cleared by reading, it
        // will be cleared automatically on dot 1 of the prerender
        // scanline.
        if (_scanline == PpuConsts.VblankScanline && _cycle == 1)
        {
            VblankFlag = true;
        }

        _cycle += 1;
        if (_cycle >= PpuConsts.CyclesPerScanline)
        {
            _cycle = 0;
            _scanline += 1;

            // VBlank is cleared on the first dot of scanline 261.
            if (_scanline == PpuConsts.Scanlines - 1)
            {
                VblankFlag = false;
            }

            if (_scanline >= PpuConsts.Scanlines)
            {
                _scanline = 0;
            }
        }
    }

    private (byte colorIndex, byte palette) GetBackgroundPixel()
    {
        if (!BackgroundEnabled)
        {
            return (0, 0);
        }

        // This is currently all a big hack to get static backgrounds rendered.
        // We need to do a lot more work to get background scrolling working.
        // In a real PPU, everything here would be rendered using shift
        // registers, similar to the logic for sprites.

        var nameTableIndex = PixelToNameTableIndex(_scanline, _cycle);
        var patternTableIndex = Mapper!.PpuRead((ushort)nameTableIndex);

        // Decide which pattern table to use based on the PPU control register
        var backgroundPatternTable = (_registers[PpuCtrl] & 0x10) > 0 ? 1 : 0;
        var pattern = GetPattern(patternTableIndex, backgroundPatternTable);

        var backgroundPalette = GetAttributeTableValue(_scanline, _cycle);
        var colorIndex = GetBackgroundPixelColorIndex(pattern, _scanline % 8, _cycle % 8);

        return (colorIndex, backgroundPalette);
    }

    /// <summary>
    /// According to the current state of the PPU returns the pixel color and
    /// palette for the sprite that should be rendered on the current cycle.
    /// This doesn't take into account any background pixels - if there is no
    /// sprite to be drawn, then it will return a pixel color of 0
    /// (transparent).
    /// </summary>
    private (byte colorIndex, byte palette, bool isBehindBackground) GetSpritePixel()
    {
        byte spritePixel = 0;
        byte spritePalette = 0;
        bool isBehindBackground = false;

        if (!SpritesEnabled)
        {
            return (spritePixel, spritePalette, isBehindBackground);
        }

        for (int i = 0; i < _spritesOnScanline; i += 1)
        {
            var spriteX = _spriteXCoordinates[i];

            if (spriteX != 0)
            {
                continue;
            }

            // We have already started shifting the sprite's pattern data, so
            // we know it's visible. If we have already drawn a sprite pixel,
            // we shouldn't overwrite it. That's because sprites with a lower
            // index have priority over those with higher indices.

            // Get the sprite pixel - since these are shift registers we get
            // only the high bit or the low bit. This depends on which way the
            // sprite is flipped because I chose to shift the registers in the
            // opposite direction when the sprite is flipped horizontally (as
            // opposed to reversing the bits in the sprite data).
            byte compareTo = _spriteFlippedHorizontally[i] ? (byte)0x01 : (byte)0x80;
            byte spritePatternLow = (byte)((_spritePatternLow[i] & compareTo) > 0 ? 1 : 0);
            byte spritePatternHigh = (byte)((_spritePatternHigh[i] & compareTo) > 0 ? 1 : 0);
            spritePixel = (byte)((spritePatternHigh << 1) | spritePatternLow);

            // Sprite palettes have a range of 4-7, so we need to add 4 to get
            // the correct range.
            spritePalette = (byte)(_spritePalette[i] + 4);

            isBehindBackground = _spritePriority[i];

            if (spritePixel > 0)
            {
                // The first sprite pixel we find is the one to draw, so there
                // is no need to check the rest of the sprites.
                break;
            }
        }

        return (spritePixel, spritePalette, isBehindBackground);
    }

    private void ClearSecondaryOam()
    {
        Array.Fill<byte>(_secondaryOam, 0xFF);
    }

    private void EvaluateSprites()
    {
        // This is all really just a big hack. We're going to do all sprite
        // evaluation in one cycle, which is not how the PPU actually works.
        // Sprite evaluation should actually take place over cycles 65-256, but
        // this over-simplified approach will work just to get pictures on the
        // screen. Some games won't work correctly with this approach.

        _spritesOnScanline = 0;
        for (int spriteIndex = 0; spriteIndex < 64; spriteIndex += 1)
        {
            // TODO - Support 8x16 sprites.
            const int SpriteHeight = 8;

            // Check if the sprite will be visible on the next scanline.
            var spriteY = _oam[spriteIndex * 4];
            var isInRange = spriteY <= _scanline && spriteY + SpriteHeight > _scanline;

            if (isInRange)
            {
                if (_spritesOnScanline >= 8)
                {
                    // TODO: Overflow = true;
                    return;
                }

                var _secondaryOamIndex = _spritesOnScanline * 4;

                // Copy the sprite's data to secondary OAM, which contains
                // all sprites that will be drawn on the next scanline.
                _oam.AsSpan(spriteIndex * 4, 4).CopyTo(_secondaryOam.AsSpan(_secondaryOamIndex, 4));
                _spritesOnScanline += 1;
            }
        }
    }

    private void FetchSprites()
    {
        for (int i = 0; i < _spritesOnScanline; i += 1)
        {
            var spriteOffset = i * 4;

            // https://www.nesdev.org/wiki/PPU_OAM
            // The secondary OAM contains the sprites that will be drawn on the
            // next scanline. Each sprite is represented by 4 bytes.
            var spriteY = _secondaryOam[spriteOffset];
            var tileIndexNumber = _secondaryOam[spriteOffset + 1];
            var attribute = _secondaryOam[spriteOffset + 2];
            var spriteX = _secondaryOam[spriteOffset + 3];

            // We don't need to remember if the sprite is flipped vertically
            // later, we can account for this now when we read the sprite's
            // pattern data.
            var isFlippedVertically = (attribute & 0b_1000_0000) > 0;

            // Set other sprite attributes
            _spriteFlippedHorizontally[i] = (attribute & 0b_0100_0000) > 0;
            _spritePriority[i] = (attribute & 0b_0010_0000) > 0;
            _spritePalette[i] = (byte)(attribute & 0x03);

            // For 8x8 sprites, the pattern table address is determined by the
            // PPU control register (that's what we're reading here, behind the
            // getter)
            var patternTableTileAddress = SpritePatternTableAddress
                + (tileIndexNumber * PpuConsts.BytesPerTile);

            // Finally, determine which row of the sprite we need to get.
            var rowOffset = _scanline - spriteY;

            if (isFlippedVertically)
            {
                rowOffset = 7 - rowOffset;
            }

            var patternLowAddress = (ushort)(patternTableTileAddress + rowOffset);
            var patternHighAddress = (ushort)(patternLowAddress + 8);

            var patternLow = Mapper!.PpuRead(patternLowAddress);
            var patternHigh = Mapper.PpuRead(patternHighAddress);

            _spritePatternLow[i] = patternLow;
            _spritePatternHigh[i] = patternHigh;
            _spriteXCoordinates[i] = spriteX;
        }
    }

    private void UpdateShiftRegisters()
    {
        for (int i = 0; i < _spritesOnScanline; i += 1)
        {
            if (_spriteXCoordinates[i] > 0)
            {
                // We decrement the sprite's X coordinate here to keep track of
                // when the scanline reaches the sprite on the screen.
                _spriteXCoordinates[i] -= 1;
                continue;
            }

            // When the sprite's X coordinate reaches 0, we can start updating
            // its shift registers.
            if (_spriteFlippedHorizontally[i])
            {
                _spritePatternHigh[i] >>= 1;
                _spritePatternLow[i] >>= 1;
            }
            else
            {
                _spritePatternHigh[i] <<= 1;
                _spritePatternLow[i] <<= 1;
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
        return 0x2000 + (scanline / 8) * 32 + (cycle / 8);
    }

    /// <summary>
    /// Returns all of the data for a single tile in the pattern table.
    /// </summary>
    public ReadOnlySpan<byte> GetPattern(int patternIndex, int table = 0)
    {
        var patternTableOffset =
            (patternIndex * PpuConsts.BytesPerTile) + (table * PpuConsts.PatternTableSizeBytes);

        return Mapper!.PpuRead((ushort)patternTableOffset, PpuConsts.BytesPerTile);
    }

    /// <summary>
    /// Get a pattern based on row and column in pattern-table space (not pixel
    /// space).
    /// </summary>
    public ReadOnlySpan<byte> GetPattern(int pixelRow, int pixelCol, int table = 0)
    {
        int patternRow = pixelRow / PpuConsts.PatternSize;
        int patternCol = pixelCol / PpuConsts.PatternSize;
        int patternIndex = patternRow * PpuConsts.PatternTableTilesWidth + patternCol;
        int patternTableOffset = patternIndex * PpuConsts.BytesPerTile;
        patternTableOffset += table * PpuConsts.PatternTableSizeBytes;

        var pattern = Mapper!.PpuRead((ushort)patternTableOffset, PpuConsts.BytesPerTile);
        return pattern;
    }

    /// <summary>
    /// Temporary, used only to visualize the pattern table.
    /// </summary>
    public Color GetPatternTablePixel(
        int pixelRow,
        int pixelCol,
        int table,
        bool useGrayscale = true
    )
    {
        // The second pattern table is located directly to the right of the first
        var pattern = GetPattern(pixelRow, pixelCol, table);

        // Use the first background palette for pattern table visualization
        var colorIndex = GetBackgroundPixelColorIndex(
            pattern,
            pixelRow % PpuConsts.PatternSize,
            pixelCol % PpuConsts.PatternSize
        );

        if (useGrayscale)
        {
            // For pattern table visualization, use grayscale based on the pattern data
            // This bypasses the palette system and shows the raw pattern data
            var grayValue = (byte)(colorIndex * 85); // 0, 85, 170, 255 for values 0, 1, 2, 3
            return new Color(grayValue, grayValue, grayValue);
        }
        else
        {
            var color = GetPaletteColor(paletteNumber: 0, colorIndex);
            return color;
        }
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
    /// <param name="pattern">
    /// The pattern data for the tile, which must be 16 bytes long.
    /// </param>
    /// <returns>
    /// The pixel's index into the color palette for the tile. This is always a
    /// value of 0, 1, 2, or 3.
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
