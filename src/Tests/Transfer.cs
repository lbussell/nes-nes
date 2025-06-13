namespace NesNes.Tests;

using NesNes.Core;
using Shouldly.ShouldlyExtensionMethods;

public class Transfer
{
    [Theory]
    [InlineData(0x00)]
    [InlineData(0xFF)]
    [InlineData(0xC0)]
    public void TAX(byte value)
    {
        byte[] program =
        [
            0xA9,   // LDA immediate
            value,  // Load value into the accumulator
            0xAA,   // TAX (Transfer A to X)
            0x00    // BRK (break)
        ];

        var cpu = new Cpu();
        cpu.Run(program);

        cpu.Registers.A.ShouldBe(value);
        cpu.Registers.X.ShouldBe(value);
        cpu.Registers.A.ShouldBe(cpu.Registers.X);

        if (value == 0)
        {
            cpu.Registers.P.ShouldHaveFlag(Flags.Zero);
        }
        else
        {
            cpu.Registers.P.ShouldNotHaveFlag(Flags.Zero);
        }

        if ((sbyte)value < 0)
        {
            cpu.Registers.P.ShouldHaveFlag(Flags.Negative);
        }
        else
        {
            cpu.Registers.P.ShouldNotHaveFlag(Flags.Negative);
        }
    }
}
