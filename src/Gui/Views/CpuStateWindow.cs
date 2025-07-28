// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using NesNes.Core;

namespace NesNes.Gui.Views;

internal class CpuStateWindow(NesConsole console)
    : ClosableWindow("CPU State", WindowFlags, startOpen: true)
{
    private const string ByteFormat = "X2";
    private const string UshortFormat = "X4";

    private const ImGuiWindowFlags WindowFlags =
        ImGuiWindowFlags.AlwaysAutoResize;

    private static readonly (string, Flags)[][] s_checkboxes =
    [
        [
            // Row 1
            ("Carry", Flags.Carry),
            ("Zero", Flags.Zero),
            ("Interrupt", Flags.InterruptDisable),
            ("Decimal", Flags.DecimalMode),
        ],
        [
            // Row 2
            ("Break", Flags.B),
            ("Unused", Flags.Unused),
            ("Overflow", Flags.Overflow),
            ("Negative", Flags.Negative)
        ],
    ];

    private readonly NesConsole _console = console;
    private readonly Cpu _cpu = console.Cpu;

    protected override void RenderContent(double deltaTimeSeconds)
    {
        ImGui.SeparatorText("CPU");

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Cycles");
        ImGui.SameLine();
        ImGui.Button(_console.CpuCycles.ToString());

        ImGui.SeparatorText("Registers");
        var registers = _cpu.Registers;

        ImGui.AlignTextToFramePadding();
        RenderUshort("PC", registers.PC);

        ImGui.SameLine();
        RenderUshort("SP", registers.SP);

        ImGui.SameLine();
        RenderByte("A", registers.A);

        ImGui.SameLine();
        RenderByte("X", registers.X);

        ImGui.SameLine();
        RenderByte("Y", registers.Y);

        ImGui.SameLine();
        RenderByte("P", (byte)registers.P);

        ImGui.SeparatorText("Flags (P)");
        RenderFlagsCheckboxes(registers.P);
    }

    private static void RenderByte(string label, byte value)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.Button(value.ToString(ByteFormat));
    }

    private static void RenderUshort(string label, ushort value)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.Button(value.ToString(UshortFormat));
    }

    private static void RenderFlagsCheckboxes(Flags flags)
    {
        foreach (var row in s_checkboxes)
        {
            foreach (var (label, flag) in row)
            {
                bool isChecked = flags.HasFlag(flag);
                ImGui.Checkbox(label, ref isChecked);
                ImGui.SameLine();
            }

            ImGui.NewLine();
        }
    }
}
