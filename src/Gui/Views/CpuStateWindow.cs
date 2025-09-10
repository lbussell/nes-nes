// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using NesNes.Core;
using NesNes.Gui.Rendering;

namespace NesNes.Gui.Views;

internal class CpuStateWindow(NesConsole console)
    : ClosableWindow("CPU State", ImGuiWindowFlags.AlwaysAutoResize)
{
    private const string ByteFormat = "X2";
    private const string UshortFormat = "X4";

    public Action? OnTogglePause { get; set; } = null;
    public Action? OnStepInstruction { get; set; } = null;
    public Action? OnStepScanline { get; set; } = null;
    public Action? OnStepFrame { get; set; } = null;
    public Action? OnReset { get; set; } = null;

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

    protected unsafe override void RenderContent(double deltaTimeSeconds)
    {
        RenderControls();

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

        ImGui.SeparatorText("Instruction");
        ImGui.AlignTextToFramePadding();
        var opcode = _console.Cpu.CurrentOpcode;
        ImGui.InputScalar("Opcode", ImGuiDataType.U8, (nint)(&opcode), 0, 0, "0x%04X", ImGuiInputTextFlags.ReadOnly);
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

    private void RenderControls()
    {
        ImGuiHelper.RenderButton("Break", OnTogglePause);
        ImGui.SameLine();
        ImGuiHelper.RenderButton("Instr.", OnStepInstruction);
        ImGui.SameLine();
        ImGuiHelper.RenderButton("Scanline", OnStepScanline);
        ImGui.SameLine();
        ImGuiHelper.RenderButton("Frame", OnStepFrame);
        ImGui.SameLine();
        ImGuiHelper.RenderButton("Reset", OnReset);
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
