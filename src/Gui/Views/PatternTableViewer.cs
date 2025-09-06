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
                var mousePosition = ImGui.GetMousePos();
                var mousePositionOverTexture = mousePosition - windowPosition - textureTopLeft;

                int tileSize = 8 * scale;
                int tileX = (int)(mousePositionOverTexture.X / tileSize);
                int tileY = (int)(mousePositionOverTexture.Y / tileSize);
                int patternIndex = tileY * 16 + tileX;

                ImGui.Text($"Tile ${patternIndex:X2} ({patternIndex})");
                ImGui.Text($"CHR Addr ${patternIndex * 16:X4}");

                _texture.RenderPattern(tileX, tileY);
                ImGui.EndTooltip();
            }
        }
    }
}
