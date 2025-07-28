// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using ImGuiNET;
using NesNes.Core;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Texture = NesNes.Gui.Rendering.Texture;

namespace NesNes.Gui.Views;

internal class PatternTableViewer : ClosableWindow
{
    private readonly NesConsole _console;
    private readonly Texture _texture;
    private bool _grayscale = false;
    private int _table = 0;

    private double _timeSinceLastUpdate = 0.0;
    private const double UpdateInterval = 1.0 / 60.0; // 1/60th of a second

    private static readonly Vector2D<int> s_patternTableSize =
        new(Ppu.PatternTablePixelWidth, Ppu.PatternTablePixelHeight);
        // new(Ppu.PatternTablePixelWidth, 2 * Ppu.PatternTablePixelHeight);

    private static readonly Vector2 s_patternTableSizeFloat =
        new(Ppu.PatternTablePixelWidth, Ppu.PatternTablePixelHeight);
        // new(Ppu.PatternTablePixelWidth, 2 * Ppu.PatternTablePixelHeight);

    public PatternTableViewer(GL openGl, NesConsole console)
        : base("Pattern Tables", startOpen: true)
    {
        _console = console;
        _texture = new Texture(openGl, s_patternTableSize);
        UpdateTexture();
    }

    protected override void RenderContent(double deltaTimeSeconds)
    {
        var shouldUpdate = false;

        if (ImGui.Checkbox("Grayscale", ref _grayscale))
        {
            shouldUpdate = true; // Force immediate update when grayscale changes
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("Table", ref _table, 1, 1);

        // Ensure table is always 0 or 1
        _table %= 2;

        _timeSinceLastUpdate += deltaTimeSeconds;
        if (_timeSinceLastUpdate >= UpdateInterval)
        {
            shouldUpdate = true;
            _timeSinceLastUpdate = 0.0;
        }

        if (shouldUpdate)
        {
            UpdateTexture();
        }

        ImGuiHelper.RenderTextureWithIntegerScaling(_texture);
    }

    private void UpdateTexture()
    {

        for (int pixelRow = 0; pixelRow < s_patternTableSize.Y; pixelRow += 1)
        {
            for (int pixelCol = 0; pixelCol < s_patternTableSize.X; pixelCol += 1)
            {
                var color = _console.Ppu.GetPatternTablePixel(
                    pixelRow,
                    pixelCol,
                    _table,
                    _grayscale
                );
                _texture.SetPixel(pixelCol, pixelRow, color.R, color.G, color.B, 0xFF);
            }
        }

        _texture.UpdateTextureData();
    }
}
