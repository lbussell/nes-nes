// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

internal class VRegister
{
    // Reference: https://www.nesdev.org/wiki/PPU_scrolling#PPU_internal_registers

    private const ushort CoarseXMask = 0b_0000_0000_0001_1111;
    private const ushort CoarseYMask = 0b_0000_0011_1110_0000;
    private const ushort NameTableMask = 0b_0000_1100_0000_0000;
    private const ushort FineYMask = 0b_0111_0000_0000_0000;

    private ushort _value;

    public ushort Value
    {
        get => _value;
        set => _value = value;
    }

    public byte CoarseX
    {
        get => (byte)(_value & CoarseXMask);
        set => _value = (ushort)((_value & ~CoarseXMask) | (value & CoarseXMask));
    }

    public byte CoarseY
    {
        get => (byte)((_value & CoarseYMask) >> 5);
        set => _value = (ushort)((_value & ~CoarseYMask) | ((value & 0x1F) << 5));
    }

    public byte NameTable
    {
        get => (byte)((_value & NameTableMask) >> 10);
        set => _value = (ushort)((_value & ~NameTableMask) | ((value & 0x03) << 10));
    }

    public byte FineY
    {
        get => (byte)((_value & FineYMask) >> 12);
        set => _value = (ushort)((_value & ~FineYMask) | ((value & 0x07) << 12));
    }

    public override string ToString()
    {
        return $"b{Value:B16}"
            + $" (FineY:b{FineY:B3}"
            + $", NameTable:b{NameTable:B2}"
            + $", CoarseY:b{CoarseY:B5}"
            + $", CoarseX:b{CoarseX:B5})";
    }
}
