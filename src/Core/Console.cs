// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class Console
{
    public required Cpu Cpu { get; init; }
    public void LoadRom(byte[] rom) { }
}
