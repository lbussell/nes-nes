// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using NesNes.Core;

namespace NesNes.Gui.Views;

internal class CartridgeInfo : ClosableWindow
{
    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.Borders
        | ImGuiTableFlags.RowBg;

    private const ImGuiWindowFlags WindowFlags =
        ImGuiWindowFlags.AlwaysAutoResize;

    private readonly (string, string)[] _tableRows;

    public CartridgeInfo(CartridgeData cartridge)
        : base("Cartridge Info", WindowFlags, startOpen: false)
    {
        var header = cartridge.Header;
        _tableRows =
        [
            ("Name",                    cartridge.Name),
            ("Size (bytes)",            cartridge.Size.ToString()),
            ("Header format",           header.IsNes2Header ? "NES 2.0" : "iNES 1.0"),
            ("Has Trainer",             header.HasTrainer ? "Yes" : "No"),
            ("PRG_ROM Pages",           header.PrgPages.ToString()),
            ("PRG_ROM Size (bytes)",    header.PrgRomSize.ToString()),
            ("CHR_ROM Pages",           header.ChrPages.ToString()),
            ("CHR_ROM Size (bytes)",    header.ChrRomSize.ToString()),
            ("Mapper",                  header.Mapper.ToString()),
            ("Mirroring",               header.NametableArrangement.ToString()),
            ("Alt. Mirroring",          header.AlternateNametableLayout ? "Yes" : "No"),
        ];
    }

    protected override void RenderContent(double deltaTimeSeconds)
    {
        RenderTable();
    }

    private void RenderTable()
    {
        if (ImGui.BeginTable("Cartridge Info", 2, TableFlags))
        {
            foreach (var (label, value) in _tableRows)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(label);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(value);
            }

            ImGui.EndTable();
        }
    }
}
