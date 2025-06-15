// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public delegate int InstructionHandler(AddressingMode mode);

public readonly record struct Instruction(
    string Name,
    InstructionHandler Handler,
    AddressingMode AddressingMode,
    int Cycles)
{
    /// <summary>
    /// Executes the instruction using the provided handler and addressing mode.
    /// </summary>
    /// <returns>
    /// The total number of cycles the instruction takes, including any extra
    /// cycles incurred by crossing page boundaries.
    /// </returns>
    public int Execute()
    {
        var extraCycles = Handler(AddressingMode);
        return Cycles + extraCycles;
    }

    public bool HasValue() => Handler is not null;
};
