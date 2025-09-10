// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using NesNes.Core;

namespace NesNes.Gui.Views;

internal class PpuStateWindow(NesConsole console)
    : ClosableWindow("PPU State", ImGuiWindowFlags.AlwaysAutoResize)
{
    private readonly NesConsole _console = console;
    private readonly IPpu _ppu = console.Ppu;

    protected override void RenderContent(double deltaTimeSeconds)
    {
        ImGui.SeparatorText("PPU");

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Cycle");
        ImGui.SameLine();
        ImGui.Button(_ppu.Cycle.ToString());

        var paletteBytes = _console.Ppu.PaletteRam;

        ImGui.Text("Palettes");
        for (int p = 0; p < 8; p++)
        {
            int baseIndex = p * 4; // 0,4,8,12
            ImGui.Text($"{p}");
            ImGui.SameLine();

            for (int i = 0; i < 4; i++)
            {
                var nesIndex = paletteBytes[baseIndex + i];
                RenderColor(nesIndex, baseIndex + i);
                ImGui.SameLine();
            }

            ImGui.NewLine();
        }
    }

    private static void RenderColor(byte nesIndex, int idx)
    {
        var color = Palette.Colors[nesIndex];
        var colorVector = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f);

        ImGui.ColorButton(
            $"##p{idx}",
            colorVector,
            ImGuiColorEditFlags.NoTooltip,
            new Vector2(16, 16)
        );
    }
}
