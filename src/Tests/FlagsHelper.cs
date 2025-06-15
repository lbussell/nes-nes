// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using Shouldly.ShouldlyExtensionMethods;

namespace NesNes.Tests;

public static class FlagsHelper
{
    /// <summary>
    /// Validates the Zero and Negative flags are set correctly based on the
    /// provided byte value.
    /// </summary>
    /// <param name="flags">
    /// This flag enum is checked for the correct flags.
    /// </param>
    /// <param name="value">
    /// The flag enum is checked based on this byte value. If it is zero, the
    /// Zero flag should be set. If it is negative (most significant bit is
    /// set), the Negative flag should be set.
    /// </param>
    public static void ValidateZeroAndNegative(Flags flags, byte value)
    {
        if (value == 0)
        {
            flags.ShouldHaveFlag(Flags.Zero);
        }
        else
        {
            flags.ShouldNotHaveFlag(Flags.Zero);
        }

        if ((sbyte)value < 0)
        {
            flags.ShouldHaveFlag(Flags.Negative);
        }
        else
        {
            flags.ShouldNotHaveFlag(Flags.Negative);
        }
    }
}
