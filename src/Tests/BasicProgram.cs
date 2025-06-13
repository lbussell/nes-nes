namespace NesNes.Tests;

using NesNes.Core;

public class BasicProgram
{
    [Fact]
    public void RunsBasicProgram()
    {
        byte[] program =
        [
            0xA9, // LDA $C0    // A = 0xC0
            0xC0,
            0xAA, // TAX        // X,Z,N = A
            0xE8, // INX        // X,Z,N = X+1
            0x00  // BRK        // stop
        ];

        var cpu = new Cpu();
        cpu.Run(program);

        cpu.Registers.A.ShouldBe(0xC0);
        cpu.Registers.X.ShouldBe(0xC1);
    }
}
