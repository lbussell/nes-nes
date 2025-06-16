// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class Console(Cpu cpu, Memory memory)
{
    private readonly Cpu _cpu = cpu;
    private Memory _memory = memory;

    public void LoadRom(byte[] rom)
    {
        _cpu.Reset();
        _memory.LoadRom(rom);
    }

    public void Start() { }
}
