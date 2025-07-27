// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using Shouldly;

namespace NesNes.Tests;

public class MemoryTests
{
    [Theory]
    [InlineData(0x0000, 0xAA)]
    [InlineData(0x0123, 0xAA)]
    [InlineData(0x07FF, 0xAA)]
    public void InternalRamShouldBeMirrored(ushort offset, byte inputValue)
    {
        offset.ShouldBeLessThan(MemoryRegions.InternalRamSize);
        ValidateMirroring(
            offset,
            inputValue,
            MemoryRegions.InternalRam,
            MemoryRegions.InternalRamEnd,
            MemoryRegions.InternalRamSize
        );
    }

    [Theory]
    [InlineData(0x00, 0xBB)]
    [InlineData(0x03, 0xBB)]
    public void PpuRegistersShouldBeMirrored(byte offset, byte inputValue)
    {
        ((ushort)offset).ShouldBeLessThan(MemoryRegions.PpuRegistersSize);
        ValidateMirroring(
            offset,
            inputValue,
            MemoryRegions.PpuRegisters,
            MemoryRegions.PpuRegistersEnd,
            MemoryRegions.PpuRegistersSize
        );
    }

    /// <summary>
    /// Loop through all of the mirrored locations for a given memory region,
    /// validating that modifying one location modifies all of the mirrored
    /// locations.
    /// </summary>
    /// <param name="offset">The offset within the memory region to
    /// modify.</param>
    /// <param name="inputValue">The value to write to the offset
    /// location.</param>
    /// <param name="mirrorStart">Start location of the mirrored
    /// region.</param>
    /// <param name="mirrorEnd">End location of the mirrored region.</param>
    /// <param name="mirrorSize">Size of each mirrored section of memory that
    /// will be repeated throughout the mirrored region.</param>
    private static void ValidateMirroring(
        ushort offset,
        byte inputValue,
        ushort mirrorStart,
        ushort mirrorEnd,
        ushort mirrorSize
    )
    {
        var ppu = new Ppu();
        IBus bus = new Bus()
        {
            Ppu = ppu
        };
        var numberOfMirrors = (mirrorEnd - mirrorStart - 1) / mirrorSize;

        // Write the input value to the base location
        var startingAddress = (ushort)(mirrorStart + offset);
        bus.CpuWrite(startingAddress, inputValue);

        // Validate that all mirrored locations contain the same value
        for (int n = 0; n < numberOfMirrors; n += 1)
        {
            var mirrorAddress = (ushort)(mirrorStart + n * mirrorSize + offset);
            byte outputValue = bus.CpuRead(mirrorAddress);
            outputValue.ShouldBe(
                inputValue,
                $"Memory mirroring failed at address: 0x{mirrorAddress:X4}"
            );
        }
    }
}
