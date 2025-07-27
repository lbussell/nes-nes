// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using ImGuiNET;

internal class Emulator : IGame
{
    private readonly NesConsole _console;
    private readonly ClosableWindow[] _windows;

    public Emulator(NesConsole console)
    {
        _console = console;
        _windows =
        [
            new ExampleWindow()
        ];
    }

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
        RenderMainMenuBar(_windows);

        foreach (var window in _windows)
        {
            window.Render(deltaTimeSeconds);
        }
    }

    private static void RenderMainMenuBar(ClosableWindow[] _windows)
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Window"))
            {
                foreach (var window in _windows)
                {
                    RenderWindowViewMenuItem(window);
                }

                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }

    private static void RenderWindowViewMenuItem(ClosableWindow window) =>
        ImGui.MenuItem(window.Name, shortcut: null, ref window.Open);
}
