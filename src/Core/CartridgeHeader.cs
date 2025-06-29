// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public record CartridgeHeader
{
    /// <summary>
    /// Size of the cartridge header in bytes.
    /// </summary>
    public const byte Size = 0x10;

    /// <summary>
    /// This is the raw iNES header data. It is 16 bytes long.
    /// </summary>
    /// <remarks>
    /// See https://www.nesdev.org/wiki/INES
    /// </remarks>
    private readonly byte[] _data = new byte[Size];

    public CartridgeHeader(ReadOnlySpan<byte> headerData)
    {
        if (headerData.Length != Size)
        {
            throw new ArgumentException(
                $"Header data must be exactly {Size} bytes long.",
                nameof(headerData)
            );
        }

        headerData.CopyTo(_data);
    }

    /// <summary>
    /// Size of PRG ROM in 16KB units.
    /// </summary>
    public byte PrgPages => _data[4];

    /// <summary>
    /// Size of CHR ROM in 8KB units
    /// </summary>
    public byte ChrPages => _data[5];
}
