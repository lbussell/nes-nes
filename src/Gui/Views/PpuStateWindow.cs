// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using NesNes.Core;

namespace NesNes.Gui.Views;

internal class PpuStateWindow(NesConsole console)
    : ClosableWindow("PPU State", ImGuiWindowFlags.AlwaysAutoResize)
{
    private const string ByteFormat = "X2";
    private const string UshortFormat = "X4";

    private readonly NesConsole _console = console;
    private readonly Ppu _ppu = console.Ppu;

    protected override void RenderContent(double deltaTimeSeconds)
    {
        ImGui.SeparatorText("PPU");

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Cycle");
        ImGui.SameLine();
        ImGui.Button(_ppu.Cycle.ToString());

        ImGui.SameLine();
        ImGui.Text("Scanline");
        ImGui.SameLine();
        ImGui.Button(_ppu.Scanline.ToString());

        ImGui.SeparatorText("Registers");

        ImGui.AlignTextToFramePadding();
        RenderUshort("V", _ppu.V);

        ImGui.SameLine();
        RenderUshort("T", _ppu.T);

        ImGui.SameLine();
        RenderByte("X", _ppu.FineXScroll);

        bool wIsChecked = _ppu.W;
        ImGui.Checkbox("W", ref wIsChecked);
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
}
