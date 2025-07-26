// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public interface IMemory
{
    /// <summary>
    /// Read 8 bits of memory from the specified address.
    /// </summary>
    /// <param name="address">Memory will be read from this address.</param>
    /// <returns>The memory that was read.</returns>
    byte Read(ushort address);

    /// <summary>
    /// Write 8 bits of memory to the specified address.
    /// </summary>
    /// <param name="address">Memory will be written to this address.</param>
    /// <param name="value">The value to write to memory.</param>
    void Write(ushort address, byte value);
}
