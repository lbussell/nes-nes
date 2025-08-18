// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace NesNes.Core;

public class PpuV2 : IPpu
{
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

    // PPU state
    private long _frame;
    private int _scanline;
    private int _cycle;

    public long Frame => _frame;
    public int Scanline => _scanline;
    public int Cycle => _cycle;
    public Span<byte> Oam => _oam;

    public IMapper? Mapper { get; set; } = null;
    public bool NonMaskableInterruptPin { get; }

    public RenderPixel? OnRenderPixel { get; set; }

    // Bits 0 and 1: 0=$2000, 1=$2400, 2=$2800, 3=$2C00
    private byte BaseNametableAddress => _t.NameTable;
    private bool VRamIncrement => _registers[PpuCtrl_2000].GetBit(2);
    private ushort SpritePatternTableAddress => (ushort)((_registers[PpuCtrl_2000] & 0x08) << 9);
    private ushort BackgroundPatternTableAddress => (ushort)((_registers[PpuCtrl_2000] & 0x10) << 8);
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

    private bool SpriteOverflow => _registers[PpuStatus_2002].GetBit(5);
    private bool SpriteZeroHit => _registers[PpuStatus_2002].GetBit(6);
    private bool VblankFlag => _registers[PpuStatus_2002].GetBit(7);

    public byte CpuRead(ushort address)
    {
        var register = MapCpuToRegisterAddress(address);
        Debug.Assert(register >= 0 && register < _registers.Length);

        switch (register)
        {
            case PpuCtrl_2000:
                break;
            case PpuMask_2001:
                break;
            case PpuStatus_2002:
                break;
            case OamAddr_2003:
                break;
            case OamData_2004:
                break;
            case PpuScroll_2005:
                break;
            case PpuAddr_2006:
                break;
            case PpuData_2007:
                break;
        }

        return 0;
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
                break;

            case PpuMask_2001:
                // Writes to the following registers are ignored if earlier than ~29658 CPU clocks
                // after reset: PPUCTRL, PPUMASK, PPUSCROLL, PPUADDR.
                if (_frame == 0 && _scanline < 258)
                {
                    break;
                }
                break;

            case PpuStatus_2002:
                break;

            case OamAddr_2003:
                break;

            case OamData_2004:
                break;

            case PpuScroll_2005:
                // Writes to the following registers are ignored if earlier than ~29658 CPU clocks
                // after reset: PPUCTRL, PPUMASK, PPUSCROLL, PPUADDR.
                if (_frame == 0 && _scanline < 258)
                {
                    break;
                }
                break;

            case PpuAddr_2006:
                // Writes to the following registers are ignored if earlier than ~29658 CPU clocks
                // after reset: PPUCTRL, PPUMASK, PPUSCROLL, PPUADDR.
                if (_frame == 0 && _scanline < 258)
                {
                    break;
                }
                break;

            case PpuData_2007:
                break;
        }
    }

    public void WriteOam(byte address, byte value)
    {
        _oam[address] = value;
    }

    public void Step()
    {
        var debugColor = DebugColors.Blank;

        // This method should closely follow the PPU timing diagram at
        // https://www.nesdev.org/w/images/default/4/4f/Ppu.svg

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
                _frame += 1;
                _scanline = 0;
            }
        }
    }

    // https://www.nesdev.org/wiki/PPU_memory_map
    private byte ReadMemory(ushort address)
    {
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
                _paletteRam[paletteAddress] = value;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(address),
                    "Address out of range for PPU memory"
                );
        }
    }

    private int MapCpuToRegisterAddress(int address)
    {
        // PPU registers are mapped to CPU address space 0x2000 - 0x3FFF and
        // the 8 registers are mirrored every 8 bytes in that range.
        Debug.Assert(address >= 0x2000 && address <= 0x3FFF);

        address -= 0x2000;
        address %= 8;
        return _registers[address];
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
