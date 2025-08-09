// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using ImGuiNET;
using NesNes.Gui.Views;

namespace NesNes.Gui;

internal class Emulator : IGame
{
    private readonly NesConsole _console;
    private readonly IClosableWindow[] _windows;
    private bool _isPaused = false;

    public Emulator(NesConsole console, PatternTableViewer patternTableViewer)
    {
        _console = console;

        var debuggerControls = new DebuggerControlsWindow
        {
            OnTogglePause = OnTogglePause,
            OnStepScanline = OnStepScanline,
            OnStepFrame = OnStepFrame,
            OnReset = OnReset
        };

        _windows =
        [
            debuggerControls,
            new CartridgeInfo(_console.Cartridge!),
            new CpuStateWindow(_console),
            patternTableViewer,
            new OamDataWindow(_console),
            new ImGuiMetrics(),
        ];
    }

    public void Update(double deltaTimeSeconds)
    {
        if (_isPaused)
        {
            return;
        }

        // Run one frame of emulation
        OnStepFrame();
    }

    private void OnTogglePause()
    {
        _isPaused = !_isPaused;
    }

    private void OnStepScanline() => _console.StepScanline();

    private void OnStepFrame() => _console.StepFrame();

    private void OnReset()
    {
        _console.Reset();
    }

    public void Render(double deltaTimeSeconds)
    {
        RenderMainMenuBar(_windows);

        foreach (var window in _windows)
        {
            window.Render(deltaTimeSeconds);
        }
    }

    private static void RenderMainMenuBar(IClosableWindow[] _windows)
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

    private static void RenderWindowViewMenuItem(IClosableWindow window) =>
        ImGui.MenuItem(window.Name, shortcut: null, ref window.Open);
}
