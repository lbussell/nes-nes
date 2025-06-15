// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Shouldly;

namespace NesNes.Tests;

public static class ShouldlyExtensions
{
    /// <summary>
    /// Asserts that the actual byte value is equal to the expected value.
    /// </summary>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected value.</param>
    public static void ShouldBe(this byte actual, byte expected)
    {
        ((int)actual).ShouldBe(expected);
    }
}
