// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public record CartridgeHeader
{
    /// <summary>
    /// Size of the cartridge header in bytes.
    /// </summary>
    public const int Size = 0x10;

    public CartridgeHeader(ReadOnlySpan<byte> headerData)
    {
        if (headerData.Length != Size)
        {
            throw new ArgumentException(
                $"Header data must be exactly {Size} bytes long.",
                nameof(headerData)
            );
        }

        Mapper = (byte)((headerData[6] >> 4) | (headerData[7] & 0xF0));

        PrgPages = headerData[4];
        ChrPages = headerData[5];

        // Header byte 6, bit 0 indicates nametable arrangement:
        // 0 = vertical, 1 = horizontal
        NametableArrangement = (NametableArrangement)(headerData[6] & 0x01);

        UsesPersistentPrgRam = (headerData[6] & 0x02) != 0;
        HasTrainer = (headerData[6] & 0x04) != 0;
        AlternateNametableLayout = (headerData[6] & 0x08) != 0;

        // A file is a NES 2.0 ROM image file if it begins with "NES<EOF>"
        // (same as iNES) and, additionally, the byte at offset 7 has bit 2
        // clear and bit 3 set
        IsNes2Header = (headerData[7] & 0x0C) == 0x08;
    }

    /// <summary>
    /// Size of PRG ROM in 16KB units.
    /// </summary>
    public byte PrgPages { get; }

    /// <summary>
    /// Size of PRG ROM in bytes.
    /// </summary>
    public int PrgRomSize => PrgPages * 0x4000;

    /// <summary>
    /// Size of CHR ROM in 8KB units. Size of 0 indicates that the cart uses
    /// CHR RAM instead.
    /// </summary>
    public byte ChrPages { get; }

    /// <summary>
    /// Size of CHR ROM in bytes. Size of 0 indicates that the cart uses CHR
    /// RAM instead.
    /// </summary>
    public int ChrRomSize => ChrPages * 0x2000;

    public byte Mapper { get; }

    public bool UsesChrRam => ChrPages == 0;

    public NametableArrangement NametableArrangement { get; }

    public bool AlternateNametableLayout { get; }

    public bool UsesPersistentPrgRam { get; }

    /// <summary>
    /// Indicates whether the cartridge has a trainer. A trainer is a small
    /// block of data that is loaded into the CPU's memory before the main
    /// program starts. It is used by some games to store additional data or
    /// to perform some initialization tasks.
    /// </summary>
    /// <remarks>
    /// If present, the trainer is 512 bytes long and is located immediately
    /// before the PRG data.
    /// </remarks>
    public bool HasTrainer { get; }

    public bool IsNes2Header { get; }
}

public enum NametableArrangement
{
    Vertical,
    Horizontal,
}
