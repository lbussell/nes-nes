namespace NesNes.Core;

public enum AddressingMode
{
    /// <summary>
    /// The instruction does not require any addressing mode.
    /// </summary>
    Implicit,

    /// <summary>
    /// Immediate addressing allows the programmer to directly specify an 8 bit
    /// immediately after the instruction.
    /// </summary>
    Immediate,

    /// <summary>
    /// The byte immediately following the instruction is interpreted as an
    /// address referring to the first 256 bytes of memory (0x0000 to 0x00FF).
    /// </summary>
    ZeroPage,

    /// <summary>
    /// Similar to Zero Page - the operand will be read from the byte following
    /// the instruction plus the value in the X register.
    /// </summary>
    ZeroPageX,

    /// <summary>
    /// Similar to Zero Page - the operand will be read from the byte following
    /// the instruction plus the value in the Y register.
    /// </summary>
    /// <remarks>
    /// This mode can only be used with the LDX and STX instructions.
    /// </remarks>
    ZeroPageY,

    /// <summary>
    /// The next two bytes after the instruction are interpreted as a
    /// 16-bit address in memory (little endian).
    /// </summary>
    Absolute,

    /// <summary>
    /// The next two bytes after the instruction are interpreted as a
    /// 16-bit address in memory (little endian), plus the value in the X
    /// register.
    /// </summary>
    AbsoluteX,

    /// <summary>
    /// The next two bytes after the instruction are interpreted as a 16-bit
    /// address in memory (little endian), plus the value in the Y register.
    /// </summary>
    AbsoluteY,

    /// <summary>
    /// The next two bytes are interpreted as a 16-bit address in memory. That
    /// location in memory refers to the least significant byte of another
    /// 16-bit address, which is the target of the instruction.
    /// </summary>
    /// <remarks>
    /// This mode can only be used by the JMP instruction.
    /// </remarks>
    Indirect,

    /// <summary>
    /// Take the next byte and add the X register to it, resulting in a
    /// zero-page address. That address refers to the low byte of the target
    /// address. The high byte is the next byte after that in memory.
    /// </summary>
    IndirectX,

    /// <summary>
    /// Take the next byte and add the Y register to it, resulting in a
    /// zero-page address. That address refers to the low byte of the target
    /// address. The high byte is the next byte after that in memory.
    /// </summary>
    IndirectY,
}
