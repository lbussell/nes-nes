// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public enum MirroringMode
{
    Horizontal,
    Vertical,
    FourScreen,
}

public record Cartridge
{
    private byte[] _rom;

    public ReadOnlySpan<byte> Data => _rom;

    /// <summary>
    /// Number of 16KB ROM banks (PRG ROM)
    /// </summary>
    public byte RomBanks { get; }

    /// <summary>
    /// Number of 8KB VROM banks (CHR ROM)
    /// </summary>
    public byte VRomBanks { get; }

    public byte RomMapperType { get; }

    public bool HasTrainer { get; }

    public bool HasBattery { get; }

    public MirroringMode ScreenMirroring { get; }

    public string FormatType { get; }

    public Cartridge(byte[] rom)
    {
        ReadOnlySpan<byte> header = rom.AsSpan(0, 16);

        ReadOnlySpan<byte> nesString = header[..4];

        if (
            nesString[0] != 'N'
            || nesString[1] != 'E'
            || nesString[2] != 'S'
            || nesString[3] != 0x1A
        )
        {
            throw new InvalidDataException("Invalid NES ROM header.");
        }

        var controlByte1 = header[6];
        var controlByte2 = header[7];

        RomBanks = header[4];
        VRomBanks = header[5];

        var usesVerticalMirroring = (controlByte1 & 0b_0000_0001) != 0;
        ScreenMirroring = usesVerticalMirroring ? MirroringMode.Vertical : MirroringMode.Horizontal;

        var usesFourScreenMode = (controlByte1 & 0b_0000_1000) != 0;
        if (usesFourScreenMode)
        {
            ScreenMirroring = MirroringMode.FourScreen;
        }

        HasBattery = (controlByte1 & 0b_0000_0010) != 0;
        HasTrainer = (controlByte1 & 0b_0000_0100) != 0;

        FormatType = (controlByte2 & 0b_1100) >> 2 == 2 ? "NES 2.0" : "NES 1.0";
        RomMapperType = (byte)((controlByte1 >> 4) | (controlByte2 & 0xF0));

        _rom = rom;
    }
}
