namespace NesNes.Tests;

using NesNes.Core;

public class Arithmetic
{
    [Fact]
    public void Inx()
    {
        var initialRegisters = new Registers()
        {
            X = 0xCC,
        };

        byte[] program =
        [
            0xE8,   // INX - X += 1
            0x00    // stop
        ];

        var cpu = new Cpu(initialRegisters, new SimpleMemory(program));
        cpu.Run();
        cpu.Registers.X.ShouldBe(0xCD);
    }

    [Fact]
    public void InxOverflow()
    {
        var initialRegisters = new Registers()
        {
            X = 0xFF,
        };

        byte[] program =
        [
            // Intentionally overflow the X register
            0xE8,   // INX - X += 1
            0xE8,   // INX - X += 1
            0x00    // stop
        ];

        var cpu = new Cpu(initialRegisters, new SimpleMemory(program));
        cpu.Run();
        cpu.Registers.X.ShouldBe(0x01);
    }
}
