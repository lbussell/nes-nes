namespace NesNes.Tests;

using NesNes.Core;
using Shouldly.ShouldlyExtensionMethods;

public class Load
{
    [Theory]
    [InlineData(0x00)]
    [InlineData(0xFF)]
    [InlineData(0xC0)]
    public void LdaImmediate(byte value)
    {
        byte[] program =
        [
            0xA9,   // LDA immediate
            value,  // Load value into the accumulator
            0x00    // BRK (break)
        ];

        var cpu = new Cpu();
        cpu.Run(program);

        cpu.Registers.A.ShouldBe(value);

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
