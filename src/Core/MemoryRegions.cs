// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

/*
 * NES Memory Map
 *
 * ┌─────────────┬───────┬────────────────────────────────────────────────┐
 * │ Address     │ Size  │ Device                                         │
 * │ Range       │       │                                                │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $0000-$07FF │ $0800 │ 2 KB internal RAM                              │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $0800-$0FFF │ $0800 │                                                │
 * ├─────────────┼───────┤                                                │
 * │ $1000-$17FF │ $0800 │ Mirrors of $0000-$07FF                         │
 * ├─────────────┼───────┤                                                │
 * │ $1800-$1FFF │ $0800 │                                                │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $2000-$2007 │ $0008 │ NES PPU registers                              │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $2008-$3FFF │ $1FF8 │ Mirrors of $2000-$2007 (repeats every 8 bytes) │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $4000-$4017 │ $0018 │ NES APU and I/O registers                      │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $4018-$401F │ $0008 │ APU and I/O functionality that is normally     │
 * │             │       │ disabled. See CPU Test Mode.                   │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $4020-$FFFF │ $BFE0 │ Unmapped. Available for cartridge use.         │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $6000-$7FFF │ $2000 │ Usually cartridge RAM, when present.           │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $8000-$BFFF │ $4000 │                                                │
 * ├─────────────┼───────┤ ROM pages                                      │
 * │ $C000-$FFFF │ $4000 │                                                │
 * └─────────────┴───────┴────────────────────────────────────────────────┘
 */

public static class MemoryRegions
{
    /// <summary>
    /// The first 2 KB (<see cref="InternalRamSize"/>) of memory is internal
    /// RAM. It is mirrored every 2 KB, all the way up to <see
    /// cref="InternalRamEnd"/>.
    /// </summary>
    public const ushort InternalRam = 0x0000;

    public const ushort InternalRamSize = 0x0800;

    /// <summary>
    /// The processor supports a 256 byte stack located between 0x0100 and
    /// 0x01FF. The stack pointer is an 8 bit register and holds the low 8 bits
    /// of the next free location on the stack. The location of the stack is
    /// fixed and cannot be moved.
    /// </summary>
    public const ushort Stack = 0x0100;

    public const ushort InternalRamEnd = PpuRegisters - 1;

    /// <summary>
    /// The PPU registers are located at 0x2000 to 0x2007. They are mirrored
    /// every 8 bytes up to <see cref="PpuRegistersEnd"/> .
    /// </summary>
    public const ushort PpuRegisters = 0x2000;

    /// <summary>
    /// Bits: VPHB SINN
    /// - V: NMI Enable
    /// - P: PPU master/slave
    /// - H: Sprite height
    /// - B: Background tile select
    /// - S: Sprite tile select
    /// - I: Increment mode
    /// - N/NN: Nametable select / X and Y scroll bit 8 (NN)
    /// </summary>
    public const ushort PpuCtrl = 0x2000;

    /// <summary>
    /// Bits: BGRs bMmG
    /// - BGR: Background color
    /// - s: Sprite enable
    /// - b: Background enable
    /// - M: Sprite left column enable
    /// - m: Background left column enable
    /// - G: Grayscale mode
    /// </summary>
    public const ushort PpuMask = 0x2001;

    /// <summary>
    /// Bits: VSO- ----
    /// - V: vblank
    /// - S: sprite 0 hit
    /// - O: sprite overflow
    /// </summary>
    /// <remarks>
    /// Read resets write pair for $2005/$2006.
    /// </remarks>
    public const ushort PpuStatus = 0x2002;

    /// <summary>
    /// OAM read/write address.
    /// </summary>
    public const ushort OamAddress = 0x2003;

    /// <summary>
    /// OAM data read/write.
    /// </summary>
    public const ushort OamData = 0x2004;

    /// <summary>
    /// PPU scroll register.
    /// </summary>
    public const ushort PpuScroll = 0x2005;

    /// <summary>
    /// VRAM address
    /// </summary>
    public const ushort PpuAddress = 0x2006;

    /// <summary>
    /// VRAM data read/write.
    /// </summary>
    public const ushort PpuData = 0x2007;

    public const ushort PpuRegistersSize = 0x0008;

    /// <summary>
    /// The inclusive end of the PPU registers
    /// </summary>
    public const ushort PpuRegistersEnd = ApuIoRegisters - 1;

    public const ushort ApuIoRegisters = 0x4000;

    public const ushort OamDma = 0x4014;

    public const ushort PrgRom = 0x8000;
    public const ushort PrgRomEnd = 0xFFFF;
    public const ushort PrgRomSize = 0x8000;

    public const ushort NmiVector = 0xFFFA;
    public const ushort ResetVector = 0xFFFC;

    public const int TotalSize = 0x10000;
}
