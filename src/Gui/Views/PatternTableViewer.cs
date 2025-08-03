// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

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

    private double _timeSinceLastUpdate = 0.0;

    // Only update the texture every 1/30th of a second
    private const double UpdateInterval = 1.0 / 30.0;

    /// <summary>
    /// Size of both pattern tables, stacked on top of each other vertically.
    /// </summary>
    private static readonly Vector2D<int> s_patternTablesSize =
        new(Ppu.PatternTablePixelWidth, 2 * Ppu.PatternTablePixelHeight);

    public PatternTableViewer(GL openGl, NesConsole console) : base("Pattern Tables")
    {
        _console = console;
        _texture = new Texture(openGl, s_patternTablesSize);
        UpdateTexture();
    }

    protected override void RenderContent(double deltaTimeSeconds)
    {
        var shouldUpdate = false;
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
        for (int pixelRow = 0; pixelRow < s_patternTablesSize.Y; pixelRow += 1)
        {
            var table = pixelRow < Ppu.PatternTablePixelHeight ? 0 : 1;

            for (int pixelCol = 0; pixelCol < s_patternTablesSize.X; pixelCol += 1)
            {
                var color = _console.Ppu.GetPatternTablePixel(
                    pixelRow % Ppu.PatternTablePixelHeight,
                    pixelCol,
                    table,
                    _grayscale
                );
                _texture.SetPixel(pixelCol, pixelRow, color.R, color.G, color.B, 0xFF);
            }
        }

        _texture.UpdateTextureData();
    }
}
