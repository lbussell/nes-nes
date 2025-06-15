// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class Cpu
{
    private readonly Instruction[] _instructions;
    private readonly IMemory _memory;
    private Registers _registers;

    public Cpu(Registers initialRegisters, IMemory memory)
    {
        _registers = initialRegisters;
        _memory = memory;

        _instructions = InitializeInstructions();
    }

    public Registers Registers => _registers;

    public void Run()
    {
        while (true)
        {
            var instructionResult = Step();

            if (instructionResult == InstructionResult.StopExecution)
            {
                break;
            }
        }
    }

    internal void RunSteps(int steps)
    {
        for (var i = 0; i < steps; i += 1)
        {
            _ = Step();
        }
    }

    internal List<byte> GetImplementedOpcodes()
    {
        var implementedOpcodes = new List<byte>();

        for (byte i = 0; i < _instructions.Length; i += 1)
        {
            if (_instructions[i].HasValue())
            {
                implementedOpcodes.Add(i);
            }
        }

        return implementedOpcodes;
    }

    private InstructionResult Step()
    {
        // Load next instruction
        byte opcode = Fetch8();
        if (opcode == 0x00)
        {
            // BRK (break) instruction, stop execution
            return InstructionResult.StopExecution;
        }

        var instruction = _instructions[opcode];
        if (!instruction.HasValue())
        {
            throw new InvalidOperationException(
                $"Unknown opcode: {opcode:X2} at PC: {_registers.PC - 1:X4}"
            );
        }

        var cycles = instruction.Execute();
        return InstructionResult.Ok;
    }

    #region Instructions

    private Instruction[] InitializeInstructions()
    {
        var opcodes = new Instruction[0x100];

        var adc = UseOperand(Adc);
        opcodes[0x69] = new("ADC", adc, AddressingMode.Immediate, 2);
        opcodes[0x65] = new("ADC", adc, AddressingMode.ZeroPage, 3);
        opcodes[0x75] = new("ADC", adc, AddressingMode.ZeroPageX, 4);
        opcodes[0x6D] = new("ADC", adc, AddressingMode.Absolute, 4);
        opcodes[0x7D] = new("ADC", adc, AddressingMode.AbsoluteX, 4);
        opcodes[0x79] = new("ADC", adc, AddressingMode.AbsoluteY, 4);
        opcodes[0x61] = new("ADC", adc, AddressingMode.IndirectX, 6);
        opcodes[0x71] = new("ADC", adc, AddressingMode.IndirectY, 5);

        opcodes[0xAA] = new("TAX", Implicit(Tax), AddressingMode.Implicit, 2);

        opcodes[0xEA] = new("NOP", Implicit(() => { }), AddressingMode.Implicit, 2);

        var lda = UseOperand(Lda);
        opcodes[0xA9] = new("LDA", lda, AddressingMode.Immediate, 2);
        opcodes[0xA5] = new("LDA", lda, AddressingMode.ZeroPage, 3);
        opcodes[0xB5] = new("LDA", lda, AddressingMode.ZeroPageX, 4);
        opcodes[0xAD] = new("LDA", lda, AddressingMode.Absolute, 4);
        opcodes[0xBD] = new("LDA", lda, AddressingMode.AbsoluteX, 4);
        opcodes[0xB9] = new("LDA", lda, AddressingMode.AbsoluteY, 4);
        opcodes[0xA1] = new("LDA", lda, AddressingMode.IndirectX, 6);
        opcodes[0xB1] = new("LDA", lda, AddressingMode.IndirectY, 5);

        var ldx = UseOperand(Ldx);
        opcodes[0xA2] = new("LDX", ldx, AddressingMode.Immediate, 2);
        opcodes[0xA6] = new("LDX", ldx, AddressingMode.ZeroPage, 3);
        opcodes[0xB6] = new("LDX", ldx, AddressingMode.ZeroPageY, 4);
        opcodes[0xAE] = new("LDX", ldx, AddressingMode.Absolute, 4);
        opcodes[0xBE] = new("LDX", ldx, AddressingMode.AbsoluteY, 4);

        var ldy = UseOperand(Ldy);
        opcodes[0xA0] = new("LDY", ldy, AddressingMode.Immediate, 2);
        opcodes[0xA4] = new("LDY", ldy, AddressingMode.ZeroPage, 3);
        opcodes[0xB4] = new("LDY", ldy, AddressingMode.ZeroPageX, 4);
        opcodes[0xAC] = new("LDY", ldy, AddressingMode.Absolute, 4);
        opcodes[0xBC] = new("LDY", ldy, AddressingMode.AbsoluteX, 4);

        return opcodes;
    }

    /// <summary>
    /// <c>A,Z,C,N = A+M+C;</c>
    /// Add the operand plus the carry flag to the accumulator.
    /// </summary>
    /// <param name="operand"></param>
    private void Adc(byte operand)
    {
        int sum = _registers.A + operand + _registers.Carry;

        _registers.SetZeroAndNegative((byte)sum);
        _registers.SetCarry((ushort)sum);

        // Set the overflow flag if the sign of the result is different from
        // the sign of both the accumulator and the operand.
        // XOR'ing the oprerands and the sum will set the high bit if the signs
        // of the sums differ. Then, we can just check the high bit (0x80).
        // If the high bit is set, then the operation overflowed.
        _registers.SetOverflow(((_registers.A ^ sum) & (operand ^ sum) & 0x80) != 0);

        _registers.A = (byte)(sum & 0xFF); // Store only the lower 8 bits
    }

    /// <summary>
    /// Load the operand into the accumulator (A) register, setting the zero
    /// and negative flags as appropriate.
    /// </summary>
    private void Lda(byte operand)
    {
        _registers.A = operand;
        _registers.SetZeroAndNegative(_registers.A);
    }

    /// <summary>
    /// Load the operand into the X register, setting the zero and negative
    /// flags as appropriate.
    /// </summary>
    private void Ldx(byte operand)
    {
        _registers.X = operand;
        _registers.SetZeroAndNegative(_registers.X);
    }

    /// <summary>
    /// Load the operand into the Y register, setting the zero and negative
    /// flags as appropriate.
    /// </summary>
    private void Ldy(byte operand)
    {
        _registers.Y = operand;
        _registers.SetZeroAndNegative(_registers.Y);
    }

    private void Tax()
    {
        // Transfer the value in the accumulator to the X register.
        _registers.X = _registers.A;
        _registers.SetZeroAndNegative(_registers.X);
    }

    #endregion

    /// <summary>
    /// Wrapper for simpler instructions that handles reading the operand from
    /// memory and executing the instruction with that operand.
    /// </summary>
    private InstructionHandler UseOperand(Action<byte> action) =>
        mode =>
        {
            var addressResult = GetAddress(mode);
            var operand = _memory.Read8(addressResult.Address);
            action(operand);
            return addressResult.ExtraCycles;
        };

    /// <summary>
    /// Wrapper for instructions that do not require an operand.
    /// </summary>
    private static InstructionHandler Implicit(Action action) =>
        _ =>
        {
            action();
            return 0;
        };

    private AddressResult GetAddress(AddressingMode mode)
    {
        return mode switch
        {
            AddressingMode.Immediate => new AddressResult(_registers.PC++, 0),
            AddressingMode.ZeroPage => new AddressResult(Fetch8(), 0),
            AddressingMode.ZeroPageX => new AddressResult((byte)(Fetch8() + _registers.X), 0),
            AddressingMode.ZeroPageY => new AddressResult((byte)(Fetch8() + _registers.Y), 0),
            AddressingMode.Absolute => Absolute(),
            AddressingMode.AbsoluteX => AbsoluteX(),
            AddressingMode.AbsoluteY => AbsoluteY(),
            AddressingMode.Indirect => Indirect(),
            AddressingMode.IndirectX => IndirectX(),
            AddressingMode.IndirectY => IndirectY(),
            AddressingMode.Implicit => throw new InvalidOperationException(
                "Implicit addressing mode does not use an operand."
            ),
            _ => throw new NotImplementedException($"Addressing mode {mode} is not implemented."),
        };

        AddressResult Absolute()
        {
            ushort address = Fetch16();
            return new AddressResult(address, 0);
        }

        AddressResult AbsoluteX()
        {
            ushort address = Fetch16();
            ushort newAddress = (ushort)(address + _registers.X);
            int extraCycles = CalculatePageCrossPenalty(address, newAddress);
            return new AddressResult(newAddress, extraCycles);
        }

        AddressResult AbsoluteY()
        {
            ushort address = Fetch16();
            ushort newAddress = (ushort)(address + _registers.Y);
            int extraCycles = CalculatePageCrossPenalty(address, newAddress);
            return new AddressResult(newAddress, extraCycles);
        }

        AddressResult Indirect()
        {
            ushort targetPointer = Fetch16();
            ushort targetAddress = _memory.Read16(targetPointer);
            return new AddressResult(targetAddress, 0);
        }

        AddressResult IndirectX()
        {
            byte zeroPageAddress = Fetch8();
            byte targetPointer = (byte)(zeroPageAddress + _registers.X);
            ushort targetAddress = Read16ZeroPageWraparound(targetPointer);
            return new AddressResult(targetAddress, 0);
        }

        // Read a 16-bit word from zero page, wrapping around if necessary.
        // The zero page is only 256 bytes, so addresses above 0xFF wrap around.
        // This handles the case where the first byte is at 0xFF and the second
        // byte is at 0x00.
        ushort Read16ZeroPageWraparound(byte address)
        {
            byte lsb = _memory.Read8(address);
            byte msb = _memory.Read8((byte)(address + 1));

            return (ushort)((msb << 8) | lsb);
        }

        AddressResult IndirectY()
        {
            byte zeroPageAddress = Fetch8();
            ushort targetPointer = Read16ZeroPageWraparound(zeroPageAddress);
            ushort targetAddress = (ushort)(targetPointer + _registers.Y);
            return new AddressResult(targetAddress, 0);
        }

        static int CalculatePageCrossPenalty(ushort originalAddress, ushort newAddress)
        {
            if ((originalAddress & 0xFF00) != (newAddress & 0xFF00))
            {
                return 1;
            }

            return 0;
        }
    }

    /// <summary>
    /// Read a byte from memory at the current program counter, incrementing
    /// the program counter as appropriate.
    /// </summary>
    private byte Fetch8()
    {
        var nextByte = _memory.Read8(_registers.PC);
        _registers.PC += 1;
        return nextByte;
    }

    /// <summary>
    /// Read a 16-bit word from memory at the current program counter,
    /// incrementing the program counter as appropriate.
    /// </summary>
    private ushort Fetch16()
    {
        var nextWord = _memory.Read16(_registers.PC);
        _registers.PC += 2;
        return nextWord;
    }

    private readonly record struct AddressResult(ushort Address, int ExtraCycles);

    private enum InstructionResult
    {
        StopExecution,
        Ok,
    }
}
