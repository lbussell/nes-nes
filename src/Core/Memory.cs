// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class Memory : IMemory
{
    private readonly byte[] _internalRam = new byte[MemoryRegions.InternalRamSize];
    private readonly byte[] _ppuRegisters = new byte[MemoryRegions.PpuRegistersSize];

    // This is a placeholder that covers all 64K of memory, until more
    // sophisticated memory mapping/mirroring is implemented.
    private readonly byte[] _memory = new byte[MemoryRegions.TotalSize];

    private readonly byte[] _rom = new byte[2 * MemoryRegions.RomPageSize];

    public void LoadRom(CartridgeData cart)
    {
        // Temporary hacky hack.
        //
        // See this comment on GitHub for more details:
        // https://github.com/PyAndy/Py3NES/issues/1#issuecomment-224071286
        //
        // For now, you can load 0x4000 bytes starting at offset 0x0010, and
        // map that as ROM into both $8000-$BFFF and $C000-$FFFF of the
        // emulated 6502's memory map. You can make an iNES parser once you
        // start trying to actually run Concentration Room or Donkey Kong.

        // cart.PrgRom has already taken into account the header offset (0x10)
        var romData = cart.PrgRom[..CartridgeData.PrgRomPageSize];

        // Map into both $8000-$BFFF and $C000-$FFFF
        var internalRomSpan = _rom.AsSpan();
        var internalRomPage1 = internalRomSpan.Slice(0, MemoryRegions.RomPageSize);
        var internalRomPage2 = internalRomSpan.Slice(
            MemoryRegions.RomPageSize,
            MemoryRegions.RomPageSize
        );

        // Copy the ROM data
        romData.CopyTo(internalRomPage1);
        romData.CopyTo(internalRomPage2);
    }

    /// <inheritdoc/>
    public byte Read8(ushort address)
    {
        return Map(address);
    }

    /// <inheritdoc/>
    public void Write8(ushort address, byte value)
    {
        Map(address) = value;
    }

    /// <summary>
    /// Maps the given address to the appropriate memory region. Takes care of
    /// mirroring and other access restrictions and quirks.
    /// </summary>
    private ref byte Map(ushort address)
    {
        // Mirror internal RAM every 2KB
        if (address >= MemoryRegions.InternalRam && address <= MemoryRegions.InternalRamEnd)
        {
            return ref _internalRam[address % MemoryRegions.InternalRamSize];
        }

        // Mirror PPU registers every 8 bytes
        if (address >= MemoryRegions.PpuRegisters && address <= MemoryRegions.PpuRegistersEnd)
        {
            return ref _ppuRegisters[
                (address - MemoryRegions.PpuRegisters) % MemoryRegions.PpuRegistersSize
            ];
        }

        if (address >= MemoryRegions.RomPage1 && address <= MemoryRegions.RomEnd)
        {
            return ref _rom[address - MemoryRegions.RomPage1];
        }

        return ref _memory[address];
    }
}
