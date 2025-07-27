// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;

internal class ExampleWindow() : ClosableWindow("Example", startOpen: false)
{
    protected override void RenderContent(double deltaTimeSeconds)
    {
        ImGui.Text("Hello world!");
        ImGui.Text($"Delta time: {deltaTimeSeconds:F3} seconds");
    }
}
