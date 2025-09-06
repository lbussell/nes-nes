// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using NesNes.Core;
using NesNes.Gui.Rendering;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Texture = NesNes.Gui.Rendering.Texture;

namespace NesNes.Gui.Views;

internal class PatternTableViewer : ClosableWindow
{
    private readonly NesConsole _console;
    private readonly Texture _texture;

    private static readonly Vector2D<int> s_patternTablesSizePixels =
        new(2 * PpuConsts.PatternTablePixelWidth, PpuConsts.PatternTablePixelHeight);

    public PatternTableViewer(GL openGl, NesConsole console) : base("Pattern Tables", startOpen: true)
    {
        _console = console;
        _texture = new Texture(openGl, s_patternTablesSizePixels);
        UpdateTexture();
    }

    protected override void RenderContent(double deltaTimeSeconds)
    {
        if (ImGui.Button("Refresh"))
        {
            UpdateTexture();
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

                var (uv0, uv1) = GetTileUv(tileX, tileY);
                ImGui.Image(_texture.Handle, new Vector2(128, 128), uv0, uv1);
            }
        }
    }

    private static (Vector2 uv0, Vector2 uv1) GetTileUv(int tileX, int tileY)
    {
        var pixelX = tileX * 8;
        var pixelY = tileY * 8;

        var width = (float)s_patternTablesSizePixels.X;
        var height = (float)s_patternTablesSizePixels.Y;

        var uv0 = new Vector2(pixelX / width, pixelY / height);
        var uv1 = new Vector2((pixelX + 8f) / width, (pixelY + 8f) / height);
        return (uv0, uv1);
    }

    private void UpdateTexture()
    {
        if (_console.Cartridge is null)
        {
            return;
        }

        // Patterns are 8x8 pixels, arranged in two 16x16 tables (left and right table).
        for (int table = 0; table < 2; table += 1)
        {
            // First table starts at $0, second starts at $1000
            var tableOffset = table * 0x1000;

            for (int patternIndex = 0; patternIndex < 256; patternIndex += 1)
            {
                var xOffset = (patternIndex % 16) * 8;
                var yOffset = (patternIndex / 16) * 8;

                // Draw the second table to the right of the first
                if (table == 1)
                {
                    xOffset += 128;
                }

                for (int y = 0; y < 8; y += 1)
                {
                    // Each pattern is represented with 16 bytes - made up of two 8-byte planes.
                    // For each row of pixels, load up shifter registers.
                    var address = tableOffset + (patternIndex * 16) + y;
                    byte lowShifter = _console.Cartridge.ChrRom[address];
                    byte highShifter = _console.Cartridge.ChrRom[address + 8];

                    for (int x = 0; x < 8; x += 1)
                    {
                        // Shift bits out of the high bits to get a pixel's color index.
                        var lowBit = (lowShifter & 0x80) > 0 ? 1 : 0;
                        var highBit = (highShifter & 0x80) > 0 ? 1 : 0;
                        var colorIndex = (highBit << 1) | lowBit;

                        // Just do grayscale for now.
                        var grayValue = (byte)(colorIndex * 85);
                        _texture.SetPixel(xOffset + x, yOffset + y, grayValue, grayValue, grayValue, 0xFF);

                        // Shift registers to get the next pixel.
                        lowShifter <<= 1;
                        highShifter <<= 1;
                    }
                }
            }
        }

        _texture.UpdateTextureData();
    }
}
