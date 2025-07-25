// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

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

    // public ReadOnlySpan<byte> RomPage1 => _prgRom.AsSpan(0, PrgRomPageSize);

    // public ReadOnlySpan<byte> RomPage2 => _prgRom.AsSpan(PrgRomPageSize, PrgRomPageSize);

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
    public static CartridgeData FromBytes(FileStream cartridgeData)
    {
        Span<byte> headerData = stackalloc byte[CartridgeHeader.Size];
        cartridgeData.ReadExactly(headerData);

        var header = new CartridgeHeader(headerData);

        var prgRomOffset = CartridgeHeader.Size;
        var prgRom = cartridgeData.Slice(prgRomOffset, header.PrgPages * PrgRomPageSize).ToArray();
        var chrRom = cartridgeData
            .Slice(prgRomOffset + prgRom.Length, header.ChrPages * ChrRomPageSize)
            .ToArray();

        return new CartridgeData(header, prgRom, chrRom);
    }
}

internal static class StreamExtensions
{
    /// <summary>
    /// Reads exactly the specified number of bytes from the stream into the buffer.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="buffer">The buffer to read data into. The method will read exactly buffer.Length bytes.</param>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends before the buffer is completely filled.</exception>
    private static void ReadExactly(this Stream stream, Span<byte> buffer)
    {
        // Track how many bytes we've successfully read so far
        int total = 0;

        // Keep reading until we've filled the entire buffer
        while (total < buffer.Length)
        {
            // Read into the remaining portion of the buffer
            int n = stream.Read(buffer.Slice(total));

            // If Read() returns 0, we've reached the end of the stream
            if (n == 0)
            {
                throw new EndOfStreamException();
            }

            // Update our progress counter
            total += n;
        }
    }
}
