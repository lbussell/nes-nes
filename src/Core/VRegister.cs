// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

internal struct VRegister
{
    // Reference: https://www.nesdev.org/wiki/PPU_scrolling#PPU_internal_registers

    private const ushort CoarseXMask = 0b_0000_0000_0001_1111;
    private const ushort CoarseYMask = 0b_0000_0011_1110_0000;
    private const ushort NameTableMask = 0b_0000_1100_0000_0000;
    private const ushort FineYMask = 0b_0111_0000_0000_0000;

    public ushort Value { get; set; }

    public byte CoarseX
    {
        get => (byte)(Value & CoarseXMask);
        set => Value = (ushort)((Value & ~CoarseXMask) | (value & CoarseXMask));
    }

    public byte CoarseY
    {
        readonly get => (byte)((Value & CoarseYMask) >> 5);
        set => Value = (ushort)((Value & ~CoarseYMask) | ((value & 0x1F) << 5));
    }

    public byte NameTable
    {
        readonly get => (byte)((Value & NameTableMask) >> 10);
        set => Value = (ushort)((Value & ~NameTableMask) | ((value & 0x03) << 10));
    }

    public byte FineY
    {
        readonly get => (byte)((Value & FineYMask) >> 12);
        set => Value = (ushort)((Value & ~FineYMask) | ((value & 0x07) << 12));
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
