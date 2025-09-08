// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

internal struct VRegister
{
    // Reference: https://www.nesdev.org/wiki/PPU_scrolling#PPU_internal_registers

    private const ushort CoarseXMask = 0b_0000_0000_0001_1111;
    private const ushort CoarseYMask = 0b_0000_0011_1110_0000;
    private const ushort NameTableMask = 0b_0000_1100_0000_0000;
    private const ushort NameTableXMask = 0b_0000_0100_0000_0000;
    private const ushort NameTableYMask = 0b_0000_1000_0000_0000;
    private const ushort FineYMask = 0b_0111_0000_0000_0000;
    private ushort _value;

    public ushort Value
    {
        readonly get => _value;
        set => _value = (ushort)(value & 0x7FFF);
    }

    /// <summary>
    /// Low 5 bits (0-4) of the register
    /// </summary>
    public byte CoarseX
    {
        readonly get => (byte)(Value & CoarseXMask);
        set => Value = (ushort)((Value & ~CoarseXMask) | (value & CoarseXMask));
    }

    /// <summary>
    /// Second 5 bits (5-9) of the register
    /// </summary>
    public byte CoarseY
    {
        readonly get => (byte)((Value & CoarseYMask) >> 5);
        set => Value = (ushort)((Value & ~CoarseYMask) | ((value & 0x1F) << 5));
    }

    /// <summary>
    /// Bits 10-11 of the register, which represent which nametable is being
    /// used
    /// </summary>
    public byte NameTable
    {
        readonly get => (byte)((Value & NameTableMask) >> 10);
        set => Value = (ushort)((Value & ~NameTableMask) | ((value & 0x03) << 10));
    }

    /// <summary>
    /// Bit 10 of the register, which represents the X component of the
    /// nametable selection
    /// </summary>
    public bool NameTableX
    {
        readonly get => (Value & NameTableXMask) != 0;
        set => Value = (ushort)(value ? (Value | NameTableXMask) : (Value & ~NameTableXMask));
    }

    /// <summary>
    /// Bit 11 of the register, which represents the Y component of the
    /// nametable selection
    /// </summary>
    public bool NameTableY
    {
        readonly get => (Value & NameTableYMask) != 0;
        set => Value = (ushort)(value ? (Value | NameTableYMask) : (Value & ~NameTableYMask));
    }

    /// <summary>
    /// Highest 3 bits of the register (out of 15 bits total - the high bit
    /// is unused)
    /// </summary>
    public byte FineY
    {
        readonly get => (byte)((Value & FineYMask) >> 12);
        set => Value = (ushort)((Value & ~FineYMask) | ((value & 0x07) << 12));
    }

    public override string ToString()
    {
        return $"${Value:X4} b{Value:B16}"
            + $" (FineY:b{FineY:B3}"
            + $", NameTable:b{NameTable:B2}"
            + $", CoarseY:b{CoarseY:B5}"
            + $", CoarseX:b{CoarseX:B5})";
    }

    public static implicit operator ushort(VRegister reg) => reg.Value;
}
