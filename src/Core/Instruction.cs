// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public delegate int InstructionHandler(AddressingMode mode);

/// <summary>
/// Represents a single instruction in a 6502 CPU, including everything needed
/// to execute the instruction.
/// </summary>
/// <param name="Name">
/// The name of the instruction, e.g., "LDA", "STA", etc.
/// </param>
/// <param name="Handler">
/// The handler function that executes the instruction.
/// </param>
/// <param name="AddressingMode">
/// The addressing mode used by the instruction.
/// </param>
/// <param name="Cycles">
/// The number of CPU cycles the instruction normally takes to execute. This
/// does not include any extra cycles that may be incurred by crossing page
/// boundaries.
/// </param>
public readonly record struct Instruction(
    string Name,
    InstructionHandler Handler,
    AddressingMode AddressingMode,
    int Cycles
)
{
    /// <summary>
    /// Executes the instruction using the provided handler and addressing mode.
    /// </summary>
    /// <returns>
    /// The number of extra cycles incurred by the instruction takes, for
    /// example by crossing page boundaries.
    /// </returns>
    public int Execute()
    {
        var extraCycles = Handler(AddressingMode);
        return extraCycles;
    }

    /// <summary>
    /// Returns true if the instruction has a valid handler function.
    /// </summary>
    public bool HasValue() => Handler is not null;
};
