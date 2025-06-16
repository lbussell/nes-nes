// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class Memory : IMemory, IRomLoader
{
    private readonly byte[] _internalRam = new byte[MemoryRegions.InternalRamSize];
    private readonly byte[] _ppuRegisters = new byte[MemoryRegions.PpuRegistersSize];

    // This is a placeholder that covers all 64K of memory, until more
    // sophisticated memory mapping/mirroring is implemented.
    private readonly byte[] _memory = new byte[MemoryRegions.TotalSize];

    private readonly byte[] _rom = new byte[MemoryRegions.RomSize];

    public void LoadRom(byte[] rom)
    {
        if (rom.Length > MemoryRegions.RomSize)
        {
            throw new ArgumentException(
                $"ROM is too big. Maximum size is {MemoryRegions.RomSize} bytes."
            );
        }

        Array.Copy(
            sourceArray: rom,
            sourceIndex: 0,
            destinationArray: _rom,
            destinationIndex: MemoryRegions.RomStart,
            length: rom.Length
        );
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

        return ref _memory[address];
    }
}
