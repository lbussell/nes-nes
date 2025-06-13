using NesNes.Core;

namespace NesNes.Tests;

/// <summary>
/// A memory implementation backed by a Dictionary for efficient testing.
/// Only stores memory that has been explicitly written to or initialized.
/// Throws an exception when reading from uninitialized memory locations.
/// </summary>
public class TestMemory : IMemory
{
    private readonly Dictionary<ushort, byte> _memory = [];

    /// <summary>
    /// Initializes memory with the provided address-value pairs.
    /// </summary>
    /// <param name="initialMemory">
    /// Array of [address, value] pairs to initialize
    /// </param>
    public void Initialize(int[][] initialMemory)
    {
        _memory.Clear();
        foreach (var entry in initialMemory)
        {
            ushort address = (ushort)entry[0];
            byte value = (byte)entry[1];
            _memory[address] = value;
        }
    }

    /// <summary>
    /// Gets all memory locations that have been written to or initialized.
    /// Returns as [address, value] pairs sorted by address.
    /// </summary>
    public int[][] GetMemorySnapshot()
    {
        return _memory
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new int[] { kvp.Key, kvp.Value })
            .ToArray();
    }

    public byte Read8(ushort address)
    {
        if (!_memory.TryGetValue(address, out byte value))
        {
            throw new InvalidOperationException(
                $"Attempted to read from uninitialized memory address: 0x{address:X4}");
        }
        return value;
    }

    public void Write8(ushort address, byte value)
    {
        _memory[address] = value;
    }
}
