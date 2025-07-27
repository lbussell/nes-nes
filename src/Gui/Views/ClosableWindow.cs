// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;

internal abstract class ClosableWindow(string name, bool startOpen = false) : IImGuiWindow
{
    private bool _isOpen = startOpen;

    public string Name { get; } = name;

    public ref bool Open => ref _isOpen;

    public void Render(double deltaTimeSeconds)
    {
        if (_isOpen)
        {
            ImGui.Begin(Name, ref _isOpen);
            RenderContent(deltaTimeSeconds);
            ImGui.End();
        }
    }

    /// <summary>
    /// Renders the content of the window. This will only be called if the
    /// window is open.
    /// </summary>
    /// <param name="deltaTimeSeconds">
    /// Time in seconds since the last time this method was called.
    /// </param>
    protected abstract void RenderContent(double deltaTimeSeconds);
}
