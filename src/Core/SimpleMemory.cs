namespace NesNes.Core;

public class SimpleMemory(byte[] memory) : IMemory
{
    private byte[] _memory = memory;

    /// <inheritdoc/>
    public byte Read8(ushort address)
    {
        return _memory[address];
    }

    /// <inheritdoc/>
    public ushort Read16(ushort address)
    {
        // Read two bytes from the specified address
        // and combine them into a single 16-bit value.
        byte lsb = _memory[address];
        byte msb = _memory[(ushort)(address + 1)];

        return (ushort)((msb << 8) | lsb);
    }

    /// <inheritdoc/>
    public void Write8(ushort address, byte value)
    {
        _memory[address] = value;
    }

    /// <inheritdoc/>
    public void Write16(ushort address, ushort value)
    {
        // Little endian - write the low byte first, then the high byte.
        _memory[address] = (byte)(value & 0x00FF); // Low byte
        _memory[(ushort)(address + 1)] = (byte)(value >> 8); // High byte
    }
}
