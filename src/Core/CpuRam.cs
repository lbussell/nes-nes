// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class CpuRam : ICpuReadable, ICpuWritable
{
    public const int Size = 0x2000;
    public const int InternalSize = 0x0800;

    private readonly byte[] _ram = new byte[InternalSize];

    /// <summary>
    /// Read 8 bits of memory from the specified address in CPU memory space.
    /// </summary>
    public byte CpuRead(ushort address) => Map(address);

    /// <summary>
    /// Write 8 bits of memory to the specified address in CPU memory space.
    /// </summary>
    public void CpuWrite(ushort address, byte value) => Map(address) = value;

    /// <summary>
    /// Returns a reference to the byte at the specified address in CPU memory
    /// space, accounting for mirroring of the NES's internal RAM.
    /// </summary>
    /// <remarks>
    /// The NES has 800 bytes of internal RAM, but it is mirrored every 800
    /// bytes all the way up to 0x2000.
    /// </remarks>
    private ref byte Map(ushort address)
    {
        if (address <= Size)
        {
            return ref _ram[address % InternalSize];
        }

        throw new ArgumentOutOfRangeException(
            nameof(address),
            $"Address {address:X4} is out of range for CPU RAM."
        );
    }
}
