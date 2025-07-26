// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

/// <summary>
/// Writes a byte to the specified address in CPU memory space.
/// </summary>
/// <param name="address">Address to write to in CPU memory space</param>
public interface ICpuWritable
{
    void CpuWrite(ushort address, byte value);
}
