// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using ImGuiNET;

internal class EmulatorGameWindow(NesConsole console) : IRenderWindow
{
    private readonly NesConsole _console = console;

    public void Update(double deltaTimeSeconds)
    {
        // Run one frame of emulation
        for (int i = 0; i < Ppu.Scanlines; i += 1)
        {
            _console.StepScanline();
        }
    }

    public void Render(double deltaTimeSeconds)
    {
        ImGui.ShowAboutWindow();
    }
}
