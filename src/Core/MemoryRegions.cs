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
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
 * │ $1000-$17FF │ $0800 │ Mirrors of $0000-$07FF                         │
 * ├─────────────┼───────┼────────────────────────────────────────────────┤
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
 * │ $8000-$FFFF │ $8000 │ Usually cartridge ROM and mapper registers.    │
 * └─────────────┴───────┴────────────────────────────────────────────────┘
 */

public static class MemoryRegions
{
    /// <summary>
    /// The processor supports a 256 byte stack located between 0x0100 and
    /// 0x01FF. The stack pointer is an 8 bit register and holds the low 8 bits
    /// of the next free location on the stack. The location of the stack is
    /// fixed and cannot be moved.
    /// </summary>
    public const ushort Stack = 0x0100;
}
