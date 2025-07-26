// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

/// <summary>
/// Represents a memory banking system.
/// </summary>
internal class Banking
{
    private readonly int _numberOfBanks;
    private readonly int _slotSize;
    private readonly int _startAddress;

    /// <summary>
    /// Mapping of slots to bank addresses. The index of the array is the slot
    /// number, and the value is the address of the bank that is currently in
    /// that slot. The address is relative to the start of the banks that this
    /// instance manages.
    /// </summary>
    private readonly ushort[] _slots;

    /// <summary>
    /// Creates a memory banking system consisting of slots and banks. Slots
    /// are regions of memory that can be swapped in and out with different
    /// banks.
    /// </summary>
    /// <param name="numberOfSlots">
    /// How many slots the memory region will be divided into.
    /// </param>
    /// <param name="slotSize">
    /// The size of each slot in bytes. Banks are also the same size as slots.
    /// </param>
    /// <param name="startAddress">
    /// The starting address of the memory region, AKA the location of the
    /// first slot.
    /// </param>
    /// <param name="totalBankingSize">
    /// The total size of the ROM/RAM region that this banking will manage.
    /// This refers to the cartridge, not the system memory map.
    /// </param>
    public Banking(int numberOfSlots, int slotSize, int startAddress, int totalBankingSize)
    {
        _slotSize = slotSize;
        _startAddress = startAddress;
        _numberOfBanks = totalBankingSize / slotSize;

        _slots = new ushort[numberOfSlots];
    }

    public static Banking CreatePrg(CartridgeHeader header, int numberOfSlots)
    {
        return new Banking(
            numberOfSlots: numberOfSlots,
            slotSize: MemoryRegions.PrgRomSize / numberOfSlots,
            startAddress: MemoryRegions.PrgRom,
            totalBankingSize: header.PrgRomSize
        );
    }

    public static Banking CreateChr(CartridgeHeader header, int numberOfSlots)
    {
        return new Banking(
            numberOfSlots: numberOfSlots,
            slotSize: PpuConsts.ChrRomSize / numberOfSlots,
            startAddress: 0x0000,
            totalBankingSize: header.ChrRomSize
        );
    }

    public void SetSlot(int slotNumber, int bankNumber)
    {
        _slots[slotNumber] = (ushort)(bankNumber * _slotSize);
    }

    public ushort MapAddress(ushort address)
    {
        // Get the slot number based on the address. The address argument is
        // relative to the start address, so we need to normalize it to be
        // relative to the start of the slots.
        var slotNumber = (address - _startAddress) / _slotSize;

        // We already pre-computed the bank address for this slot in the
        // SetSlot method.
        var bankAddress = _slots[slotNumber];

        // Make sure to wrap around the address just in case it overflows.
        return (ushort)(bankAddress + (address % _slotSize));
    }

    /// <summary>
    /// Given an address in local address space (starting at 0), returns the
    /// address within the slot that this address belongs to.
    /// </summary>
    private int GetBankAddress(ushort localAddress) => localAddress % _slotSize;
}
