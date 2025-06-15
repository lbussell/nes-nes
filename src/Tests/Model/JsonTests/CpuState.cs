// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;

namespace NesNes.Tests.Model.JsonTests;

/// <summary>
/// Represents the state of the processor (registers and memory).
/// </summary>
public record CpuState(
    ushort PC,
    byte S,
    byte A,
    byte X,
    byte Y,
    byte P,
    int[][] Ram)
{
    public Registers GetRegisters()
    {
        return new Registers
        {
            PC = PC,
            SP = S,
            A = A,
            X = X,
            Y = Y,
            P = (Flags)P
        };
    }
}
