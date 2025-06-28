// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public interface IMemoryListener
{
    /// <summary>
    /// The range of addresses that this listener is interested in.
    /// </summary>
    MemoryRange MemoryRange { get; }

    /// <summary>
    /// Reads a byte from the specified address.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    /// <returns>Returns true if the read was handled.</returns>
    bool Read(ushort address, out byte value);

    /// <summary>
    /// Write a byte to the specified address.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    /// <returns>Returns true if the write was handled.</returns>
    bool Write(ushort address, byte value);
}
