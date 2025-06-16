// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

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
