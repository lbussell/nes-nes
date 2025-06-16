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

        for (int i = 0; i < _instructions.Length; i += 1)
        {
            if (_instructions[i].HasValue())
            {
                implementedOpcodes.Add((byte)i);
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

        var and = UseOperand(And);
        opcodes[0x29] = new("AND", and, AddressingMode.Immediate, 2);
        opcodes[0x25] = new("AND", and, AddressingMode.ZeroPage, 3);
        opcodes[0x35] = new("AND", and, AddressingMode.ZeroPageX, 4);
        opcodes[0x2D] = new("AND", and, AddressingMode.Absolute, 4);
        opcodes[0x3D] = new("AND", and, AddressingMode.AbsoluteX, 4);
        opcodes[0x39] = new("AND", and, AddressingMode.AbsoluteY, 4);
        opcodes[0x21] = new("AND", and, AddressingMode.IndirectX, 6);
        opcodes[0x31] = new("AND", and, AddressingMode.IndirectY, 5);

        opcodes[0x0A] = new("ASL", Implicit(AslA), AddressingMode.Implicit, 2);
        opcodes[0x06] = new("ASL", AslMemory, AddressingMode.ZeroPage, 5);
        opcodes[0x16] = new("ASL", AslMemory, AddressingMode.ZeroPageX, 6);
        opcodes[0x0E] = new("ASL", AslMemory, AddressingMode.Absolute, 6);
        opcodes[0x1E] = new("ASL", AslMemory, AddressingMode.AbsoluteX, 7);

        opcodes[0x90] = new("BCC", Bcc, AddressingMode.Relative, 2);
        opcodes[0xB0] = new("BCS", Bcs, AddressingMode.Relative, 2);
        opcodes[0xF0] = new("BEQ", Beq, AddressingMode.Relative, 2);
        opcodes[0x30] = new("BMI", Bmi, AddressingMode.Relative, 2);
        opcodes[0xD0] = new("BNE", Bne, AddressingMode.Relative, 2);
        opcodes[0x10] = new("BPL", Bpl, AddressingMode.Relative, 2);
        opcodes[0x50] = new("BVC", Bvc, AddressingMode.Relative, 2);
        opcodes[0x70] = new("BVS", Bvs, AddressingMode.Relative, 2);

        var bit = UseOperand(Bit);
        opcodes[0x24] = new("BIT", bit, AddressingMode.ZeroPage, 3);
        opcodes[0x2C] = new("BIT", bit, AddressingMode.Absolute, 4);

        opcodes[0x18] = new(
            "CLC",
            Implicit(() => _registers.ClearFlag(Flags.Carry)),
            AddressingMode.Implicit,
            2
        );
        opcodes[0xD8] = new(
            "CLD",
            Implicit(() => _registers.ClearFlag(Flags.DecimalMode)),
            AddressingMode.Implicit,
            2
        );
        opcodes[0x58] = new(
            "CLI",
            Implicit(() => _registers.ClearFlag(Flags.InterruptDisable)),
            AddressingMode.Implicit,
            2
        );
        opcodes[0xB8] = new(
            "CLV",
            Implicit(() => _registers.ClearFlag(Flags.Overflow)),
            AddressingMode.Implicit,
            2
        );

        opcodes[0xC9] = new("CMP", UseOperand(Cmp), AddressingMode.Immediate, 2);
        opcodes[0xC5] = new("CMP", UseOperand(Cmp), AddressingMode.ZeroPage, 3);
        opcodes[0xD5] = new("CMP", UseOperand(Cmp), AddressingMode.ZeroPageX, 4);
        opcodes[0xCD] = new("CMP", UseOperand(Cmp), AddressingMode.Absolute, 4);
        opcodes[0xDD] = new("CMP", UseOperand(Cmp), AddressingMode.AbsoluteX, 4);
        opcodes[0xD9] = new("CMP", UseOperand(Cmp), AddressingMode.AbsoluteY, 4);
        opcodes[0xC1] = new("CMP", UseOperand(Cmp), AddressingMode.IndirectX, 6);
        opcodes[0xD1] = new("CMP", UseOperand(Cmp), AddressingMode.IndirectY, 5);

        opcodes[0xE0] = new("CPX", UseOperand(Cpx), AddressingMode.Immediate, 2);
        opcodes[0xE4] = new("CPX", UseOperand(Cpx), AddressingMode.ZeroPage, 3);
        opcodes[0xEC] = new("CPX", UseOperand(Cpx), AddressingMode.Absolute, 4);

        opcodes[0xC0] = new("CPY", UseOperand(Cpy), AddressingMode.Immediate, 2);
        opcodes[0xC4] = new("CPY", UseOperand(Cpy), AddressingMode.ZeroPage, 3);
        opcodes[0xCC] = new("CPY", UseOperand(Cpy), AddressingMode.Absolute, 4);

        opcodes[0xC6] = new("DEC", UseAddress(Dec), AddressingMode.ZeroPage, 5);
        opcodes[0xD6] = new("DEC", UseAddress(Dec), AddressingMode.ZeroPageX, 6);
        opcodes[0xCE] = new("DEC", UseAddress(Dec), AddressingMode.Absolute, 6);
        opcodes[0xDE] = new("DEC", UseAddress(Dec), AddressingMode.AbsoluteX, 7);
        opcodes[0xCA] = new("DEX", Implicit(() => Dex(ref _registers.X)), AddressingMode.Implicit, 2);
        opcodes[0x88] = new("DEY", Implicit(() => Dex(ref _registers.Y)), AddressingMode.Implicit, 2);

        opcodes[0x49] = new("EOR", UseOperand(Eor), AddressingMode.Immediate, 2);
        opcodes[0x45] = new("EOR", UseOperand(Eor), AddressingMode.ZeroPage, 3);
        opcodes[0x55] = new("EOR", UseOperand(Eor), AddressingMode.ZeroPageX, 4);
        opcodes[0x4D] = new("EOR", UseOperand(Eor), AddressingMode.Absolute, 4);
        opcodes[0x5D] = new("EOR", UseOperand(Eor), AddressingMode.AbsoluteX, 4);
        opcodes[0x59] = new("EOR", UseOperand(Eor), AddressingMode.AbsoluteY, 4);
        opcodes[0x41] = new("EOR", UseOperand(Eor), AddressingMode.IndirectX, 6);
        opcodes[0x51] = new("EOR", UseOperand(Eor), AddressingMode.IndirectY, 5);

        opcodes[0xE6] = new("INC", UseAddress(Inc), AddressingMode.ZeroPage, 5);
        opcodes[0xF6] = new("INC", UseAddress(Inc), AddressingMode.ZeroPageX, 6);
        opcodes[0xEE] = new("INC", UseAddress(Inc), AddressingMode.Absolute, 6);
        opcodes[0xFE] = new("INC", UseAddress(Inc), AddressingMode.AbsoluteX, 7);

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

        var sbc = UseOperand(Sbc);
        opcodes[0xE9] = new("SBC", sbc, AddressingMode.Immediate, 2);
        opcodes[0xE5] = new("SBC", sbc, AddressingMode.ZeroPage, 3);
        opcodes[0xF5] = new("SBC", sbc, AddressingMode.ZeroPageX, 4);
        opcodes[0xED] = new("SBC", sbc, AddressingMode.Absolute, 4);
        opcodes[0xFD] = new("SBC", sbc, AddressingMode.AbsoluteX, 4);
        opcodes[0xF9] = new("SBC", sbc, AddressingMode.AbsoluteY, 4);
        opcodes[0xE1] = new("SBC", sbc, AddressingMode.IndirectX, 6);
        opcodes[0xF1] = new("SBC", sbc, AddressingMode.IndirectY, 5);

        return opcodes;
    }

    /// <summary>
    /// <c>A,Z,C,N = A+M+C;</c>
    /// Add the operand plus the carry flag to the accumulator.
    /// </summary>
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
        _registers.SetFlag(Flags.Overflow, ((_registers.A ^ sum) & (operand ^ sum) & 0x80) != 0);

        _registers.A = (byte)(sum & 0xFF); // Store only the lower 8 bits
    }

    /// <summary>
    /// Logical AND between the accumulator and operand.
    /// </summary>
    private void And(byte operand)
    {
        _registers.A &= operand;
        _registers.SetZeroAndNegative(_registers.A);
    }

    /// <summary>
    /// Arithmetic shift left on accumulator.
    /// </summary>
    private void AslA()
    {
        int result = _registers.A << 1;
        _registers.A = (byte)result;
        _registers.SetCarry(result);
        _registers.SetZeroAndNegative(_registers.A);
    }

    /// <summary>
    /// Arithmetic shift left on a memory location.
    /// </summary>
    private int AslMemory(AddressingMode mode)
    {
        var addressResult = GetAddress(mode);
        byte operand = _memory[addressResult.Address];

        int result = operand << 1;
        _registers.SetCarry(result);
        _registers.SetZeroAndNegative((byte)result);
        _memory[addressResult.Address] = (byte)result;

        return addressResult.ExtraCycles;
    }

    /// <summary>
    /// Branch if the carry flag is clear.
    /// </summary>
    private int Bcc(AddressingMode mode) => BranchIf(Flags.Carry, false, mode);

    /// <summary>
    /// Branch if the carry flag is set.
    /// </summary>
    private int Bcs(AddressingMode mode) => BranchIf(Flags.Carry, true, mode);

    /// <summary>
    /// Branch if the zero flag is set (equal).
    /// </summary>
    private int Beq(AddressingMode mode) => BranchIf(Flags.Zero, true, mode);

    /// <summary>
    /// Branch if the negative flag is set (minus).
    /// </summary>
    private int Bmi(AddressingMode mode) => BranchIf(Flags.Negative, true, mode);

    /// <summary>
    /// Branch if the zero flag is clear (not equal).
    /// </summary>
    private int Bne(AddressingMode mode) => BranchIf(Flags.Zero, false, mode);

    /// <summary>
    /// Branch if the negative flag is clear (plus).
    /// </summary>
    private int Bpl(AddressingMode mode) => BranchIf(Flags.Negative, false, mode);

    // BRK

    /// <summary>
    /// Branch if the overflow flag is clear.
    /// </summary>
    private int Bvc(AddressingMode mode) => BranchIf(Flags.Overflow, false, mode);

    /// <summary>
    /// Branch if the overflow flag is set.
    /// </summary>
    private int Bvs(AddressingMode mode) => BranchIf(Flags.Overflow, true, mode);

    /// <summary>
    /// Branch if the given flag matches the expected value. Takes one extra
    /// cycle if the branch is taken, and 1 additional cycle on top of that if a
    /// page boundary is crossed.
    /// </summary>
    /// <returns>Additional CPU cycles taken.</returns>
    private int BranchIf(Flags flag, bool expectedValue, AddressingMode mode)
    {
        // Always fetch the address, since we need to advance the program counter.
        var addressResult = GetAddress(mode);

        // If the flag matches the expected value, branch to the target address.
        if (_registers.P.HasFlag(flag) == expectedValue)
        {
            // If the branch crosses a page boundary, add an extra cycle.
            var extraCycles = 1 + CalculatePageCrossPenalty(_registers.PC, addressResult.Address);
            _registers.PC = addressResult.Address;
            return extraCycles;
        }

        // Since the branch was not taken, we incurred 0 extra cycles.
        return 0;
    }

    /// <summary>
    /// This instructions is used to test if one or more bits are set in a
    /// target memory location. The mask pattern in A is ANDed with the value in
    /// memory to set or clear the zero flag, but the result is not kept.
    /// Bits 7 and 6 of the value from memory are copied into the N and V flags.
    /// </summary>
    private void Bit(byte operand)
    {
        // Perform a bitwise AND between the accumulator and the operand.
        byte result = (byte)(_registers.A & operand);

        // Set the zero flag if the result is zero.
        _registers.SetZero(result);

        // Set the negative flag to the 7th bit of the operand.
        _registers.SetNegative(operand);

        // Set the overflow flag to the 6th bit of the operand.
        _registers.SetFlag(Flags.Overflow, (operand & 0b_0100_0000) != 0);
    }

    /// <summary>
    /// This instruction compares the contents of the accumulator with another
    /// memory held value and sets the zero and carry flags as appropriate.
    /// </summary>
    private void Cmp(byte operand)
    {
        byte result = (byte)(_registers.A - operand);
        _registers.SetZeroAndNegative(result);
        _registers.SetFlag(Flags.Carry, _registers.A >= operand);
    }

    /// <summary>
    /// This instruction compares the contents of the X register with another
    /// memory held value and sets the zero and carry flags as appropriate.
    /// </summary>
    private void Cpx(byte operand)
    {
        byte result = (byte)(_registers.X - operand);
        _registers.SetZeroAndNegative(result);
        _registers.SetFlag(Flags.Carry, _registers.X >= operand);
    }

    /// <summary>
    /// This instruction compares the contents of the Y register with another
    /// memory held value and sets the zero and carry flags as appropriate.
    /// </summary>
    private void Cpy(byte operand)
    {
        byte result = (byte)(_registers.Y - operand);
        _registers.SetZeroAndNegative(result);
        _registers.SetFlag(Flags.Carry, _registers.Y >= operand);
    }

    /// <summary>
    /// Subtracts one from the value held at a specified memory location setting
    /// the zero and negative flags as appropriate.
    /// </summary>
    private void Dec(ushort address)
    {
        var value = _memory[address];
        var result = (byte)(value - 1);
        _memory[address] = result;
        _registers.SetZeroAndNegative(result);
    }

    /// <summary>
    /// Subtracts one from the target register, setting the zero and negative
    /// flags as appropriate.
    /// </summary>
    private void Dex(ref byte target)
    {
        target -= 1;
        _registers.SetZeroAndNegative(target);
    }

    /// <summary>
    /// Exclusive OR between the accumulator and the operand.
    /// </summary>
    private void Eor(byte operand)
    {
        _registers.A ^= operand;
        _registers.SetZeroAndNegative(_registers.A);
    }

    /// <summary>
    /// Increment the value at a specified memory location, setting the zero and
    /// negative flags as appropriate.
    /// </summary>
    private void Inc(ushort address)
    {
        byte value = _memory[address];
        byte result = (byte)(value + 1);
        _memory[address] = result;
        _registers.SetZeroAndNegative(result);
    }

    /// <summary>
    /// A,Z,C,N = A-M-(1-C)
    /// </summary>
    private void Sbc(byte operand)
    {
        // Since ADC is correctly implemented, we can use it to implement SBC.
        Adc((byte)~operand);
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

    private InstructionHandler UseAddress(Action<ushort> action) =>
        mode =>
        {
            var addressResult = GetAddress(mode);
            action(addressResult.Address);
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
            AddressingMode.Relative => Relative(),
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

        AddressResult Relative()
        {
            byte offset = Fetch8();
            ushort targetAddress = (ushort)(_registers.PC + (sbyte)offset);
            return new AddressResult(targetAddress, 0);
        }

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
            int extraCycles = CalculatePageCrossPenalty(zeroPageAddress, targetAddress);
            return new AddressResult(targetAddress, extraCycles);
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

    private static int CalculatePageCrossPenalty(ushort originalAddress, ushort newAddress)
    {
        if ((originalAddress & 0xFF00) != (newAddress & 0xFF00))
        {
            return 1;
        }

        return 0;
    }

    private readonly record struct AddressResult(ushort Address, int ExtraCycles);

    private enum InstructionResult
    {
        StopExecution,
        Ok,
    }
}
