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

        PrgPages = headerData[4];
        ChrPages = headerData[5];
        Mapper = (byte)((headerData[6] >> 4) | (headerData[7] & 0xF0));
    }

    /// <summary>
    /// Size of PRG ROM in 16KB units.
    /// </summary>
    public byte PrgPages { get; }

    /// <summary>
    /// Size of CHR ROM in 8KB units. Size of 0 indicates that the cart uses
    /// CHR RAM instead.
    /// </summary>
    public byte ChrPages { get; }

    public byte Mapper { get; }

    public bool UsesChrRam => ChrPages == 0;
}
