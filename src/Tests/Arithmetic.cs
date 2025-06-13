namespace NesNes.Tests;

using NesNes.Core;

public class Arithmetic
{
    [Theory]
    [InlineData(0x00)]
    [InlineData(0xFF)]
    [InlineData(0xC0)]
    public void Inx(byte value)
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
        FlagsHelper.ValidateZeroAndNegative(cpu.Registers.P, value);
    }
}
