// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public enum MirroringMode
{
    Horizontal,
    Vertical,
    FourScreen,
}

public record CartridgeData
{
    /// <summary>
    /// Size of a single PRG ROM page in bytes.
    /// </summary>
    public const ushort PrgRomPageSize = 0x4000;

    /// <summary>
    /// Size of a single CHR ROM page in bytes.
    /// </summary>
    public const ushort ChrRomPageSize = 0x2000;

    private readonly byte[] _prgRom;
    private readonly byte[] _chrRom;

    /// <summary>
    /// The cartridge header, which includes metadata about the cartridge's
    /// layout and other info about the game.
    /// </summary>
    public CartridgeHeader Header { get; }

    /// <summary>
    /// Contains the PRG ROM data, which contains program code.
    /// </summary>
    public ReadOnlySpan<byte> PrgRom => _prgRom;

    /// <summary>
    /// Contains the CHR ROM data, which is used for graphics.
    /// </summary>
    public ReadOnlySpan<byte> ChrRom => _chrRom;

    private CartridgeData(CartridgeHeader header, byte[] prgRom, byte[] chrRom)
    {
        Header = header;
        _chrRom = chrRom;
        _prgRom = prgRom;
    }

    /// <summary>
    /// Creates a new <see cref="CartridgeData"/> instance from raw ROM data.
    /// </summary>
    public static CartridgeData FromData(ReadOnlySpan<byte> cartridgeData)
    {
        var headerData = cartridgeData[..CartridgeHeader.Size];
        var header = new CartridgeHeader(headerData);

        var prgRomOffset = CartridgeHeader.Size;
        var prgRom = cartridgeData.Slice(prgRomOffset, header.PrgPages * PrgRomPageSize).ToArray();
        var chrRom = cartridgeData
            .Slice(prgRomOffset + prgRom.Length, header.ChrPages * ChrRomPageSize)
            .ToArray();

        return new CartridgeData(header, prgRom, chrRom);
    }
}
