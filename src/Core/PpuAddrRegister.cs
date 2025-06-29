// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class PpuAddrRegister
{
    /*
    From https://www.nesdev.org/wiki/PPU_registers#PPUDATA_-_VRAM_data_($2007_read/write)

    Reading from PPUDATA does not directly return the value at the
    current VRAM address, but instead returns the contents of an
    internal read buffer. This read buffer is updated on every PPUDATA
    read, but only after the previous contents have been returned to
    the CPU, effectively delaying PPUDATA reads by one. This is because
    PPU bus reads are too slow and cannot complete in time to service
    the CPU read. Because of this read buffer, after the VRAM address
    has been set through PPUADDR, one should first read PPUDATA to
    prime the read buffer (ignoring the result) before then reading the
    desired data from it.

    Note that the read buffer is updated only on PPUDATA reads. It is
    not affected by writes or other PPU processes such as rendering, and
    it maintains its value indefinitely until the next read.
    */

    // The high byte of the target address is always written first,
    // followed by the low byte. This is opposite of most other memory
    // functions in the NES which are little-endian.
    private bool _nextWriteIsLowByte;

    public ushort Value { get; private set; }

    public void Write(byte value)
    {
        if (_nextWriteIsLowByte)
        {
            Value = (ushort)((Value & 0xFF00) | value);
        }
        else
        {
            Value = (ushort)((Value & 0x00FF) | (value << 8));
        }

        _nextWriteIsLowByte = !_nextWriteIsLowByte;
    }

    public void ResetLatch()
    {
        _nextWriteIsLowByte = false;
    }
}
