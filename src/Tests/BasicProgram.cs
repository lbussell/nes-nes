namespace NesNes.Tests;

using NesNes.Core;

public class BasicProgram
{
    [Fact]
    public void RunsBasicProgram()
    {
        byte[] program =
        [
            0xA9, // LDA $C0
            0xC0,
            0xAA, // TAX
            0xE8, // INX
            0x00  // BRK
        ];

        var cpu = new Cpu();
        cpu.Run(program);

        // Pass the test if the program did not crash.
        // TODO: validate the final state of the CPU
        Assert.True(true);
    }
}
