// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class SimpleMemory(int size) : IMemory
{
    private readonly byte[] _memory = new byte[size];

    /// <inheritdoc/>
    public byte Read8(ushort address) => _memory[address];

    /// <inheritdoc/>
    public void Write8(ushort address, byte value) => _memory[address] = value;
}
