namespace NesNes.Core;

public class Cpu
{
    private Registers _registers;
    private byte[] _program = [];

    public Cpu()
    {
        _registers = new Registers();
    }

    public Registers Registers => _registers;

    public void Run(byte[] program)
    {
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

            case 0xA9:
                // LDA immediate
                // Loads a byte of memory into the accumulator. Sets the zero
                // and negative flags as appropriate.
                byte nextByte = _program[_registers.PC++];
                _registers.A = nextByte;
                _registers.SetZero(_registers.A);
                _registers.SetNegative(_registers.A);
                break;

            // other instructions go here...

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
