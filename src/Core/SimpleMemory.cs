// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class SimpleMemory(byte[] memory) : IMemory
{
    private readonly byte[] _memory = memory;

    /// <inheritdoc/>
    public byte Read8(ushort address) =>
        _memory[address];

    /// <inheritdoc/>
    public void Write8(ushort address, byte value) =>
        _memory[address] = value;
}
