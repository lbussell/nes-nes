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

internal sealed class NameTableViewer : ClosableWindow
{
    private readonly NesConsole _console;
    private readonly PatternTableTexture _patternTable;

    public NameTableViewer(NesConsole console, PatternTableTexture patternTable)
        : base("Name Tables", startOpen: true)
    {
        _console = console;
        _patternTable = patternTable;
    }

    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.Borders
        | ImGuiTableFlags.RowBg
        | ImGuiTableFlags.ScrollY
        | ImGuiTableFlags.ScrollX
        | ImGuiTableFlags.NoHostExtendX
        | ImGuiTableFlags.SizingFixedSame;

    protected override void RenderContent(double deltaTimeSeconds)
    {
        if (_console.Bus.Mapper is null)
        {
            ImGui.Text("No cartridge loaded.");
            return;
        }

        if (ImGui.BeginTable("NT", 32, TableFlags))
        {
            // One name table is 32 tiles wide by 30 tiles tall. Each tile is one byte in the
            // nametable. There are 4 name tables, each 0x400 (256) bytes in size, starting at
            // $2000 in PPU memory.
            var nameTableData = _console.Bus.Mapper.PpuRead(address: 0x2000, length: 0x400);

            for (int y = 0; y < 30; y++)
            {
                ImGui.TableNextRow();
                for (int x = 0; x < 32; x++)
                {
                    ImGui.TableSetColumnIndex(x);
                    ImGui.Text($"{nameTableData[y * 32 + x]:X2}");
                }
            }

            ImGui.EndTable();
        }
    }
}
