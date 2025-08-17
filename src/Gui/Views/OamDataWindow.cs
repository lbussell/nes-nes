// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using NesNes.Core;

namespace NesNes.Gui.Views;

internal class OamDataWindow(NesConsole console) : ClosableWindow("Object Attribute Memory")
{
    private const string On = "1";
    private const string Off = "0";
    private const string Yes = "Y";
    private const string No = "N";

    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.Borders
        | ImGuiTableFlags.RowBg
        | ImGuiTableFlags.SizingStretchProp;

    private readonly NesConsole _console = console;

    protected override void RenderContent(double deltaTimeSeconds)
    {
        // var oam = _console.Ppu.Oam;
        var oam = new Span<byte>();

        if (ImGui.BeginTable("OAM Data", 9, TableFlags))
        {
            // Extra spaces for the first few columns to make them wider. They
            // need to be able to fit 2/3 characters without constantly re-sizing.
            ImGui.TableSetupColumn("# ");
            ImGui.TableSetupColumn("Y  ");
            ImGui.TableSetupColumn("X  ");
            ImGui.TableSetupColumn("Tile");
            ImGui.TableSetupColumn("Bank");
            ImGui.TableSetupColumn("VF");
            ImGui.TableSetupColumn("HF");
            ImGui.TableSetupColumn("Pri");
            ImGui.TableSetupColumn("Pal");
            ImGui.TableHeadersRow();

            for (int spriteIndex = 0; spriteIndex * 4 < oam.Length; spriteIndex += 1)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(spriteIndex.ToString());

                ImGui.TableNextColumn();
                ShowSpriteData(oam.Slice(spriteIndex * 4, 4));
            }

            ImGui.EndTable();
        }
    }

    private static void ShowSpriteData(ReadOnlySpan<byte> spriteData)
    {
        // Y
        ImGui.Text(spriteData[0].ToString());

        // X position
        ImGui.TableNextColumn();
        ImGui.Text(spriteData[3].ToString());

        // Tile index number
        ImGui.TableNextColumn();
        var byte1 = spriteData[1];
        ImGui.Text((byte1 & 0xFE).ToString());

        // Bank of tiles to use (0 or 1)
        ImGui.TableNextColumn();
        var bank = byte1 & 0x01;
        ImGui.Text(bank.ToString());

        var byte2 = spriteData[2];

        // Flip sprite vertically
        ImGui.TableNextColumn();
        var flipVertically = (byte2 & 0x80) != 0;
        ImGui.Text(flipVertically ? Yes : No);

        // Flip sprite horizontally
        ImGui.TableNextColumn();
        var flipHorizontally = (byte2 & 0x40) != 0;
        ImGui.Text(flipHorizontally ? Yes : No);

        // Priority
        ImGui.TableNextColumn();
        var priority = (byte2 & 0x20) != 0;
        ImGui.Text(priority ? On : Off);

        // Palette number
        ImGui.TableNextColumn();
        var palette = byte2 & 0x03;
        ImGui.Text(palette.ToString());
    }
}
