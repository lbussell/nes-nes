// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public delegate void RenderPixel(ushort x, ushort y, byte r, byte g, byte b);

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

    /// <summary>
    /// When the PPU enters this scanline, it triggers an NMI (non-masking
    /// interrupt)
    /// </summary>
    public const int VblankScanline = 241;

    // The following are indices into the _registers memory array
    private const int PpuCtrl = 0;
    private const int PpuStatus = 2;
    private const int PpuAddress = 6;
    private const int PpuData = 7;

    // https://www.nesdev.org/wiki/PPU_memory_map
    private const int PatternTablesEnd = 0x2000;
    private const int NameTablesEnd = 0x3000;
    private const int NameTableSize = 1024;
    private const int PaletteRamStart = 0x3F00;
    private const int PaletteRamEnd = 0x4000;

    #endregion

    private readonly PpuAddrRegister _addressRegister = new();
    private readonly byte[] _registers = new byte[MemoryRegions.PpuRegistersSize];
    private readonly IMemory _nameTables;
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

    public Ppu(IMemory? initialNametables = null)
    {
        _nameTables = initialNametables ?? new SimpleMemory(2 * NameTableSize);
    }

    /// <summary>
    /// This is called whenever a pixel is rendered.
    /// </summary>
    public RenderPixel? RenderPixelCallback { get; set; }

    /// <summary>
    /// Called whenever the PPU triggers an NMI (non-maskable interrupt).
    /// </summary>
    public Action? NmiCallback { get; set; }

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

    private bool VblankFlag
    {
        get => GetRegisterBit(PpuStatus, 1 << 7);
        set => SetRegisterBit(PpuStatus, 1 << 7, value);
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
            case PpuData:
                var result = _dataBuffer;
                _dataBuffer = ReadMemory(_addressRegister.Value);
                return result;
            default:
                return _registers[address];
        }
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
            default:
                _registers[address] = value;
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
        if (address < NameTablesEnd)
        {
            // TODO: Implement nametable mirroring
            // For now, just read/write to the first nametable only
            var nameTableAddress = address - 0x2000;
            nameTableAddress %= NameTableSize;
            _nameTables[(ushort)nameTableAddress] = value;
        }
        if (address < PaletteRamStart)
        {
            // Unused memory region
        }
        if (address < PaletteRamEnd)
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
        if (_cycle >= CyclesPerScanline)
        {
            _cycle = 0;
            _scanline += 1;

            // The vblank flag is set at the start of vblank (scanline 241).
            // Reading PPUSTATUS will return the current state of this flag and
            // then clear it. If the vblank flag is not cleared by reading, it
            // will be cleared automatically on dot 1 of the prerender
            // scanline.
            if (_scanline == VblankScanline)
            {
                VblankFlag = true;
            }

            // VBlank is cleared on the first dot of scanline 261.
            if (_scanline == Scanlines - 1)
            {
                VblankFlag = false;
            }

            // Trigger NMI (non-maskable interrupt)
            if (NmiEnabled && VblankFlag)
            {
                // We "read" the PPU status register, so we need to clear the VBlank flag.
                VblankFlag = false;
                NmiCallback?.Invoke();
            }

            if (_scanline >= Scanlines)
            {
                _scanline = 0;
            }
        }

        if (_cycle < DisplayWidth && _scanline < DisplayHeight)
        {
            // var color = NametableToColor(_cycle, _scanline);
            RenderPixelCallback?.Invoke(
                _cycle,
                _scanline,
                s_randomColor.R,
                s_randomColor.G,
                s_randomColor.B
            );
        }

        _cycle += 1;
    }

    private static Color s_randomColor = new(
        (byte)Random.Shared.Next(0xFF),
        (byte)Random.Shared.Next(0xFF),
        (byte)Random.Shared.Next(0xFF)
    );

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
}
