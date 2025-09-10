// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;

namespace NesNes.Gui.Views;

internal class ExampleWindow() : ClosableWindow("Example")
{
    protected override void RenderContent(double deltaTimeSeconds)
    {
        ImGui.Text("Hello world!");
        ImGui.Text($"Delta time: {deltaTimeSeconds:F3} seconds");
    }
}
