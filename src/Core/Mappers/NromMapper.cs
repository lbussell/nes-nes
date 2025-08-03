// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;

namespace NesNes.Core.Mappers;

internal class NromMapper : IMapper
{
    private readonly CartridgeData _cartridge;

    // 2 nametables, 0x400 bytes each. Through horizontal or vertical
    // mirroring, these end up being mapped to 4 logical nametables.
    // Conceptually, the mapper shouldn't really own the nametables. However,
    // on a real NES, the mapper (in the cartridge) controls how the PPU
    // accesses this memory region, even though the actual nametables CIRAM is
    // located inside the console. It's simpler for us if we just keep the
    // nametables in the mapper for now.
    private readonly byte[] _nametables = new byte[0x800];

    private readonly Banking _chrBanking;
    private readonly Banking _prgBanking;
    private readonly Banking _nametableBanking;

    public NromMapper(CartridgeData cartridge)
    {
        _cartridge = cartridge;

        // NROM can have one or two PRG banks, each 16K in size. If it has only
        // one, then the second slot mirrors the first.
        _prgBanking = Banking.CreatePrg(cartridge.Header, numberOfSlots: 2);
        _prgBanking.SetSlot(slotNumber: 0, bankNumber: 0);
        if (cartridge.Header.PrgPages == 1)
        {
            _prgBanking.SetSlot(slotNumber: 1, bankNumber: 0);
        }
        else
        {
            _prgBanking.SetSlot(slotNumber: 1, bankNumber: 1);
        }

        _chrBanking = Banking.CreateChr(cartridge.Header, numberOfSlots: 1);
        _chrBanking.SetSlot(slotNumber: 0, bankNumber: 0);

        // Nametable memory arrangement:
        // 0x0000-0x03FF: First nametable (Bank 0)
        // 0x0400-0x07FF: Second nametable (Bank 1)
        //
        // Nametable arrangement (from https://www.nesdev.org/wiki/PPU_nametables):
        //
        //      (0,0)     (256,0)     (511,0)
        //        +-----------+-----------+
        //        |           |           |
        //        |   $2000   |   $2400   |
        //        |   Slot 0  |   Slot 1  |
        //        |           |           |
        // (0,240)+-----------+-----------+(511,240)
        //        |           |           |
        //        |   $2800   |   $2C00   |
        //        |   Slot 2  |   Slot 3  |
        //        |           |           |
        //        +-----------+-----------+
        //      (0,479)   (256,479)   (511,479)
        //
        _nametableBanking = Banking.CreateNametable(cartridge.Header);
        if (cartridge.Header.NametableArrangement == NametableArrangement.Horizontal)
        {
            // For horizontal nametable arrangement:
            // $2000 (slot 0) and $2800 (slot 2) contain the first nametable
            // $2400 (slot 1) and $2C00 (slot 3) contain the second nametable
            _nametableBanking.SetSlot(slotNumber: 0, bankNumber: 0);
            _nametableBanking.SetSlot(slotNumber: 1, bankNumber: 1);
            _nametableBanking.SetSlot(slotNumber: 2, bankNumber: 0);
            _nametableBanking.SetSlot(slotNumber: 3, bankNumber: 1);
        }
        else
        {
            // For vertical nametable arrangement:
            // $2000 (slot 0) and $2400 (slot 1) contain the first nametable
            // $2800 (slot 2) and $2C00 (slot 3) contain the second nametable
            _nametableBanking.SetSlot(slotNumber: 0, bankNumber: 0);
            _nametableBanking.SetSlot(slotNumber: 1, bankNumber: 0);
            _nametableBanking.SetSlot(slotNumber: 2, bankNumber: 1);
            _nametableBanking.SetSlot(slotNumber: 3, bankNumber: 1);
        }
    }

    /// <inheritdoc/>
    public byte CpuRead(ushort address)
    {
        // Read from PRG ROM
        var prgAddress = _prgBanking.MapAddress(address);
        return _cartridge.PrgRom[prgAddress];
    }

    /// <inheritdoc/>
    public void CpuWrite(ushort address, byte value)
    {
        // NROM does not support writing to PRG ROM. You get weird bugs in
        // Donkey Kong if you allow writes to PRG ROM here.
        return;
    }

    /// <summary>
    /// Read from the CHR_ROM on behalf of the PPU. Assumes that the caller has
    /// already checked that the address is within the correct bounds for
    /// reading from CHR ROM.
    /// </summary>
    /// <param name="address">Address in PPU memory space.</param>
    public byte PpuRead(ushort address)
    {
        return PpuRead(address, length: 1)[0];
    }

    /// <summary>
    /// Read from the CHR_ROM on behalf of the PPU. Assumes that the caller has
    /// already checked that the address and length are within the correct
    /// bounds for reading from CHR or nametables.
    /// </summary>
    /// <param name="address">Address in PPU memory space.</param>
    public ReadOnlySpan<byte> PpuRead(ushort address, int length = 1)
    {
        // https://www.nesdev.org/wiki/PPU_memory_map
        if (address < 0x2000)
        {
            // Read from CHR ROM
            var chrAddress = _chrBanking.MapAddress(address);
            return _cartridge.ChrRom.Slice(chrAddress, length);
        }
        else if (address < 0x3F00)
        {
            var nametableAddress = _nametableBanking.MapAddress(address);
            return _nametables.AsSpan(nametableAddress, length);
        }

        throw new ArgumentOutOfRangeException(
            nameof(address),
            address,
            "Address must be in the range 0x0000 to 0x3FFF for NROM mapper."
        );
    }

    /// <inheritdoc/>
    public void PpuWrite(ushort address, byte value)
    {
        // https://www.nesdev.org/wiki/PPU_memory_map
        if (address < 0x2000)
        {
            // CHR ROM is read-only on most or all NROM cartridges.
        }
        else if (address < 0x3F00)
        {
            var nametableAddress = _nametableBanking.MapAddress(address);
            _nametables[nametableAddress] = value;
        }
    }
}
