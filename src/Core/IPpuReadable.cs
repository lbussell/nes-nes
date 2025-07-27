// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public interface IPpuReadable
{
    /// <summary>
    /// Reads a byte from the specified address in PPU memory space.
    /// </summary>
    /// <param name="address">Address to read from in PPU memory space</param>
    /// <returns>The value that was read</returns>
    byte PpuRead(ushort address);

    /// <summary>
    /// Gets a span of contiguous bytes from PPU memory space.
    /// </summary>
    /// <param name="address">Starting address in PPU memory space</param>
    /// <param name="length">Length of the span to create</param>
    /// <returns>Span of bytes over the given range of memory</returns>
    ReadOnlySpan<byte> PpuRead(ushort address, int length);
}
