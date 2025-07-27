// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public interface IBus : ICpuReadable, ICpuWritable
{
}

public class Bus : IBus
{
    public required Ppu Ppu { get; init; }
    public CpuRam Ram { get; } = new CpuRam();
    public IMapper? Mapper { get; set; } = null;

    public Action TickCpu { get; set; } = () => { };

    /// <inheritdoc/>
    public byte CpuRead(ushort address)
    {
        if (address < 0x2000)
        {
            return Ram.CpuRead(address);
        }
        else if (Ppu is not null && address < 0x4000)
        {
            return Ppu.CpuRead(address);
        }
        else if (Mapper is not null && address >= MemoryRegions.PrgRom)
        {
            return Mapper.CpuRead(address);
        }

        throw new ArgumentOutOfRangeException(
            nameof(address),
            $"Address {address:X4} is out of range for CPU memory."
        );
    }

    /// <inheritdoc/>
    public void CpuWrite(ushort address, byte value)
    {
        if (address < 0x2000)
        {
            Ram.CpuWrite(address, value);
        }
        else if (Ppu is not null && address < 0x4000)
        {
            Ppu.CpuWrite(address, value);
        }
        else if (address == MemoryRegions.OamDma)
        {
            // OAM DMA transfer. The value written to the OAM DMA register is
            // the source address page to copy data from. All bytes from the
            // source page of CPU memory will be copied to OAM memory.
            DoOamDma(sourcePage: value);
        }
        else if (address >= MemoryRegions.PrgRom)
        {
            Mapper?.CpuWrite(address, value);
        }
    }

    /// <summary>
    /// Direct memory access (DMA) transfer from CPU memory to Object Attribute
    /// Memory (OAM). All bytes from the source page of CPU memory will be
    /// copied to OAM memory.
    /// </summary>
    /// <param name="sourcePage">
    /// The page in memory that will be copied to OAM in its entirety.
    /// </param>
    private void DoOamDma(byte sourcePage)
    {
        // There are a couple of dummy writes/ticks at the beginning of OAM DMA
        TickCpu();
        TickCpu();

        byte data;
        for (int i = 0; i <= 0xFF; i++)
        {
            data = CpuRead((ushort)((sourcePage << 8) + i));
            TickCpu();

            Ppu?.WriteOam((byte)i, data);
            TickCpu();
        }
    }
}
