// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using Shouldly;

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

    [Theory] //    low          high      i  expected
    //    76543210     76543210
    [InlineData(0b_11000001, 0b_10000101, 0, 0b_11)]
    [InlineData(0b_11000001, 0b_10000101, 1, 0b_00)]
    [InlineData(0b_11000001, 0b_10000101, 2, 0b_10)]
    [InlineData(0b_11000001, 0b_10000101, 6, 0b_01)]
    [InlineData(0b_11000001, 0b_10000101, 7, 0b_11)]
    public void ZipBytes(byte lowByte, byte highByte, byte index, byte expected)
    {
        var zipped = Ppu.ZipBytes(lowByte, highByte, index);

        zipped.ShouldBe(
            expected,
            $"""

                       76543210
            Low Byte:  {lowByte:b8}
            High Byte: {highByte:b8}
            Index:     {index}

            Expected: {expected:b2}
            Actual:   {zipped:b2}

            """
        );
    }
}
