// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class Memory : IMemory
{
    private readonly byte[] _internalRam = new byte[MemoryRegions.InternalRamSize];

    // This is a placeholder that covers all 64K of memory, until more
    // sophisticated memory mapping/mirroring is implemented.
    private readonly byte[] _memory = new byte[MemoryRegions.TotalSize];

    private readonly byte[] _rom = new byte[2 * MemoryRegions.RomPageSize];

    private Span<byte> RomPage1 => _rom.AsSpan(0, MemoryRegions.RomPageSize);

    private Span<byte> RomPage2 =>
        _rom.AsSpan(MemoryRegions.RomPageSize, MemoryRegions.RomPageSize);

    private readonly IMemoryListener[] _listeners;

    /// <summary>
    /// Creates a new instance of <see cref="Memory"/>
    /// </summary>
    /// <param name="listeners">
    /// A collection of memory listeners that may be interested in intercepting
    /// or listening to memory reads and writes.
    /// </param>
    public Memory(IEnumerable<IMemoryListener> listeners)
    {
        _listeners = listeners
            // Sort listeners by memory addresses so that we can efficiently
            // check if listeners are interested in a specific address.
            .OrderBy(l => l.MemoryRange.Start)
            .ThenBy(l => l.MemoryRange.End)
            .ToArray();
    }

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
        ReadOnlySpan<byte> prgRomData = cart.PrgRom[..CartridgeData.PrgRomPageSize];

        // Copy the ROM data into both $8000-$BFFF and $C000-$FFFF
        prgRomData.CopyTo(RomPage1);
        prgRomData.CopyTo(RomPage2);
    }

    /// <inheritdoc/>
    public byte Read8(ushort address)
    {
        foreach (IMemoryListener listener in _listeners)
        {
            if (address >= listener.MemoryRange.Start && address < listener.MemoryRange.End)
            {
                // If the listener handles this address, delegate the read to
                // the listener. If the listener returned false, it hears the
                // read but we'll continue to the next listener.
                if (listener.ListenRead(address, out byte value))
                {
                    return value;
                }
            }
        }

        // Fall back to old behavior. TODO: Remove this once other components
        // are updated to implement IMemoryListener.
        return Map(address);
    }

    /// <inheritdoc/>
    public void Write8(ushort address, byte value)
    {
        foreach (IMemoryListener listener in _listeners)
        {
            if (address >= listener.MemoryRange.Start && address < listener.MemoryRange.End)
            {
                // If the listener handled the write, we can stop here. If it
                // returned false, it heard the write but we'll continue to the
                // next listener.
                if (listener.ListenWrite(address, value))
                {
                    return;
                }
            }
        }

        // Fall back to old behavior. TODO: Remove this once other components
        // are updated to implement IMemoryListener.
        Map(address) = value;
    }

    /// <summary>
    /// Maps the given address to the appropriate memory region. Takes care of
    /// mirroring and other access restrictions and quirks.
    /// </summary>
    /// <remarks>
    /// This should be removed once other components (cartridge, etc.) are
    /// updated to implement <see cref="IMemoryListener"/> and handle their own
    /// memory access.
    /// </remarks>
    private ref byte Map(ushort address)
    {
        // Mirror internal RAM every 2KB
        if (address >= MemoryRegions.InternalRam && address <= MemoryRegions.InternalRamEnd)
        {
            return ref _internalRam[address % MemoryRegions.InternalRamSize];
        }

        if (address >= MemoryRegions.RomPage1 && address <= MemoryRegions.RomEnd)
        {
            return ref _rom[address - MemoryRegions.RomPage1];
        }

        return ref _memory[address];
    }
}
