// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public record DisassembledInstruction(
    string Name,
    AddressingMode Mode,
    byte Opcode,
    byte[] ExtraBytes
)
{
    public static DisassembledInstruction Unknown(byte opcode) =>
        new("", AddressingMode.Implicit, opcode, []);

    public override string ToString()
    {
        static string FormatAddressMode(byte[] extraBytes, AddressingMode mode)
        {
            return mode switch
            {
                AddressingMode.Immediate => $"#${extraBytes[0]:X2}",
                AddressingMode.ZeroPage => $"${extraBytes[0]:X2}",
                AddressingMode.ZeroPageX => $"${extraBytes[0]:X2}, X",
                AddressingMode.ZeroPageY => $"${extraBytes[0]:X2}, Y",
                AddressingMode.Absolute => $"${extraBytes[1]:X2}{extraBytes[0]:X2}",
                AddressingMode.AbsoluteX => $"${extraBytes[1]:X2}{extraBytes[0]:X2}, X",
                AddressingMode.AbsoluteY => $"${extraBytes[1]:X2}{extraBytes[0]:X2}, Y",
                AddressingMode.Indirect => $"(${extraBytes[1]:X2}{extraBytes[0]:X2})",
                AddressingMode.IndirectX => $"(${extraBytes[0]:X2}, X)",
                AddressingMode.IndirectY => $"(${extraBytes[0]:X2}), Y",
                _ => string.Empty,
            };
        }

        return $"{Name} {FormatAddressMode(ExtraBytes, Mode)}";
    }
};
