// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace NesNes.Core;

/// <summary>
/// Represents all of the registers in the NES CPU.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public record struct Registers
{
    /// <summary>
    /// Program counter - holds the holds the address of the next instruction
    /// to be executed.
    /// </summary>
    public ushort PC;

    /// <summary>
    /// Stack pointer - The stack is located in [0x0100, 0x01FF]. The stack
    /// pointer holds the address of the top of the stack. The stack grows from
    /// top to bottom. When a byte is pushed to the stack, this address is
    /// decremented. Likewise, when a byte is retrieved from the stack, this
    /// register is incremented.
    /// </summary>
    public byte SP;

    /// <summary>
    /// Accumulator - stores the results of arithmetic, logic, and memory
    /// access operations. It is also used as an input parameter for some
    /// operations.
    /// </summary>
    public byte A;

    /// <summary>
    /// General purpose register similar to Y
    /// </summary>
    public byte X;

    /// <summary>
    /// General purpose register similar to X
    /// </summary>
    public byte Y;

    /// <summary>
    /// Processor status - represents 7 status flags that can be set or unset
    /// depending on the result of the last executed instruction.
    /// </summary>
    /// <remarks>
    /// Also see <see cref="Flags"/>.
    /// </remarks>
    public Flags P;

    public override readonly string ToString()
    {
        return $"A:{A:X2} X:{X:X2} Y:{Y:X2} P:{(byte)P:X2} SP:{SP:X2} PC:{PC:X4}";
    }

    public void Reset()
    {
        PC = 0xC000;
        SP = 0xFD;
        A = 0x00;
        X = 0x00;
        Y = 0x00;
        P = Flags.Unused | Flags.InterruptDisable;
    }

    public static Registers Initial
    {
        get
        {
            var registers = new Registers();
            registers.Reset();
            return registers;
        }
    }
}
