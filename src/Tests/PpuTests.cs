// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;

namespace NesNes.Tests;

public class PpuTests
{
    [Fact]
    public void ReadMemory()
    {
        const ushort TargetAddress = 0x2345;
        const byte ExpectedValue = 0xAB;

        IMemory nametables = new DictionaryMemory();
        nametables[TargetAddress - 0x2000] = ExpectedValue;

        var ppu = new Ppu(nametables);

        // Load the high byte of the target address
        ppu.ListenWrite(0x2006, TargetAddress >> 8);

        // Load the low byte of the target address
        ppu.ListenWrite(0x2006, TargetAddress & 0xFF);

        // Read out the PPU's current buffer
        ppu.ListenRead(0x2007, out byte _);

        // Read again to get the byte at $2345
        ppu.ListenRead(0x2007, out byte outputValue);
        outputValue.ShouldBe(ExpectedValue);
    }
}
