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

internal sealed class PatternTableTexture : IImGuiRenderable
{
    private readonly Texture _texture;
    private readonly NesConsole _console;

    public nint Handle => _texture.Handle;
    public Vector2D<int> Size => _texture.Size;

    public PatternTableTexture(GL gl, NesConsole console)
    {
        _texture = new Texture(gl, s_patternTablesSizePixels);
        _console = console;
    }

    public static int GetHoveredPattern(
        Vector2 mousePosition,
        Vector2 windowPosition,
        Vector2 textureTopLeft,
        int scale
    )
    {
        var mousePositionOverTexture = mousePosition - windowPosition - textureTopLeft;

        int tileSize = 8 * scale;
        int tileX = (int)(mousePositionOverTexture.X / tileSize);
        int tileY = (int)(mousePositionOverTexture.Y / tileSize);
        int patternIndex = tileY * 16 + tileX;
        return patternIndex;
    }

    // patternIndex: 0-511 (0-255 = first table (top), 256-511 = second table (bottom))
    public void RenderPattern(int patternIndex)
    {
        int table = patternIndex / 256; // 0 or 1
        int indexWithinTable = patternIndex % 256; // 0-255
        int tileX = indexWithinTable % 16;
        int tileY = (indexWithinTable / 16) + (table * 16); // second table starts 16 tiles (128 px) down
        var (uv0, uv1) = GetTileUvs(tileX, tileY);
        ImGui.Image(_texture.Handle, new Vector2(128, 128), uv0, uv1);
    }

    private static (Vector2 uv0, Vector2 uv1) GetTileUvs(int tileX, int tileY)
    {
        var pixelX = tileX * 8;
        var pixelY = tileY * 8;

        var width = (float)s_patternTablesSizePixels.X;
        var height = (float)s_patternTablesSizePixels.Y;

        var uv0 = new Vector2(pixelX / width, pixelY / height);
        var uv1 = new Vector2((pixelX + 8f) / width, (pixelY + 8f) / height);
        return (uv0, uv1);
    }

    public void UpdateTextureData()
    {
        if (_console.Cartridge is null)
        {
            return;
        }

        // Patterns are 8x8 pixels, arranged in two 16x16 tables ("left" and "right" tables).
        // However, we want to render them as one 16x32 table (left table on top, right table on
        // bottom).
        for (int patternIndex = 0; patternIndex < 512; patternIndex += 1)
        {
            var xOffset = (patternIndex % 16) * 8;
            var yOffset = (patternIndex / 16) * 8;

            for (int y = 0; y < 8; y += 1)
            {
                // Each pattern is represented with 16 bytes - made up of two 8-byte planes.
                // For each row of pixels, load up shifter registers.
                var address = (patternIndex * 16) + y;
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

                    _texture.SetPixel(
                        xOffset + x,
                        yOffset + y,
                        grayValue,
                        grayValue,
                        grayValue,
                        0xFF
                    );

                    // Shift registers to get the next pixel.
                    lowShifter <<= 1;
                    highShifter <<= 1;
                }
            }
        }

        _texture.UpdateTextureData();
    }

    private static readonly Vector2D<int> s_patternTablesSizePixels =
        new(PpuConsts.PatternTablePixelWidth, 2 * PpuConsts.PatternTablePixelHeight);
}
