// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using NesNes.Gui.Rendering;

namespace NesNes.Gui.Views;

internal sealed class PatternTableViewer : ClosableWindow
{
    private readonly PatternTableTexture _texture;

    public PatternTableViewer(PatternTableTexture patternTableTexture)
        : base("Pattern Tables", startOpen: true)
    {
        _texture = patternTableTexture;
        _texture.UpdateTextureData();
    }

    protected override void RenderContent(double deltaTimeSeconds)
    {
        if (ImGui.Button("Refresh"))
        {
            _texture.UpdateTextureData();
        }

        var windowPosition = ImGui.GetWindowPos();

        ImGuiHelper.RenderTextureWithIntegerScaling(
            _texture,
            out var textureTopLeft,
            out int scale
        );

        if (ImGui.IsItemHovered())
        {
            if (ImGui.BeginItemTooltip())
            {
                int patternIndex = PatternTableTexture.GetHoveredPattern(
                    ImGui.GetMousePos(),
                    windowPosition,
                    textureTopLeft,
                    scale
                );

                int table = patternIndex / 256;
                int address = patternIndex * 16;
                int patternIndexWithinTable = patternIndex % 256;

                ImGui.Text($"Table {table}");
                ImGui.Text($"Tile ${patternIndexWithinTable:X2} ({patternIndexWithinTable})");
                ImGui.Text($"CHR Addr ${address:X4}");
                _texture.RenderPattern(patternIndex, scale: 16);

                ImGui.EndTooltip();
            }
        }
    }
}
