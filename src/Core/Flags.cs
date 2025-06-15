// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

[Flags]
public enum Flags
{
    Carry = 0b_0000_0001,
    Zero = 0b_0000_0010,
    InterruptDisable = 0b_0000_0100,
    DecimalMode = 0b_0000_1000,
    BreakCommand = 0b_0001_0000,

    // None/Unused = 0b_0010_0000,
    Overflow = 0b_0100_0000,
    Negative = 0b_1000_0000,
}
