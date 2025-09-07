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
    private static readonly Vector4 s_viewportColor = new(1f, 0f, 0f, 1f);

    private readonly NesConsole _console;
    private readonly PatternTableTexture _patternTable;
    private readonly NameTableTexture _nameTable;

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

        _nameTable.RenderToTexture();
        var windowPosition = ImGui.GetWindowPos();
        ImGuiHelper.RenderTextureWithIntegerScaling(_nameTable, out var textureTopLeft, out var scale);

        if (ImGui.IsItemHovered())
        {
            var mouse = ImGui.GetMousePos();
            var relative = mouse - windowPosition - textureTopLeft; // in pixels of scaled texture
            float tileSize = 8 * scale;

            int tileX = (int)(relative.X / tileSize);
            int tileY = (int)(relative.Y / tileSize);

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
                _patternTable.RenderPattern(globalPatternIndex, scale: 8); // show enlarged pattern
                ImGui.EndTooltip();
            }
        }

        // Draw a rectangle around the viewport area.
        // TODO: This should move with scrolling.
        var viewportPosition = windowPosition + textureTopLeft + new Vector2(0, 0);
        ImGui.GetForegroundDrawList().AddRect(
            viewportPosition,
            viewportPosition + s_viewportSize,
            ImGui.GetColorU32(s_viewportColor)
        );
    }
}
