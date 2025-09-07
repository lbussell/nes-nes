// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using NesNes.Core;
using NesNes.Gui.Rendering;
using Silk.NET.OpenGL;

namespace NesNes.Gui.Views;

internal sealed class NameTableViewer : ClosableWindow
{
    private static readonly Vector2 s_viewportSize = new(256, 240);
    private static readonly Vector2 s_attributeSize = new(32, 32);
    private static readonly Vector2 s_attributeSizeSmall = new(32, 16);
    private static readonly ImGuiColorEditFlags s_colorEditFlags =
        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel;

    private readonly NesConsole _console;
    private readonly PatternTableTexture _patternTable;
    private readonly NameTableTexture _nameTable;

    private static Vector3 s_viewportColor = new(1f, 0f, 0f);
    private static Vector3 s_attributeColor = new(0f, 0f, 1f);
    private static Vector3 s_attributeColor2 = new(0f, 1f, 0f);
    private bool _attributeDebug;
    private bool _attributeSubDebug;
    private bool _showViewport;

    public NameTableViewer(GL gl, NesConsole console, PatternTableTexture patternTable)
        : base("Name Tables", startOpen: true)
    {
        _console = console;
        _patternTable = patternTable;
        _nameTable = new NameTableTexture(gl, console, patternTable);
    }

    protected override void RenderContent(double deltaTimeSeconds)
    {
        if (_console.Bus.Mapper is null)
        {
            ImGui.Text("No cartridge loaded.");
            return;
        }

        ImGui.Checkbox("Show viewport", ref _showViewport);
        ImGui.SameLine();
        ImGui.ColorEdit3(nameof(s_viewportColor), ref s_viewportColor, s_colorEditFlags);
        ImGui.SameLine();
        ImGui.Checkbox("Attr. debug", ref _attributeDebug);
        ImGui.SameLine();
        ImGui.ColorEdit3(nameof(s_attributeColor), ref s_attributeColor, s_colorEditFlags);
        ImGui.SameLine();
        ImGui.Checkbox("Attr. debug++", ref _attributeSubDebug);
        ImGui.SameLine();
        ImGui.ColorEdit3(nameof(s_attributeColor2), ref s_attributeColor2, s_colorEditFlags);

        _nameTable.RenderToTexture();
        var thisWindowPosition = ImGui.GetWindowPos();
        ImGuiHelper.RenderTextureWithIntegerScaling(_nameTable, out var textureTopLeftInWindow, out var scale);
        var showToolTip = ImGui.IsItemHovered();

        var textureTopLeftAbsolute = thisWindowPosition + textureTopLeftInWindow;

        if (_attributeDebug || _attributeSubDebug)
        {
            // Draw rectangles for attribute tables
            ushort attributeAddress = 0x23C0;
            for (int table = 0; table < 4; table += 1)
            {
                var col = table % 2;
                var row = table / 2;
                var xOffset = col * 8;

                var attributes = _console.Bus.Mapper.PpuRead(attributeAddress, length: 16 * 4);

                for (int x = 0; x < 8; x += 1)
                {
                    var yOffset = row * 240;
                    for (int y = 0; y < 8; y += 1)
                    {
                        var attributeTableRelative = new Vector2((x + xOffset) * 32, y * 32 + yOffset);
                        attributeTableRelative *= scale;
                        var attributeTablePosition = textureTopLeftAbsolute + attributeTableRelative;
                        var attributeSize = y == 7 ? s_attributeSizeSmall : s_attributeSize;

                        var attributeIndex = y * 8 + x;
                        byte attributeByte = attributes[attributeIndex];

                        if (_attributeDebug)
                        {
                            DrawShadedRect(
                                attributeTablePosition,
                                attributeSize * scale,
                                s_attributeColor
                            );

                            ImGui.SetCursorPos(attributeTablePosition - thisWindowPosition);
                            ImGui.BeginGroup();
                            if (scale == 1)
                            {
                                ImGui.Text(attributeByte.ToString("X2"));
                            }
                            else
                            {
                                ImGui.Text($"A:${attributeAddress + attributeIndex:X4}");
                                ImGui.Text($"D:${attributeByte:X2}");
                            }
                            ImGui.EndGroup();
                        }

                        if (_attributeSubDebug)
                        {
                            // Draw 4 sub-rectangles within the attribute rectangle
                            // Each sub-rectangle represents one quadrant of the 4x4 tiles
                            // that the attribute byte covers.
                            var subRectSize = attributeSize / 2 * scale;
                            for (int subY = 0; subY < 2; subY += 1)
                            {
                                for (int subX = 0; subX < 2; subX += 1)
                                {
                                    var subRectPosition = attributeTablePosition
                                        + new Vector2(subX, subY) * subRectSize;

                                    DrawShadedRect(subRectPosition, subRectSize, s_attributeColor2);
                                    ImGui.SetCursorPos(subRectPosition - thisWindowPosition);
                                    ImGui.BeginGroup();

                                    int shift = (subY * 2 + subX) * 2;
                                    byte paletteBits = (byte)((attributeByte >> shift) & 0b11);

                                    ImGui.Text($"P:{paletteBits}");
                                    ImGui.EndGroup();
                                }
                            }
                        }
                    }
                }

                // Move to next table
                attributeAddress += 0x400;
            }
        }

        // Draw a rectangle around the viewport area.
        if (_showViewport)
        {
            // TODO: This should move with scrolling.
            var viewportPosition = textureTopLeftAbsolute + new Vector2(0, 0);
            ImGui.GetForegroundDrawList().AddRect(
                viewportPosition,
                viewportPosition + s_viewportSize * scale,
                ImGui.GetColorU32(new Vector4(s_viewportColor, 1f))
            );
        }

        if (showToolTip)
        {
            var mousePositionAbsolute = ImGui.GetMousePos();
            var mousePositionOverTexture = mousePositionAbsolute - textureTopLeftAbsolute;
            float tileSize = 8 * scale;

            int tileX = (int)(mousePositionOverTexture.X / tileSize);
            int tileY = (int)(mousePositionOverTexture.Y / tileSize);

            int ntX = tileX / 32; // 0 or 1
            int ntY = tileY / 30; // 0 or 1
            int nametableNumber = ntY * 2 + ntX; // 0-3

            int localX = tileX % 32;
            int localY = tileY % 30;
            int tileIndexInNameTable = localY * 32 + localX; // 0-959

            ushort nameTableBase = (ushort)(0x2000 + nametableNumber * 0x400);
            ushort nameTableAddress = (ushort)(nameTableBase + tileIndexInNameTable);

            // Read pattern index (tile number) from PPU memory
            byte patternIndex = _console.Bus.Mapper.PpuRead(nameTableAddress);

            bool useSecondPatternTable = _console.Ppu.BackgroundPatternTableAddress > 0;
            int patternTableNumber = useSecondPatternTable ? 1 : 0;
            int globalPatternIndex = patternIndex + (patternTableNumber * 256);
            int patternAddress = _console.Ppu.BackgroundPatternTableAddress + (patternIndex * 16);

            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text($"Nametable {nametableNumber} (${nameTableBase:X4})");
                ImGui.Text($"Tile ({localX},{localY}) -> NT Addr ${nameTableAddress:X4}");
                ImGui.Text($"Pattern Index ${patternIndex:X2} ({patternIndex})");
                ImGui.Text($"Pattern Table {patternTableNumber}");
                ImGui.Text($"Pattern Addr ${patternAddress:X4}");
                _patternTable.RenderPattern(globalPatternIndex, scale: 8);
                ImGui.EndTooltip();
            }
        }
    }

    private static void DrawShadedRect(Vector2 topLeft, Vector2 size, Vector3 color)
    {
        var drawList = ImGui.GetForegroundDrawList();
        var shadeColor = new Vector4(color, 0.25f);
        var outlineColor = new Vector4(color, 1f);
        drawList.AddRectFilled(topLeft, topLeft + size, ImGui.GetColorU32(shadeColor));
        drawList.AddRect(topLeft, topLeft + size, ImGui.GetColorU32(outlineColor));
    }
}
