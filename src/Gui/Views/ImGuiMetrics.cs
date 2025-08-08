// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;

namespace NesNes.Gui.Views;

internal class ImGuiMetrics : IClosableWindow
{
    private bool _isOpen = false;

    public ref bool Open => ref _isOpen;

    public string Name { get; } = "ImGui Metrics";

    public void Render(double _)
    {
        if (_isOpen)
        {
            ImGui.ShowMetricsWindow(ref _isOpen);
        }
    }
}
