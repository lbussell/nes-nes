// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

/*
 * NES Memory Map
 *
 * ┌─────────────┬───────┬────────────────────────────────────────────────┐
 * │ Address     │ Size  │ Device                                         │
 * │ Range       │       │                                                │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $0000-$07FF │ $0800 │ 2 KB internal RAM                              │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $0800-$0FFF │ $0800 │                                                │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $1000-$17FF │ $0800 │ Mirrors of $0000-$07FF                         │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $1800-$1FFF │ $0800 │                                                │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $2000-$2007 │ $0008 │ NES PPU registers                              │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $2008-$3FFF │ $1FF8 │ Mirrors of $2000-$2007 (repeats every 8 bytes) │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $4000-$4017 │ $0018 │ NES APU and I/O registers                      │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $4018-$401F │ $0008 │ APU and I/O functionality that is normally     │
 * │             │       │ disabled. See CPU Test Mode.                   │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $4020-$FFFF │ $BFE0 │ Unmapped. Available for cartridge use.         │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $6000-$7FFF │ $2000 │ Usually cartridge RAM, when present.           │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $8000-$FFFF │ $8000 │ Usually cartridge ROM and mapper registers.    │
 * └─────────────┴───────┴────────────────────────────────────────────────┘
 */

public interface IMemory
{
    /// <summary>
    /// Index accessor for reading and writing memory at a specific address.
    /// </summary>
    /// <param name="address">The memory address to access.</param>
    /// <returns>The byte value at the specified address when reading.</returns>
    public byte this[ushort address]
    {
        get => Read8(address);
        set => Write8(address, value);
    }

    /// <summary>
    /// Read 8 bits of memory from the specified address.
    /// </summary>
    /// <param name="address">Memory will be read from this address.</param>
    /// <returns>The memory that was read.</returns>
    byte Read8(ushort address);

    /// <summary>
    /// Write 8 bits of memory to the specified address.
    /// </summary>
    /// <param name="address">Memory will be written to this address.</param>
    /// <param name="value">The value to write to memory.</param>
    void Write8(ushort address, byte value);

    /// <summary>
    /// Read 16 bits of memory from the specified address. Bytes are read in
    /// little-endian order, meaning the low byte is read first and the high
    /// byte second.
    /// </summary>
    /// <param name="address">Memory will be read from this address</param>
    /// <returns>The value of the two bytes combined</returns>
    public ushort Read16(ushort address)
    {
        // Read two bytes from the specified address and combine them into a
        // single 16-bit value.
        byte lsb = Read8(address);
        byte msb = Read8((ushort)(address + 1));

        return (ushort)((msb << 8) | lsb);
    }

    /// <summary>
    /// Write 16 bits of memory to the specified address. Bytes are written in
    /// little-endian order, meaning the low byte is written first and the high
    /// byte second.
    /// </summary>
    /// <param name="address">Memory will be written to this address.</param>
    /// <param name="value">
    /// The value to write, which will be split into two bytes and written in
    /// little-endian order.
    /// </param>
    public void Write16(ushort address, ushort value)
    {
        // Little endian - write the low byte first, then the high byte.
        Write8(address, (byte)(value & 0x00FF)); // Low byte
        Write8((ushort)(address + 1), (byte)(value >> 8)); // High byte
    }
}
