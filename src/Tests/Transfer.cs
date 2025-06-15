// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT


using NesNes.Core;

namespace NesNes.Tests;

public class Transfer
{
    [Theory]
    [InlineData(0x00)]
    [InlineData(0xFF)]
    [InlineData(0xC0)]
    public void Tax(byte value)
    {
        byte[] program =
        [
            0xA9,   // LDA immediate
            value,  // Load value into the accumulator
            0xAA,   // TAX (Transfer A to X)
            0x00    // BRK (break)
        ];

        var memory = new SimpleMemory(program);
        var cpu = new Cpu(new Registers(), memory);
        cpu.Run();

        cpu.Registers.A.ShouldBe(value);
        cpu.Registers.X.ShouldBe(value);
        cpu.Registers.A.ShouldBe(cpu.Registers.X);
        FlagsHelper.ValidateZeroAndNegative(cpu.Registers.P, value);
    }
}
