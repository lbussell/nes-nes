// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NesNes.Core;

public class PpuV2 : IPpu
{
    private struct BackgroundRenderData
    {
        public byte NextTileId;
        public byte Attribute;
        public byte PatternLow;
        public byte PatternHigh;
    }

    private struct SpriteRenderData
    {
        public byte X;
        public byte Palette;
        public byte PatternHigh;
        public byte PatternLow;
        public bool FlippedHorizontally;
        public bool Priority;

        public void Shift()
        {
            if (X > 0)
            {
                X -= 1;
            }
            else if (FlippedHorizontally)
            {
                PatternHigh >>= 1;
                PatternLow >>= 1;
            }
            else
            {
                PatternHigh <<= 1;
                PatternLow <<= 1;
            }
        }
    }

    /// <summary>
    /// Background shifters used for rendering background pixels. Pixels are
    /// shifted out of the high bit first, and new data is loaded into the low
    /// bits.
    /// </summary>
    private struct BackgroundShifters
    {
        public ushort PatternLow;
        public ushort PatternHigh;
        public ushort AttributeLow;
        public ushort AttributeHigh;

        public readonly byte GetPixel(byte fineX)
        {
            // Fine X scroll selects which bit of the 16-bit shift register is output. The bit
            // normally output is bit 15 (mask 0x8000). We shift each cycle, but instead of
            // performing fine X worth of dummy shifts we tap the appropriate bit using (0x8000 >>
            // fineX).
            var mask = (ushort)(0x8000 >> fineX);
            var bit0 = (PatternLow & mask) > 0 ? 1 : 0;
            var bit1 = (PatternHigh & mask) > 0 ? 1 : 0;
            return (byte)(bit0 | (bit1 << 1));
        }

        public readonly byte GetPalette(byte fineX)
        {
            var mask = (ushort)(0x8000 >> fineX);
            var bit0 = (AttributeLow & mask) > 0 ? 1 : 0;
            var bit1 = (AttributeHigh & mask) > 0 ? 1 : 0;
            return (byte)(bit0 | (bit1 << 1));
        }

        public void Shift()
        {
            PatternLow <<= 1;
            PatternHigh <<= 1;
            AttributeLow <<= 1;
            AttributeHigh <<= 1;
        }

        public void Load(BackgroundRenderData data)
        {
            PatternLow = (ushort)((PatternLow & 0xFF00) | data.PatternLow);
            PatternHigh = (ushort)((PatternHigh & 0xFF00) | data.PatternHigh);

            var attributeLow = (data.Attribute & 0b01) > 0 ? 0xFF : 0x00;
            var attributeHigh = (data.Attribute & 0b10) > 0 ? 0xFF : 0x00;
            AttributeLow = (ushort)((AttributeLow & 0xFF00) | attributeLow);
            AttributeHigh = (ushort)((AttributeHigh & 0xFF00) | attributeHigh);
        }
    }

    public const int NumScanlines = 262;
    public const int NumCycles = 341;

    // PPU registers
    private const int PpuCtrl_2000 = 0;
    private const int PpuMask_2001 = 1;
    private const int PpuStatus_2002 = 2;
    private const int OamAddr_2003 = 3;
    private const int OamData_2004 = 4;
    private const int PpuScroll_2005 = 5;
    private const int PpuAddr_2006 = 6;
    private const int PpuData_2007 = 7;

    // PPU internal memory
    // * Nametables and pattern tables are handled by Mapper
    private readonly byte[] _registers = new byte[8];
    private readonly byte[] _paletteRam = new byte[0x20];
    private readonly byte[] _oam = new byte[0x100];
    private readonly byte[] _secondaryOam = new byte[0x20];

    // PPU internal registers
    private VRegister _v;
    private VRegister _t;
    private byte _fineXScroll;
    private bool _w;
    private byte _openBus;
    private byte _dataBuffer;
    private BackgroundRenderData _bg;
    private BackgroundShifters _bgShifters;
    private readonly SpriteRenderData[] _sprites = new SpriteRenderData[8];
    private int _spritesOnScanline;
    // Indicates whether we are drawing sprite 0 on the current scanline.
    private bool _spriteZeroHitIsPossible;

    // PPU state
    private long _frame;
    private int _scanline;
    private int _cycle;

    public long Frame => _frame;
    public int Scanline => _scanline;
    public int Cycle => _cycle;
    public Span<byte> Oam => _oam;
    public Span<byte> PaletteRam => _paletteRam;

    public IMapper? Mapper { get; set; } = null;
    public bool NonMaskableInterruptPin => NmiEnabled && VblankFlag;

    public RenderPixel? OnRenderPixel { get; set; }

    // Bits 0 and 1: 0=$2000, 1=$2400, 2=$2800, 3=$2C00
    private byte BaseNametableAddress => _t.NameTable;
    private bool VRamIncrement => _registers[PpuCtrl_2000].GetBit(2);
    private ushort SpritePatternTableAddress => (ushort)((_registers[PpuCtrl_2000] & 0x08) << 9);
    public ushort BackgroundPatternTableAddress => (ushort)((_registers[PpuCtrl_2000] & 0x10) << 8);
    private bool SpritesUse8x16 => _registers[PpuCtrl_2000].GetBit(5);
    // PpuCtrl bit 6 is unused on commercial hardware (uses EXT)
    private bool NmiEnabled => _registers[PpuCtrl_2000].GetBit(7);

    private bool Grayscale => _registers[PpuMask_2001].GetBit(0);
    private bool ShowBackgroundInLeft8Pixels => _registers[PpuMask_2001].GetBit(1);
    private bool ShowSpritesInLeft8Pixels => _registers[PpuMask_2001].GetBit(2);
    private bool BackgroundEnabled => _registers[PpuMask_2001].GetBit(3);
    private bool SpritesEnabled => _registers[PpuMask_2001].GetBit(4);
    private bool EmphasizeRed => _registers[PpuMask_2001].GetBit(5);
    private bool EmphasizeGreen => _registers[PpuMask_2001].GetBit(6);
    private bool EmphasizeBlue => _registers[PpuMask_2001].GetBit(7);

    private bool SpriteOverflow
    {
        get => _registers[PpuStatus_2002].GetBit(5);
        set => _registers[PpuStatus_2002] = _registers[PpuStatus_2002].SetBit(5, value);
    }

    private bool SpriteZeroHit
    {
        get => _registers[PpuStatus_2002].GetBit(6);
        set => _registers[PpuStatus_2002] = _registers[PpuStatus_2002].SetBit(6, value);
    }

    private bool VblankFlag
    {
        get => _registers[PpuStatus_2002].GetBit(7);
        set => _registers[PpuStatus_2002] = _registers[PpuStatus_2002].SetBit(7, value);
    }

    public byte CpuRead(ushort address)
    {
        var register = MapCpuToRegisterAddress(address);
        Debug.Assert(register >= 0 && register < _registers.Length);

        switch (register)
        {
            // Don't currently have a use for customizing these
            // case PpuCtrl_2000:
            //     break;
            // case PpuMask_2001:
            //     break;

            case PpuStatus_2002:
                // The upper 3 bits of PpuStatus are data, but the lower 5 bits
                // are mapped to the open bus
                _openBus = (byte)(_registers[PpuStatus_2002] & 0xE0);
                _w = false;
                VblankFlag = false;
                break;

            // Don't currently have a use for customizing these
            // case OamAddr_2003:
            //     break;

            case OamData_2004:
                var oamAddress = _registers[OamAddr_2003];
                _openBus = _oam[oamAddress];
                break;

            // Don't currently have a use for customizing these
            // case PpuScroll_2005:
            //     break;
            // case PpuAddr_2006:
            //     break;

            case PpuData_2007:
                // Reads are delayed by one access
                _openBus = _dataBuffer;
                _dataBuffer = ReadMemory(_v);

                // Palette RAM can be read immediately without going through the data buffer
                if (_v >= 0x3F00 && _v < 0x4000)
                {
                    _openBus = _dataBuffer;
                }

                IncrementV();
                break;

            default:
                _openBus = _registers[register];
                break;
        }

        return _openBus;
    }

    public void CpuWrite(ushort address, byte value)
    {
        var register = MapCpuToRegisterAddress(address);
        Debug.Assert(register >= 0 && register < _registers.Length);

        switch (register)
        {
            case PpuCtrl_2000:
                // Writes to the following registers are ignored if earlier than ~29658 CPU clocks
                // after reset: PPUCTRL, PPUMASK, PPUSCROLL, PPUADDR.
                if (_frame == 0 && _scanline < 258)
                {
                    break;
                }

                _registers[PpuCtrl_2000] = value;
                _t.NameTable = (byte)(value & 0x03);
                break;

            case PpuMask_2001:
                // Writes to the following registers are ignored if earlier than ~29658 CPU clocks
                // after reset: PPUCTRL, PPUMASK, PPUSCROLL, PPUADDR.
                if (_frame == 0 && _scanline < 258)
                {
                    break;
                }

                _registers[register] = value;
                break;

            // case PpuStatus_2002:
            //     break;
            // case OamAddr_2003:
            //     break;

            case OamData_2004:
                var oamAddress = _registers[OamAddr_2003];
                _oam[oamAddress] = value;
                // OAM address is incremented after writes
                oamAddress += 1;
                _registers[OamAddr_2003] = oamAddress;
                break;

            case PpuScroll_2005:
                // Writes to the following registers are ignored if earlier than ~29658 CPU clocks
                // after reset: PPUCTRL, PPUMASK, PPUSCROLL, PPUADDR.
                if (_frame == 0 && _scanline < 258)
                {
                    break;
                }

                if (!_w)
                {
                    // First write
                    _w = true;
                    // t: ........ ...ABCDE <- d: ABCDE...
                    // x:               FGH <- d: .....FGH
                    _t.CoarseX = (byte)((value & 0xF8) >> 3);
                    _fineXScroll = (byte)(value & 0x07);
                }
                else
                {
                    // Second write
                    _w = false;
                    // t: .FGH..AB CDE..... <- d: ABCDEFGH
                    _t.CoarseY = (byte)(value >> 3);
                    _t.FineY = (byte)(value & 0x07);
                }

                break;

            case PpuAddr_2006:
                // Writes to the following registers are ignored if earlier than ~29658 CPU clocks
                // after reset: PPUCTRL, PPUMASK, PPUSCROLL, PPUADDR.
                if (_frame == 0 && _scanline < 258)
                {
                    break;
                }

                if (!_w)
                {
                    // First write
                    _w = true;
                    // t: ..CDEFGH ........ <- d: ..CDEFGH
                    _t.Value = (ushort)((_t.Value & 0x00FF) | ((value & 0x3F) << 8));
                }
                else
                {
                    // Second write
                    _w = false;
                    // t: ....... ABCDEFGH <- d: ABCDEFGH
                    _t.Value = (ushort)((_t.Value & 0xFF00) | value);
                    _v = _t;
                }

                break;

            case PpuData_2007:
                WriteMemory(_v, value);
                IncrementV();
                break;

            default:
                _registers[register] = value;
                break;
        }
    }

    public void WriteOam(byte address, byte value)
    {
        _oam[address] = value;
    }

    public void Step()
    {
        // This method should closely follow the PPU timing diagram at
        // https://www.nesdev.org/w/images/default/4/4f/Ppu.svg

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

        // Do background fetching operations
        if (_scanline == 261 || _scanline < 240)
        {
            if ((_cycle > 0 && _cycle < 261) || (_cycle >= 321 && _cycle <= 336))
            {
                // Don't shift for the first pixel
                if (_cycle >= 1)
                {
                    _bgShifters.Shift();
                    for (int i = 0; i < _spritesOnScanline; i += 1)
                    {
                        _sprites[i].Shift();
                    }
                }

                switch (_cycle % 8)
                {
                    case 0:
                        IncrementCoarseXScroll();
                        _bgShifters.Load(_bg);
                        break;
                    case 1:
                        _bg.NextTileId = FetchNameTable();
                        break;
                    case 3:
                        _bg.Attribute = FetchAttribute();
                        break;
                    case 5:
                        _bg.PatternLow = FetchBackgroundLow();
                        break;
                    case 7:
                        _bg.PatternHigh = FetchBackgroundHigh();
                        break;
                }

                if (_cycle == 256)
                {
                    // Increment vertical scroll
                    IncrementVerticalScroll();
                }
                else if (_cycle == 257)
                {
                    if (BackgroundEnabled || SpritesEnabled)
                    {
                        // Transfer horizontal components of T to V
                        _v.CoarseX = _t.CoarseX;
                        _v.NameTableX = _t.NameTableX;
                    }
                }
            }

            // Only draw visible scanlines 0-239 (240 is post-render, 261 is pre-render)
            if (_scanline < 240 && _cycle >= 0 && _cycle < 256)
            {
                var backgroundPixel = _bgShifters.GetPixel(_fineXScroll);
                var backgroundPalette = _bgShifters.GetPalette(_fineXScroll);

                // Left 8-pixel masking per PPUMASK bits 1 & 2
                if (_cycle <= 8)
                {
                    if (!ShowBackgroundInLeft8Pixels)
                    {
                        backgroundPixel = 0;
                    }
                }

                var (spritePixel, spritePalette, spriteBehindBackground, isSpriteZero) = GetSpritePixel();

                var (colorIndex, palette) = (spritePixel, backgroundPixel, spriteBehindBackground) switch
                {
                    // No sprite pixel
                    (0, _, _) => (backgroundPixel, backgroundPalette),

                    // No background pixel
                    (_, 0, _) => (spritePixel, spritePalette),

                    // If both pixels are present, the sprite pixel is drawn
                    // over the background only if its priority flag is set
                    // to false.
                    ( > 0, > 0, false) => (spritePixel, spritePalette),

                    // Otherwise the background pixel is drawn over sprite
                    ( > 0, > 0, true) => (backgroundPixel, backgroundPalette),
                };

                // Sprite 0 hit: occurs if non-zero background & sprite pixel overlap (before x=255)
                if (
                    !SpriteZeroHit
                    && isSpriteZero
                    && spritePixel > 0
                    && backgroundPixel > 0
                    && _cycle < 255
                )
                {
                    SpriteZeroHit = true;
                }

                Output(GetPaletteColor(palette, colorIndex));
            }
        }

        if (_scanline == 241 && _cycle == 1)
        {
            // Set VBlank flag
            VblankFlag = true;
        }

        if (_scanline == 261)
        {
            if (_cycle == 1)
            {
                // Clear VBlank flag, sprite 0 hit, sprite overflow
                VblankFlag = false;
                SpriteOverflow = false;
                SpriteZeroHit = false;
            }

            if (_cycle >= 280 && _cycle <= 304)
            {
                // Copy vertical components of T to V
                if (BackgroundEnabled || SpritesEnabled)
                {
                    _v.CoarseY = _t.CoarseY;
                    _v.FineY = _t.FineY;
                    _v.NameTableY = _t.NameTableY;
                }
            }
        }

        // Move on to the next cycle/scanline
        _cycle += 1;
        if (_cycle >= NumCycles)
        {
            _cycle = 0;
            _scanline += 1;
            if (_scanline >= NumScanlines)
            {
                _frame += 1;
                _scanline = 0;
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

            _sprites[i] = new SpriteRenderData
            {
                X = spriteX,
                Palette = (byte)(attribute & 0x03),
                PatternHigh = Mapper!.PpuRead(patternHighAddress),
                PatternLow = Mapper!.PpuRead(patternLowAddress),
                FlippedHorizontally = (attribute & 0b_0100_0000) > 0,
                Priority = (attribute & 0b_0010_0000) > 0,
            };
        }
    }

    private void EvaluateSprites()
    {
        // This is all really just a big hack. We're going to do all sprite
        // evaluation in one cycle, which is not how the PPU actually works.
        // Sprite evaluation should actually take place over cycles 65-256, but
        // this over-simplified approach will work just to get pictures on the
        // screen. Some games won't work correctly with this approach.

        _spritesOnScanline = 0;

        var nextScanline = _scanline + 1; // Evaluate for the next scanline per NES timing

        _spriteZeroHitIsPossible = false;
        for (int spriteIndex = 0; spriteIndex < 64; spriteIndex += 1)
        {
            // TODO - Support 8x16 sprites.
            const int SpriteHeight = 8;

            // Check if the sprite will be visible on the next scanline.
            var spriteY = _oam[spriteIndex * 4];
            // var isInRange = spriteY <= _scanline && spriteY + SpriteHeight > _scanline;
            var isInRange = spriteY < nextScanline && nextScanline <= spriteY + SpriteHeight;

            if (isInRange)
            {
                if (_spritesOnScanline >= 8)
                {
                    SpriteOverflow = true;
                    return;
                }

                var _secondaryOamIndex = _spritesOnScanline * 4;

                // Copy the sprite's data to secondary OAM, which contains
                // all sprites that will be drawn on the next scanline.
                _oam.AsSpan(spriteIndex * 4, 4).CopyTo(_secondaryOam.AsSpan(_secondaryOamIndex, 4));
                _spritesOnScanline += 1;

                if (spriteIndex == 0)
                {
                    _spriteZeroHitIsPossible = true;
                }
            }
        }
    }

    private (byte colorIndex, byte palette, bool isBehindBackground, bool isSpriteZero) GetSpritePixel()
    {
        byte spritePixel = 0;
        byte spritePalette = 0;
        bool isBehindBackground = false;
        bool isSpriteZero = false;

        if (!SpritesEnabled)
        {
            return (spritePixel, spritePalette, isBehindBackground, isSpriteZero);
        }

        for (int i = 0; i < _spritesOnScanline; i += 1)
        {
            var sprite = _sprites[i];

            if (sprite.X != 0)
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
            byte compareTo = sprite.FlippedHorizontally ? (byte)0x01 : (byte)0x80;
            byte spritePatternLow = (byte)((sprite.PatternLow & compareTo) > 0 ? 1 : 0);
            byte spritePatternHigh = (byte)((sprite.PatternHigh & compareTo) > 0 ? 1 : 0);
            spritePixel = (byte)((spritePatternHigh << 1) | spritePatternLow);

            // Sprite palettes have a range of 4-7, so we need to add 4 to get the correct range.
            spritePalette = (byte)(sprite.Palette + 4);

            isBehindBackground = sprite.Priority;

            if (spritePixel > 0)
            {
                if (_spriteZeroHitIsPossible && i == 0)
                {
                    // This is sprite 0 (from the OAM table), and we are about to draw a non-zero
                    // pixel value from it. If there is also a non-transparent background pixel at
                    // this location, we will need to set the sprite 0 hit flag. We can't do that
                    // here because we don't know what the background pixel is yet, but we do know
                    // that it is possible for a sprite 0 hit to occur on this scanline.

                    // We know this is sprite 0 because sprites are always evaluated in the order
                    // they appear in OAM.

                    // If _spriteZeroHitIsPossible is false, that means the sprite at index 0 here
                    // is not sprite 0 from OAM - the real sprite 0 is being rendered on this
                    // scanline.

                    isSpriteZero = true;
                }

                // The first sprite pixel we find is the one to draw, so there is no need to check
                // the rest of the sprites.
                break;
            }
        }

        return (spritePixel, spritePalette, isBehindBackground, isSpriteZero);
    }

    private void ClearSecondaryOam()
    {
        Array.Fill<byte>(_secondaryOam, 0xFF);
    }

    private void IncrementCoarseXScroll()
    {
        if (!BackgroundEnabled && !SpritesEnabled)
        {
            return;
        }

        if (_v.CoarseX == 31)
        {
            // Wrap around to next nametable
            _v.CoarseX = 0;

            var currentNameTableX = _v.NameTableX;
            _v.NameTableX = !currentNameTableX;
            return;
        }

        // Regular increment
        _v.CoarseX += 1;
    }

    private void IncrementVerticalScroll()
    {
        if (!BackgroundEnabled && !SpritesEnabled)
        {
            return;
        }

        var fineY = _v.FineY;

        // Regular increment of coarse Y
        if (fineY < 7)
        {
            _v.FineY = (byte)(fineY + 1);
            return;
        }

        _v.FineY = 0;

        var coarseY = _v.CoarseY;
        if (coarseY == 29)
        {
            // Reset coarse Y scroll and switch to next vertical nametable
            _v.CoarseY = 0;
            var currentNameTableY = _v.NameTableY;
            _v.NameTableY = !currentNameTableY;
            return;
        }

        if (coarseY == 31)
        {
            // Reset coarse Y scroll without switching nametable
            _v.CoarseY = 0;
            return;
        }

        // Regular increment
        _v.CoarseY += 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchBackgroundLow()
    {
        return ReadMemory((ushort)(
            // Start with the base address of the pattern table that is
            // selected for backgrounds. This is either $0000 or $1000
            // depending on the PPUCTRL register.
            BackgroundPatternTableAddress
            // Multiply by 16 because each tile is 16 bytes.
            + _bg.NextTileId * 16
            // Add fine Y scroll to select the correct row of pixels
            // within the tile.
            + _v.FineY
        ));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchBackgroundHigh()
    {
        return ReadMemory((ushort)(
            // Same as FetchBackgroundLow (above), but add 8 to get the high
            // byte of the tile data
            BackgroundPatternTableAddress + _bg.NextTileId * 16 + _v.FineY + 8
        ));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchNameTable()
    {
        return ReadMemory((ushort)(0x2000 | (_v.Value & 0x0FFF)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchAttribute()
    {
        var v = _v.Value;
        // Attribute table base address for the current nametable
        var address =
            0x23C0
            | (v & 0x0C00)
            | ((v >> 4) & 0x38)
            | ((v >> 2) & 0x07);

        var attributeByte = ReadMemory((ushort)address);

        // Each attribute byte encodes palette for a 32x32 pixel (4x4 tile) area in 4 quadrants:
        // bits 0-1: top-left, bits 2-3: top-right, bits 4-5: bottom-left, bits 6-7: bottom-right.
        // Determine which quadrant the current tile is in using coarse X/Y.
        var coarseX = _v.CoarseX;
        var coarseY = _v.CoarseY;

        var shift = ((coarseY & 0x02) << 1) | (coarseX & 0x02); // yields 0,2,4,6
        var paletteBits = (byte)((attributeByte >> shift) & 0x03);
        return paletteBits;
    }

    // https://www.nesdev.org/wiki/PPU_memory_map
    private byte ReadMemory(ushort address)
    {
        address &= 0x3FFF;
        Debug.Assert(address < 0x4000);

        switch (address)
        {
            // Nametables and attribute tables
            case < 0x3000:
                return Mapper?.PpuRead(address) ?? 0;

            // Unused memory region
            case < 0x3F00:
                return 0;

            // Palette RAM
            case < 0x4000:
                // $3F00-$3F1F is mirrored across $3F20-$3FFF
                var paletteAddress = address - 0x3F00;
                paletteAddress %= 0x20;

                // Hardware mirrors $3F10/$14/$18/$1C to $3F00/$04/$08/$0C
                // This means that background color 0 is shared across all four
                // background palettes, and sprite color 0 is shared across all four
                // sprite palettes.
                if ((paletteAddress & 0x13) == 0x10)
                {
                    paletteAddress -= 0x10;
                }

                return _paletteRam[paletteAddress];

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(address),
                    "Address out of range for PPU memory"
                );
        }
    }

    private void WriteMemory(ushort address, byte value)
    {
        address &= 0x3FFF;
        Debug.Assert(address < 0x4000);

        switch (address)
        {
            // Pattern tables
            case < 0x2000:
                // Pattern tables are read-only for most games
                break;

            // Nametables and attribute tables
            case < 0x3000:
                Mapper?.PpuWrite(address, value);
                break;

            // Unused memory region
            case < 0x3F00:
                break;

            // Palette RAM
            case < 0x4000:
                // $3F00-$3F1F is mirrored across $3F20-$3FFF
                var paletteAddress = address - 0x3F00;
                paletteAddress %= 0x20;

                // Hardware mirrors $3F10/$14/$18/$1C to $3F00/$04/$08/$0C
                // This means that background color 0 is shared across all four
                // background palettes, and sprite color 0 is shared across all four
                // sprite palettes.
                if ((paletteAddress & 0x13) == 0x10)
                {
                    paletteAddress -= 0x10;
                }

                _paletteRam[paletteAddress] = value;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(address),
                    "Address out of range for PPU memory"
                );
        }
    }

    private void IncrementV()
    {
        // Increment V based on the VRAM increment setting
        ushort increment = (ushort)(VRamIncrement ? 32 : 1);
        _v.Value += increment;
    }

    private int MapCpuToRegisterAddress(int address)
    {
        // PPU registers are mapped to CPU address space 0x2000 - 0x3FFF and
        // the 8 registers are mirrored every 8 bytes in that range.
        Debug.Assert(address >= 0x2000 && address <= 0x3FFF);

        address -= 0x2000;
        address %= 8;

        Debug.Assert(address >= 0 && address < 8);
        return address;
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

    /// <summary>
    /// Get a color from the palette RAM, converting the NES palette index to RGB.
    /// Each palette has 4 colors.
    /// </summary>
    private Color GetPaletteColor(int paletteNumber, int colorIndex)
    {
        if (colorIndex == 0)
        {
            paletteNumber = 0;
        }

        var paletteOffset = (paletteNumber * 4) + colorIndex;
        var paletteIndex = _paletteRam[paletteOffset];
        var color = Palette.Colors[paletteIndex];
        return color;
    }
}
