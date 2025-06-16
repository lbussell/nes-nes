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

    /// <summary>
    /// Gets the value of the carry flag. This can be used directly in
    /// arithmetic operations.
    /// </summary>
    public readonly byte Carry => P.HasFlag(Flags.Carry) ? (byte)1 : (byte)0;

    public override readonly string ToString()
    {
        return $"""
            PC   A  X  Y  NV1BDIZC SP
            {PC:X4} {A:X2} {X:X2} {Y:X2} {(byte)P:B8} {SP:X2}
            """;
    }

    /// <summary>
    /// Clears the given flag from the processor status register.
    /// </summary>
    /// <param name="flag">This flag will be unset.</param>
    public void ClearFlag(Flags flag) => P &= ~flag;

    /// <summary>
    /// Sets the given flag in the processor status register.
    /// </summary>
    /// <param name="flag">This flag will be set.</param>
    public void SetFlag(Flags flag) => P |= flag;

    /// <summary>
    /// Sets the given flag in the processor status register.
    /// </summary>
    /// <param name="flag">This flag will be set.</param>
    /// <param name="value">Set the flag according to the given value.</param>
    public void SetFlag(Flags flag, bool value)
    {
        if (value)
        {
            SetFlag(flag);
        }
        else
        {
            ClearFlag(flag);
        }
    }

    /// <summary>
    /// Sets the carry flag based on the given value. Given the result of an
    /// operation, sets the carry flag based on whether or not the result
    /// overflowed the max value for a byte. Otherwise, the carry flag is
    /// cleared
    /// </summary>
    /// <param name="value">
    /// The result of an operation. If the operation overflowed a single byte,
    /// then the carry flag will be set.
    /// </param>
    public void SetCarry(int value)
    {
        if (value > 0xFF)
        {
            P |= Flags.Carry;
        }
        else
        {
            P &= ~Flags.Carry;
        }
    }

    /// <summary>
    /// Sets the zero and negative flags based on the given value.
    /// </summary>
    /// <param name="value">
    /// The zero and negative flags will be set according to this value.
    /// </param>
    public void SetZeroAndNegative(byte value)
    {
        SetZero(value);
        SetNegative(value);
    }

    /// <summary>
    /// Sets the zero flag if the given value is zero, otherwise clears it.
    /// </summary>
    /// <param name="value">
    /// The zero flag will be set or unset based on this value.
    /// </param>
    public void SetZero(byte value)
    {
        if (value == 0)
        {
            P |= Flags.Zero; // Set the zero flag
        }
        else
        {
            P &= ~Flags.Zero; // Clear the zero flag
        }
    }

    /// <summary>
    /// Sets the negative flag if the most significant bit of the given
    /// value is set, otherwise clears it.
    /// </summary>
    /// <param name="value">
    /// The negative flag will be set or unset based this value.
    /// </param>
    public void SetNegative(byte value)
    {
        if ((sbyte)value < 0)
        {
            P |= Flags.Negative; // Set the negative flag
        }
        else
        {
            P &= ~Flags.Negative; // Clear the negative flag
        }
    }
}
