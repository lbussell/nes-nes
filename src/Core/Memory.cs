// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class Memory : IMemory
{
    private readonly Ppu? _ppu;

    private readonly byte[] _internalRam = new byte[MemoryRegions.InternalRamSize];

    private CartridgeData? _cartridge = null;

    private readonly IMemoryListener[] _listeners;

    /// <summary>
    /// Creates a new instance of <see cref="Memory"/>
    /// </summary>
    /// <param name="listeners">
    /// A collection of memory listeners that may be interested in intercepting
    /// or listening to memory reads and writes.
    /// </param>
    public Memory(Ppu? ppu = null, Controllers? controllers = null)
    {
        _ppu = ppu;
        var listeners = new List<IMemoryListener>();

        if (ppu is not null)
        {
            listeners.Add(ppu);
        }

        if (controllers is not null)
        {
            listeners.Add(controllers);
        }

        _listeners = listeners.ToArray();
    }

    public Action TickCpu { get; set; } = () => { };

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

        _cartridge = cart;
    }

    /// <inheritdoc/>
    public byte Read(ushort address)
    {
        byte value = 0;
        bool wasHandled = false;

        foreach (IMemoryListener listener in _listeners)
        {
            if (address >= listener.MemoryRange.Start && address <= listener.MemoryRange.End)
            {
                // If the listener handles this address, delegate the read to
                // the listener. If the listener returned false, it hears the
                // read but we'll continue to the next listener.
                if (listener.ListenRead(address, out byte readValue))
                {
                    wasHandled = true;
                    value = readValue;
                }
            }
        }

        if (wasHandled)
        {
            return value;
        }

        // No listeners handled the read, so fall back to old behavior.
        // TODO: Remove this once other components are updated to implement
        // IMemoryListener.

        // Mirror internal RAM every 2KB
        if (address >= MemoryRegions.InternalRam && address <= MemoryRegions.InternalRamEnd)
        {
            return _internalRam[address % MemoryRegions.InternalRamSize];
        }

        if (address >= MemoryRegions.PrgRom && address <= MemoryRegions.PrgRomEnd)
        {
            var romAddress = address - MemoryRegions.PrgRom;
            if (_cartridge is not null)
            {
                return _cartridge.PrgRom[romAddress];
            }
        }

        return 0;
    }

    /// <inheritdoc/>
    public void Write(ushort address, byte value)
    {
        if (address == MemoryRegions.OamDma)
        {
            // OAM DMA transfer. The value written to the OAM DMA register is
            // the source address page to copy data from. All bytes from the
            // source page of CPU memory will be copied to OAM memory.
            DoOamDma(sourcePage: value);
            return;
        }

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

        // Mirror internal RAM every 2KB
        if (address >= MemoryRegions.InternalRam && address <= MemoryRegions.InternalRamEnd)
        {
            _internalRam[address % MemoryRegions.InternalRamSize] = value;
        }

        if (address >= MemoryRegions.PrgRom && address <= MemoryRegions.PrgRomEnd)
        {
            // TODO: Some games have cartridge RAM. But for now, just ignore
            // writes to the cartridge ROM.

            // var romAddress = address - MemoryRegions.RomPage1;
            // _cartridge?.PrgRom[romAddress] = value;
        }
    }

    private void DoOamDma(byte sourcePage)
    {
        // There are a couple of dummy writes/ticks at the beginning of OAM DMA
        TickCpu();
        TickCpu();

        byte data;
        for (int i = 0; i <= 0xFF; i++)
        {
            data = Read((ushort)((sourcePage << 8) + i));
            TickCpu();

            _ppu?.WriteOam((byte)i, data);
            TickCpu();
        }
    }
}
