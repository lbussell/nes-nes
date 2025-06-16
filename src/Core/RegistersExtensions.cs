// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public static class RegistersExtensions
{
    extension(ref Registers registers)
    {
        /// <summary>
        /// Gets the value of the carry flag. This can be used directly in
        /// arithmetic operations.
        /// </summary>
        public byte Carry => registers.P.HasFlag(Flags.Carry) ? (byte)1 : (byte)0;

        /// <summary>
        /// Sets the given flag in the processor status register.
        /// </summary>
        /// <param name="flag">This flag will be set.</param>
        /// <param name="value">Set the flag according to the given value.</param>
        public void SetFlag(Flags flag, bool value = true)
        {
            if (value)
            {
                registers.P |= flag;
            }
            else
            {
                registers.P &= ~flag;
            }
        }

        /// <summary>
        /// Clears the given flag from the processor status register.
        /// </summary>
        /// <param name="flag">This flag will be unset.</param>
        public void ClearFlag(Flags flag) => registers.SetFlag(flag, false);

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
        public void SetCarry(int value) =>
            registers.SetFlag(Flags.Carry, value > 0xFF);

        /// <summary>
        /// Sets the zero and negative flags based on the given value.
        /// </summary>
        /// <param name="value">
        /// The zero and negative flags will be set according to this value.
        /// </param>
        public void SetZeroAndNegative(byte value)
        {
            registers.SetZero(value);
            registers.SetNegative(value);
        }

        /// <summary>
        /// Sets the zero flag if the given value is zero, otherwise clears it.
        /// </summary>
        /// <param name="value">
        /// The zero flag will be set or unset based on this value.
        /// </param>
        public void SetZero(byte value) =>
            registers.SetFlag(Flags.Zero, value == 0);

        /// <summary>
        /// Sets the negative flag if the most significant bit of the given
        /// value is set, otherwise clears it.
        /// </summary>
        /// <param name="value">
        /// The negative flag will be set or unset based this value.
        /// </param>
        public void SetNegative(byte value) =>
            registers.SetFlag(Flags.Negative, (value & 0b_1000_0000) != 0);
    }
}
