// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

/// <summary>
/// Writes a byte to the specified address in PPU memory space.
/// </summary>
/// <param name="address">Address to write to in PPU memory space</param>
public interface IPpuWritable
{
    void PpuWrite(ushort address, byte value);
}
