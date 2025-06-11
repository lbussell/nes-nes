namespace NesNes.Core;

public class Cpu
{
    private Registers _registers;
    private byte[] _program = [];

    public Cpu()
    {
        _registers = new Registers();
    }

    public void Run(byte[] program)
    {
        // Load the program into memory
        _program = program;

        while (true)
        {
            ExecuteInstruction();
        }
    }

    private void ExecuteInstruction()
    {
        // Load next instruction
        byte opcode = _program[_registers.PC];
        _registers.PC += 1;

        // Execute instruction
        switch (opcode)
        {
            case 0xA9: // LDA

            // other instructions go here...

            default:
                // Handle unknown opcode
                throw new NotImplementedException(
                    $"Opcode {opcode:X2} is not implemented.");
        }
    }
}
