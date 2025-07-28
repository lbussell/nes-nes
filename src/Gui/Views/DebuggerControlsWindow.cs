// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;

namespace NesNes.Gui.Views;

internal class DebuggerControlsWindow()
    : ClosableWindow("Controls", ImGuiWindowFlags.AlwaysAutoResize, startOpen: true)
{
    public Action OnTogglePause { get; set; } = () => { };
    public Action OnStepScanline { get; set; } = () => { };
    public Action OnStepFrame { get; set; } = () => { };
    public Action OnReset { get; set; } = () => { };

    protected override void RenderContent(double deltaTimeSeconds)
    {
        if (ImGui.Button("Break/Continue"))
        {
            OnTogglePause.Invoke();
        }

        ImGui.SameLine();
        if (ImGui.Button("Scanline"))
        {
            OnStepScanline.Invoke();
        }

        ImGui.SameLine();
        if (ImGui.Button("Frame"))
        {
            OnStepFrame.Invoke();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset"))
        {
            OnReset.Invoke();
        }
    }
}
