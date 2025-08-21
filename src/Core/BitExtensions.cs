// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;

namespace NesNes.Core;

internal static class BitExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SetBit(this int n, int bitPosition)
    {
        return n | (1 << bitPosition);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort SetBit(this ushort n, int bitPosition)
    {
        return (ushort)(n | (1 << bitPosition));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte SetBit(this byte n, int bitPosition)
    {
        return (byte)(n | (1 << bitPosition));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SetBit(this int n, int bitPosition, bool value)
    {
        if (value)
        {
            return n.SetBit(bitPosition);
        }
        else
        {
            return n.ClearBit(bitPosition);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort SetBit(this ushort n, ushort bitPosition, bool value)
    {
        if (value)
        {
            return n.SetBit(bitPosition);
        }
        else
        {
            return n.ClearBit(bitPosition);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte SetBit(this byte n, byte bitPosition, bool value)
    {
        if (value)
        {
            return n.SetBit(bitPosition);
        }
        else
        {
            return n.ClearBit(bitPosition);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(this int n, int bitPosition)
    {
        return (n & (1 << bitPosition)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(this ushort n, ushort bitPosition)
    {
        return (n & (1 << bitPosition)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(this byte n, byte bitPosition)
    {
        return (n & (1 << bitPosition)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ClearBit(this int n, int bitPosition)
    {
        return n & ~(1 << bitPosition);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ClearBit(this ushort n, ushort bitPosition)
    {
        return (ushort)(n & ~(1 << bitPosition));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ClearBit(this byte n, byte bitPosition)
    {
        return (byte)(n & ~(1 << bitPosition));
    }
}
