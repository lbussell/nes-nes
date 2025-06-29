// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public delegate void CpuCallback(ushort PC, Registers registers);

public class Cpu
{
    private readonly Instruction[] _instructions;
    private readonly IMemory _memory;
    private Registers _registers;
    private readonly CpuCallback? _onInstructionCompleted;
    private int _totalCyclesElapsed = 0;

    public Cpu(Registers registers, IMemory memory, CpuCallback? onInstructionCompleted = null)
    {
        _registers = registers;
        _memory = memory;
        _onInstructionCompleted = onInstructionCompleted;

        _instructions = InitializeInstructions();
    }

    public Registers Registers
    {
        get => _registers;
        // Internal set only for testing purposes.
        internal set => _registers = value;
    }

    /// <summary>
    /// Sets registers and flags to their initial states. The reset vector is
    /// read from memory in order to reset the program counter to the beginning
    /// of the program. Call this after a new cartridge is loaded or when the
    /// console is powered on.
    /// </summary>
    public void Reset()
    {
        _registers = Registers.Initial;
        _registers.PC = _memory.Read16(MemoryRegions.ResetVector);
    }

    /// <summary>
    /// Executes a single CPU instruction.
    /// </summary>
    /// <returns>
    /// The number of CPU cycles elapsed while executing the instruction.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// If the CPU tries to execute an illegal opcode.
    /// </exception>
    public int Step()
    {
        if (_nmiPending)
        {
            return NonMaskableInterrupt();
        }

        var oldPC = _registers.PC;

        // Load next instruction
        byte opcode = Fetch8();

        // Execute instruction
        var instruction = _instructions[opcode];
        if (!instruction.HasValue())
        {
            throw new InvalidOperationException(
                $"Unknown opcode: {opcode:X2} at PC: {_registers.PC - 1:X4}"
            );
            return 5;
        }

        // Run callback function if it was provided
        if (_onInstructionCompleted is not null)
        {
            _onInstructionCompleted(oldPC, _registers);
        }

        // Return the number of cycles the instruction took to execute
        var cyclesElapsed = instruction.Execute();
        _totalCyclesElapsed += cyclesElapsed;
        return cyclesElapsed;
    }

    /// <summary>
    /// Gets the list of all opcodes supported by the CPU.
    /// </summary>
    /// <returns>
    /// List of bytes where each byte represents a supported opcode.
    /// </returns>
    public List<byte> GetSupportedOpcodes()
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

    private bool _nmiPending = false;

    public void QueueNonMaskableInterrupt()
    {
        _nmiPending = true;
    }

    private int NonMaskableInterrupt()
    {
        _nmiPending = false;

        // Store PC and Status on the stack
        PushStack(_registers.PC);
        PushStack((byte)_registers.P);

        // Disable interrupts
        _registers.SetFlag(Flags.InterruptDisable);

        // Load PC with the address in the NMI vector
        _registers.PC = _memory.Read16(MemoryRegions.NmiVector);

        // All of that takes 7 cycles!
        return 7;
    }

    #region Instructions

    private Instruction[] InitializeInstructions()
    {
        var opcodes = new Instruction[0x100];
        // csharpier-ignore-start

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

        opcodes[0x18] = new("CLC", Implicit(() => _registers.ClearFlag(Flags.Carry)), AddressingMode.Implicit, 2);
        opcodes[0xD8] = new("CLD", Implicit(() => _registers.ClearFlag(Flags.DecimalMode)), AddressingMode.Implicit, 2);
        opcodes[0x58] = new("CLI", Implicit(() => _registers.ClearFlag(Flags.InterruptDisable)), AddressingMode.Implicit, 2);
        opcodes[0xB8] = new("CLV", Implicit(() => _registers.ClearFlag(Flags.Overflow)), AddressingMode.Implicit, 2);

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
        opcodes[0xE8] = new("INX", Implicit(() => Increment(ref _registers.X)), AddressingMode.Implicit, 2);
        opcodes[0xC8] = new("INY", Implicit(() => Increment(ref _registers.Y)), AddressingMode.Implicit, 2);

        opcodes[0x4C] = new("JMP", UseAddress(Jmp), AddressingMode.Absolute, 3);
        opcodes[0x6C] = new("JMP", UseAddress(Jmp), AddressingMode.Indirect, 5);
        opcodes[0x20] = new("JSR", UseAddress(Jsr), AddressingMode.Absolute, 6);

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

        var lsr = UseAddress(LsrMemory);
        opcodes[0x4A] = new("LSR", Implicit(LsrA), AddressingMode.Implicit, 2);
        opcodes[0x46] = new("LSR", lsr, AddressingMode.ZeroPage, 5);
        opcodes[0x56] = new("LSR", lsr, AddressingMode.ZeroPageX, 6);
        opcodes[0x4E] = new("LSR", lsr, AddressingMode.Absolute, 6);
        opcodes[0x5E] = new("LSR", lsr, AddressingMode.AbsoluteX, 7);

        opcodes[0xEA] = new("NOP", Implicit(() => { }), AddressingMode.Implicit, 2);

        var orA = UseOperand(OrA);
        opcodes[0x09] = new("ORA", orA, AddressingMode.Immediate, 2);
        opcodes[0x05] = new("ORA", orA, AddressingMode.ZeroPage, 3);
        opcodes[0x15] = new("ORA", orA, AddressingMode.ZeroPageX, 4);
        opcodes[0x0D] = new("ORA", orA, AddressingMode.Absolute, 4);
        opcodes[0x1D] = new("ORA", orA, AddressingMode.AbsoluteX, 4);
        opcodes[0x19] = new("ORA", orA, AddressingMode.AbsoluteY, 4);
        opcodes[0x01] = new("ORA", orA, AddressingMode.IndirectX, 6);
        opcodes[0x11] = new("ORA", orA, AddressingMode.IndirectY, 5);

        opcodes[0x48] = new("PHA", Implicit(PushA), AddressingMode.Implicit, 3);
        opcodes[0x08] = new("PHP", Implicit(PushP), AddressingMode.Implicit, 3);
        opcodes[0x68] = new("PLA", Implicit(PullA), AddressingMode.Implicit, 4);
        opcodes[0x28] = new("PLP", Implicit(PullP), AddressingMode.Implicit, 4);

        var rol = UseAddress(RotateLeftMemory);
        opcodes[0x2A] = new("ROL", Implicit(RotateLeftA), AddressingMode.Implicit, 2);
        opcodes[0x26] = new("ROL", rol, AddressingMode.ZeroPage, 5);
        opcodes[0x36] = new("ROL", rol, AddressingMode.ZeroPageX, 6);
        opcodes[0x2E] = new("ROL", rol, AddressingMode.Absolute, 6);
        opcodes[0x3E] = new("ROL", rol, AddressingMode.AbsoluteX, 7);

        var ror = UseAddress(RotateRightMemory);
        opcodes[0x6A] = new("ROR", Implicit(RotateRightA), AddressingMode.Implicit, 2);
        opcodes[0x66] = new("ROR", ror, AddressingMode.ZeroPage, 5);
        opcodes[0x76] = new("ROR", ror, AddressingMode.ZeroPageX, 6);
        opcodes[0x6E] = new("ROR", ror, AddressingMode.Absolute, 6);
        opcodes[0x7E] = new("ROR", ror, AddressingMode.AbsoluteX, 7);

        opcodes[0x40] = new("RTI", Implicit(Rti), AddressingMode.Implicit, 6);
        opcodes[0x60] = new("RTS", Implicit(Rts), AddressingMode.Implicit, 6);

        var sbc = UseOperand(Sbc);
        opcodes[0xE9] = new("SBC", sbc, AddressingMode.Immediate, 2);
        opcodes[0xE5] = new("SBC", sbc, AddressingMode.ZeroPage, 3);
        opcodes[0xF5] = new("SBC", sbc, AddressingMode.ZeroPageX, 4);
        opcodes[0xED] = new("SBC", sbc, AddressingMode.Absolute, 4);
        opcodes[0xFD] = new("SBC", sbc, AddressingMode.AbsoluteX, 4);
        opcodes[0xF9] = new("SBC", sbc, AddressingMode.AbsoluteY, 4);
        opcodes[0xE1] = new("SBC", sbc, AddressingMode.IndirectX, 6);
        opcodes[0xF1] = new("SBC", sbc, AddressingMode.IndirectY, 5);

        opcodes[0x38] = new("SEC", Implicit(() => _registers.SetFlag(Flags.Carry)), AddressingMode.Implicit, 2);
        opcodes[0xF8] = new("SED", Implicit(() => _registers.SetFlag(Flags.DecimalMode)), AddressingMode.Implicit, 2);
        opcodes[0x78] = new("SEI", Implicit(() => _registers.SetFlag(Flags.InterruptDisable)), AddressingMode.Implicit, 2);

        var sta = UseAddress(address => _memory[address] = _registers.A);
        opcodes[0x85] = new("STA", sta, AddressingMode.ZeroPage, 3);
        opcodes[0x95] = new("STA", sta, AddressingMode.ZeroPageX, 4);
        opcodes[0x8D] = new("STA", sta, AddressingMode.Absolute, 4);
        opcodes[0x9D] = new("STA", sta, AddressingMode.AbsoluteX, 5);
        opcodes[0x99] = new("STA", sta, AddressingMode.AbsoluteY, 5);
        opcodes[0x81] = new("STA", sta, AddressingMode.IndirectX, 6);
        opcodes[0x91] = new("STA", sta, AddressingMode.IndirectY, 6);

        var stx = UseAddress(address => _memory[address] = _registers.X);
        opcodes[0x86] = new("STX", stx, AddressingMode.ZeroPage, 3);
        opcodes[0x96] = new("STX", stx, AddressingMode.ZeroPageY, 4);
        opcodes[0x8E] = new("STX", stx, AddressingMode.Absolute, 4);

        var sty = UseAddress(address => _memory[address] = _registers.Y);
        opcodes[0x84] = new("STY", sty, AddressingMode.ZeroPage, 3);
        opcodes[0x94] = new("STY", sty, AddressingMode.ZeroPageX, 4);
        opcodes[0x8C] = new("STY", sty, AddressingMode.Absolute, 4);

        opcodes[0xAA] = new("TAX", Implicit(Tax), AddressingMode.Implicit, 2);
        opcodes[0xA8] = new("TAY", Implicit(Tay), AddressingMode.Implicit, 2);
        opcodes[0xBA] = new("TSX", Implicit(Tsx), AddressingMode.Implicit, 2);
        opcodes[0x8A] = new("TXA", Implicit(Txa), AddressingMode.Implicit, 2);
        opcodes[0x9A] = new("TXS", Implicit(Txs), AddressingMode.Implicit, 2);
        opcodes[0x98] = new("TYA", Implicit(Tya), AddressingMode.Implicit, 2);

        // csharpier-ignore-end

        return opcodes;
    }

    /// <summary>
    /// Pushes a single 8-bit value onto the stack.
    /// </summary>
    private void PushStack(byte value)
    {
        _memory[(ushort)(MemoryRegions.Stack + _registers.SP)] = value;
        _registers.SP -= 1;
    }

    /// <summary>
    /// Pulls a single 8-bit value from the stack. The stack pointer is
    /// incremented after pulling the value.
    /// </summary>
    private byte PullStack()
    {
        _registers.SP += 1;
        var result = _memory[(ushort)(MemoryRegions.Stack + _registers.SP)];
        return result;
    }

    /// <summary>
    /// Pulls a 16-bit value from the stack.
    /// </summary>
    /// <returns></returns>
    private ushort PullStack16()
    {
        byte low = PullStack();
        byte high = PullStack();
        return (ushort)((high << 8) | low);
    }

    /// <summary>
    /// Pushes a 16-bit value onto the stack.
    /// </summary>
    private void PushStack(ushort value)
    {
        byte low = (byte)(value & 0x00FF);
        byte high = (byte)(value >> 8);

        PushStack(high);
        PushStack(low);
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
    /// Increment the target register, setting the zero and negative flags as
    /// appropriate.
    /// </summary>
    private void Increment(ref byte target)
    {
        target += 1;
        _registers.SetZeroAndNegative(target);
    }

    /// <summary>
    /// Set the program counter to the address specified by the operand.
    /// </summary>
    private void Jmp(ushort address)
    {
        // Set the program counter to the target address.
        _registers.PC = address;
    }

    /// <summary>
    /// The JSR instruction pushes the address (minus one) of the return point
    /// on to the stack and then sets the program counter to the target memory
    /// address.
    /// </summary>
    private void Jsr(ushort address)
    {
        // Push the return address (minus one) onto the stack.
        PushStack((ushort)(_registers.PC - 1));

        // Set the program counter to the target address.
        _registers.PC = address;
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

    /// <summary>
    /// Each of the bits in A is shifted one place to the right. The bit that
    /// was in bit 0 is shifted into the carry flag. Bit 7 is set to zero.
    /// Zero and negative flags are set as appropriate.
    /// </summary>
    private void LsrA()
    {
        _registers.SetFlag(Flags.Carry, (_registers.A & 0x01) != 0);
        _registers.A >>= 1;
        _registers.SetZeroAndNegative(_registers.A);
    }

    /// <summary>
    /// Each of the bits in memory is shifted one place to the right. The bit
    /// that was in bit 0 is shifted into the carry flag. Bit 7 is set to zero.
    /// Zero and negative flags are set as appropriate.
    /// </summary>
    private void LsrMemory(ushort address)
    {
        byte value = _memory[address];
        _registers.SetFlag(Flags.Carry, (value & 0x01) != 0);
        byte result = (byte)(value >> 1);
        _memory[address] = result;
        _registers.SetZeroAndNegative(result);
        // TODO: Maybe ignore page cross penalty?
    }

    /// <summary>
    /// An inclusive OR is performed, bit by bit, on the accumulator contents
    /// using the contents of a byte of memory.
    /// </summary>
    private void OrA(byte operand)
    {
        _registers.A |= operand;
        _registers.SetZeroAndNegative(_registers.A);
    }

    /// <summary>
    /// Push the processor status flags onto the stack. This is only one of two
    /// instructions which set the B flag.
    /// </summary>
    private void PushA()
    {
        PushStack(_registers.A);
    }

    /// <summary>
    /// Push the processor status flags onto the stack. This is only one of two
    /// instructions which set the B flag.
    /// </summary>
    private void PushP()
    {
        Flags status = _registers.P;
        status |= Flags.B;
        PushStack((byte)status);
    }

    /// <summary>
    /// Pull an 8-bit value from the stack into the accumulator (A), setting
    /// the zero and negative flags as appropriate.
    /// </summary>
    private void PullA()
    {
        _registers.A = PullStack();
        _registers.SetZeroAndNegative(_registers.A);
    }

    /// <summary>
    /// Pull an 8-bit value from the stack and overwrites all processor flags
    /// with the value, except for the B flag and the unused flag which are set
    /// according to special rules.
    /// </summary>
    private void PullP()
    {
        _registers.P = (Flags)PullStack();
        _registers.SetFlag(Flags.Unused);
        _registers.SetFlag(Flags.B, false);
    }

    /// <summary>
    /// Rotate accumulator left one bit.
    /// </summary>
    private void RotateLeftA()
    {
        _registers.A = RotateLeft(_registers.A);
    }

    /// <summary>
    /// Rotate byte in memory left one bit.
    /// </summary>
    private void RotateLeftMemory(ushort address)
    {
        _memory[address] = RotateLeft(_memory[address]);
    }

    /// <summary>
    /// Move each of the bits of <c>value</c> one place to the left. Bit 0 is
    /// filled with the current value of the carry flag whilst the old bit 7
    /// becomes the new carry flag value. Set zero and negative flags as
    /// appropriate.
    /// </summary>
    /// <returns>The new value after the rotation.</returns>
    private byte RotateLeft(byte value)
    {
        byte carryValue = _registers.Carry;
        bool oldBit7 = (value & 0x80) != 0;

        value = (byte)((value << 1) | carryValue);

        _registers.SetFlag(Flags.Carry, oldBit7);
        _registers.SetZeroAndNegative(value);

        return value;
    }

    /// <summary>
    /// Rotate accumulator right one bit.
    /// </summary>
    private void RotateRightA()
    {
        _registers.A = RotateRight(_registers.A);
    }

    /// <summary>
    /// Rotate byte in memory right one bit.
    /// </summary>
    private void RotateRightMemory(ushort address)
    {
        _memory[address] = RotateRight(_memory[address]);
    }

    /// <summary>
    /// Move each of the bits in either A or M one place to the right. Bit 7 is
    /// filled with the current value of the carry flag whilst the old bit 0
    /// becomes the new carry flag value.
    /// </summary>
    /// <returns>The new value after the rotation.</returns>
    private byte RotateRight(byte value)
    {
        // Get the current carry flag value.
        byte carryValue = _registers.Carry;

        // Check if the old bit 0 is set.
        bool oldBit0 = (value & 0x01) != 0;

        // Shift the bits to the right and set the new bit 7 to the old carry flag.
        value = (byte)((value >> 1) | (carryValue << 7));

        // Set the carry flag to the old bit 0.
        _registers.SetFlag(Flags.Carry, oldBit0);
        _registers.SetZeroAndNegative(value);

        return value;
    }

    /// <summary>
    /// The RTI instruction is used at the end of an interrupt processing
    /// routine. It pulls the processor flags from the stack followed by the
    /// program counter.
    /// </summary>
    private void Rti()
    {
        PullP();
        _registers.PC = PullStack16();
    }

    /// <summary>
    /// Return from subroutine - RTS is used at the end of a subroutine to
    /// return to the calling routine. It pulls the program counter (minus one)
    /// from the stack.
    /// </summary>
    private void Rts()
    {
        _registers.PC = PullStack16();
        _registers.PC += 1;
    }

    /// <summary>
    /// Transfer the value in the accumulator to the X register.
    /// </summary>
    private void Tax()
    {
        _registers.X = _registers.A;
        _registers.SetZeroAndNegative(_registers.X);
    }

    /// <summary>
    /// Copies the current contents of the accumulator into the Y register and
    /// sets the zero and negative flags as appropriate.
    /// </summary>
    private void Tay()
    {
        _registers.Y = _registers.A;
        _registers.SetZeroAndNegative(_registers.Y);
    }

    /// <summary>
    /// Copies the current contents of the stack register into the X register
    /// and sets the zero and negative flags as appropriate.
    /// </summary>
    private void Tsx()
    {
        _registers.X = _registers.SP;
        _registers.SetZeroAndNegative(_registers.X);
    }

    /// <summary>
    /// Copies the current contents of the X register into the accumulator and
    /// sets the zero and negative flags as appropriate.
    /// </summary>
    private void Txa()
    {
        _registers.A = _registers.X;
        _registers.SetZeroAndNegative(_registers.A);
    }

    /// <summary>
    /// Copies the current contents of the accumulator into the Y register and
    /// sets the zero and negative flags as appropriate.
    /// </summary>
    private void Txs()
    {
        _registers.SP = _registers.X;
    }

    /// <summary>
    /// Copies the current contents of the X register into the accumulator and
    /// sets the zero and negative flags as appropriate.
    /// </summary>
    private void Tya()
    {
        _registers.A = _registers.Y;
        _registers.SetZeroAndNegative(_registers.A);
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
    /// Wrapper for instructions that simply operate on an address.
    /// </summary>
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

    /// <summary>
    /// Get the address to be used by an opcode based on the addressing mode.
    /// </summary>
    /// <param name="mode">
    /// The addressing mode that will be used to fetch the address.
    /// </param>
    /// <returns>Data structure representing the address that was calculated /
    /// fetched and any extra CPU cycles that were incurred.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="AddressingMode.Implicit"/> is used, because it
    /// never has an associated address to fetch or calculate.
    /// </exception>
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
            ushort lsbAddress = Fetch16();

            byte lsb = _memory[lsbAddress];
            // Handle wrap-around for the second byte
            ushort msbAddress = (ushort)((lsbAddress & 0xFF00) + (byte)(lsbAddress + 1));
            byte msb = _memory[msbAddress];
            ushort targetAddress = (ushort)((msb << 8) | lsb);

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

    /// <summary>
    /// Determines the number of extra cycles incurred when crossing pages from
    /// <c>originalAddress</c> to <c>newAddress</c>.
    /// </summary>
    /// <returns>
    /// The number of extra cycles incurred, if any. Returns zero if a page
    /// boundary was not crossed.
    /// </returns>
    private static int CalculatePageCrossPenalty(ushort originalAddress, ushort newAddress)
    {
        if ((originalAddress & 0xFF00) != (newAddress & 0xFF00))
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// The result of calculating an address to be used by a CPU opcode. Some
    /// addressing modes incur extra CPU cycles to fetch or calculate the
    /// address, which is represented by <c>ExtraCycles</c>.
    /// </summary>
    private readonly record struct AddressResult(ushort Address, int ExtraCycles);
}
