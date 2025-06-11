namespace NesNes.Core;

/// <summary>
/// Represents all of the registers in the NES CPU.
/// </summary>
public struct Registers
{
    /// <summary>
    /// Program counter - holds the holds the address of the next instruction
    /// to be executed.
    /// </summary>
    public ushort PC;

    /// <summary>
    /// Stack pointer - The stack is located in [0x0100, 0x01FF]. The stack
    /// pointer holds the address of the top of the stack. The stack grows from
    /// top to bottom. When a byte is pushed to the stack, this address is
    /// decremented. Likewise, when a byte is retrieved from the stack, this
    /// register is incremented.
    /// </summary>
    public ushort SP;

    /// <summary>
    /// Accumulator - stores the results of arithmetic, logic, and memory
    /// access operations. It is also used as an input parameter for some
    /// operations.
    /// </summary>
    public byte A;

    /// <summary>
    /// General purpose register similar to Y
    /// </summary>
    public byte X;

    /// <summary>
    /// General purpose register similar to X
    /// </summary>
    public byte Y;

    /// <summary>
    /// Processor status - represents 7 status flags that can be set or unset
    /// depending on the result of the last executed instruction.
    /// </summary>
    public byte P;
}
