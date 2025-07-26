// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

internal class NromMapper : ICpuReadable, ICpuWritable, IPpuReadable, IPpuWritable
{
    private readonly CartridgeData _cartridge;

    private readonly Banking _chrBanking;

    private readonly Banking _prgBanking;

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
        // Read from CHR ROM
        var chrAddress = _chrBanking.MapAddress(address);
        return _cartridge.ChrRom[chrAddress];
    }

    /// <inheritdoc/>
    public void PpuWrite(ushort address, byte value)
    {
        return;
    }
}
