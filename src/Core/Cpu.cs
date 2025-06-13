namespace NesNes.Core;

public class Cpu
{
    private Registers _registers;
    private byte[] _program = [];

    public Cpu(Registers registers = new())
    {
        _registers = registers;
    }

    public Registers Registers => _registers;

    /// <summary>
    /// Run a simple program starting at address 0x0000. Does not do any of the
    /// typical 6502 initialization steps. This is mostly for testing.
    /// </summary>
    /// <param name="program">
    /// The program to execute.
    /// </param>
    public void Run(byte[] program)
    {
        _registers.PC = 0;

        // Load the program into memory
        _program = program;

        while (true)
        {
            var result = ExecuteInstruction();
            if (result == InstructionResult.StopExecution)
            {
                break;
            }
        }
    }

    private InstructionResult ExecuteInstruction()
    {
        // Load next instruction
        byte opcode = _program[_registers.PC];
        _registers.PC += 1;

        // Execute instruction
        switch (opcode)
        {
            case 0x00:
                // BRK
                // For now, just stop execution.
                // TODO: implement proper BRK handling
                return InstructionResult.StopExecution;

            case 0xAA:
                // TAX
                // Transfer the value in the accumulator to the X register.
                // Machine cycles: 2
                _registers.X = _registers.A;
                _registers.SetZeroAndNegative(_registers.X);
                break;

            case 0xA9:
                // LDA immediate
                // Load a byte of memory into the accumulator. Sets the zero
                // and negative flags as appropriate.
                byte nextByte = _program[_registers.PC++];
                _registers.A = nextByte;
                _registers.SetZeroAndNegative(_registers.A);
                break;

            case 0xE8:
                // INX - increment X
                // X,Z,N = X+1
                // Cycles: 2
                _registers.X += 1;
                _registers.SetZeroAndNegative(_registers.X);
                break;

            default:
                // Handle unknown opcode
                throw new NotImplementedException(
                    $"Opcode {opcode:X2} is not implemented.");
        }

        return InstructionResult.Ok;
    }

    private enum InstructionResult
    {
        StopExecution,
        Ok,
    }
}
