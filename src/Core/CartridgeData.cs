// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class CartridgeData
{
    private const int TrainerSize = 512;

    /// <summary>
    /// Size of a single PRG ROM page in bytes.
    /// </summary>
    public const ushort PrgRomPageSize = 0x4000;

    /// <summary>
    /// Size of a single CHR ROM page in bytes.
    /// </summary>
    public const ushort ChrRomPageSize = 0x2000;

    private readonly byte[] _rom;

    // The current offset of PrgRom in the rom data.
    private int _prgRomOffset;

    // The current offset of PrgRom in the rom data.
    private int _chrRomOffset;

    /// <summary>
    /// The cartridge header, which includes metadata about the cartridge's
    /// layout and other info about the game.
    /// </summary>
    public CartridgeHeader Header { get; }

    /// <summary>
    /// The name of the file that this cartridge data was loaded from.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The total size of the cartridge data in bytes.
    /// </summary>
    public int Size => _rom.Length;

    /// <summary>
    /// PRG ROM contains program code.
    /// </summary>
    public ReadOnlySpan<byte> PrgRom => _rom.AsSpan(_prgRomOffset, PrgRomPageSize);

    /// <summary>
    /// CHR ROM contains graphics data.
    /// </summary>
    public ReadOnlySpan<byte> ChrRom => _rom.AsSpan(_chrRomOffset, ChrRomPageSize);

    /// <summary>
    /// Creates a new <see cref="CartridgeData"/> instance from raw ROM data.
    /// </summary>
    public CartridgeData(Stream cartridgeData, string name = "")
    {
        Span<byte> headerData = stackalloc byte[CartridgeHeader.Size];
        cartridgeData.ReadExactly(headerData);

        Header = new CartridgeHeader(headerData);
        Name = name;

        // Now that we've read and parsed the header, we can go ahead and read
        // the rest of the cartridge data.
        cartridgeData.Seek(0, SeekOrigin.Begin);
        _rom = new byte[cartridgeData.Length];
        var bytesRead = cartridgeData.Read(_rom);
        Console.WriteLine($"Read {bytesRead} bytes of ROM data.");

        _prgRomOffset = CartridgeHeader.Size;
        if (Header.HasTrainer)
        {
            _prgRomOffset += TrainerSize;
        }

        _chrRomOffset = _prgRomOffset + (Header.PrgPages * PrgRomPageSize);
    }
}
