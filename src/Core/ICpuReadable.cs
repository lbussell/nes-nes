// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public interface ICpuReadable
{
    /// <summary>
    /// Reads a byte from the specified address in CPU memory space.
    /// </summary>
    /// <param name="address">Address to read from in CPU memory space</param>
    /// <returns>The value that was read</returns>
    byte CpuRead(ushort address);
}
