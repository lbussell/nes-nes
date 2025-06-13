namespace NesNes.Core;

public class Console
{
    public required Cpu Cpu { get; init; }
    public required SimpleMemory Memory { get; init; }

    public void LoadRom(byte[] rom)
    {
    }
}
