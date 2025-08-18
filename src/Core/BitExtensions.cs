// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;

namespace NesNes.Core;

internal static class BitExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SetBit(this int value, int bitPosition)
    {
        return value | (1 << bitPosition);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort SetBit(this ushort value, int bitPosition)
    {
        return (ushort)(value | (1 << bitPosition));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte SetBit(this byte value, int bitPosition)
    {
        return (byte)(value | (1 << bitPosition));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(this int value, int bitPosition)
    {
        return (value & (1 << bitPosition)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(this ushort value, ushort bitPosition)
    {
        return (value & (1 << bitPosition)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(this byte value, byte bitPosition)
    {
        return (value & (1 << bitPosition)) != 0;
    }
}
