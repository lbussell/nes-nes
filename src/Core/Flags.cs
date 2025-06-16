// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

[Flags]
public enum Flags
{
    Carry = 0b_0000_0001,

    /// <summary>
    /// After most instructions that have a value result, this flag will either
    /// be set or cleared based on whether or not that value is equal to zero.
    /// </summary>
    Zero = 0b_0000_0010,

    InterruptDisable = 0b_0000_0100,

    /// <summary>
    /// On the NES, decimal mode is disabled and so this flag has no effect.
    /// However, it still exists and can be observed and modified, as normal.
    /// </summary>
    DecimalMode = 0b_0000_1000,

    /// <summary>
    /// While there are only six flags in the processor status register within
    /// the CPU, the value pushed to the stack contains additional state in bit
    /// 4 called the B flag that can be useful to software. The value of B
    /// depends on what caused the flags to be pushed. Note that this flag does
    /// not represent a register that can hold a value, but rather a transient
    /// signal in the CPU controlling whether it was processing an interrupt
    /// when the flags were pushed. B is 0 when pushed by interrupts (NMI and
    /// IRQ) and 1 when pushed by instructions (BRK and PHP).
    /// </summary>
    /// <remarks>
    /// See https://www.nesdev.org/wiki/Status_flags#The_B_flag
    /// </remarks>
    B = 0b_0001_0000,

    /// <summary>
    /// This flag is always pushed as 1.
    /// </summary>
    Unused = 0b_0010_0000,

    Overflow = 0b_0100_0000,

    Negative = 0b_1000_0000,
}
