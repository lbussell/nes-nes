// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using NesNes.Gui.Views;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Texture = NesNes.Gui.Rendering.Texture;
using ImGuiNET;
using NesNes.Gui.Rendering;

namespace NesNes.Gui;

internal sealed class GameWindow : IGameWindow
{
    public GameWindow(IWindow window, NesConsole console)
    {
        _window = window;
        _console = console;

        _gl = _window.CreateOpenGL();
        _input = _window.CreateInput();

        _imGui = new ImGuiController(_gl, _window, _input);
        var imGuiIO = ImGui.GetIO();
        imGuiIO.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        _internalSize = new Vector2D<int>(PpuV2.NumCycles, PpuV2.NumScanlines);
        _renderTexture = new Texture(_gl, _internalSize);

        _console.Ppu.OnRenderPixel = (x, y, r, g, b) => _renderTexture.SetPixel(x, y, r, g, b);

        var patternTableTexture = new PatternTableTexture(_gl, _console);

        _imGuiWindows =
        [
            // debuggerControls,
            new CartridgeInfo(_console.Cartridge!),
            new CpuStateWindow(_console)
            {
                OnReset = OnReset,
                OnStepFrame = OnStepFrame,
                OnStepScanline = OnStepScanline,
                OnStepInstruction = OnStepInstruction,
                OnTogglePause = OnTogglePause,
            },
            new PpuStateWindow(_console),
            new PatternTableViewer(_gl, patternTableTexture),
            new OamDataWindow(_console),
            new ImGuiMetrics(),
        ];
    }

    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _input;
    private readonly ImGuiController _imGui;
    private readonly NesConsole _console;

    private readonly IClosableWindow[] _imGuiWindows;

    private readonly Texture _renderTexture;
    private readonly Vector2D<int> _internalSize;

    private bool _isPaused;

    public void Load()
    {
        _gl.ClearColor(s_clearColor);
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

    public void Render(double deltaTimeSeconds)
    {
        // Do any necessary updates
        _imGui.Update((float)deltaTimeSeconds);

        // Do any rendering *beneath* the ImGui context
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _renderTexture.UpdateTextureData();

        // Do ImGui setup
        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport());

        // Render the game's renderTexture to the ImGui game window
        ImGui.Begin("Game", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGuiHelper.RenderTextureWithIntegerScaling(_renderTexture, out var _, out var _);
        ImGui.End();

        // Render menubar, which contains an item for each window.
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Window"))
            {
                foreach (var window in _imGuiWindows)
                {
                    RenderWindowViewMenuItem(window);
                }

                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }

        // Render windows (they only render themselves if open)
        foreach (var window in _imGuiWindows)
        {
            window.Render(deltaTimeSeconds);
        }

        // Finish by telling ImGui to render
        _imGui.Render();
    }

    public void FramebufferResize(Vector2D<int> newSize) => _gl.Viewport(newSize);

    private void OnTogglePause() => _isPaused = !_isPaused;
    private void OnStepInstruction() => _console.StepInstruction();
    private void OnStepScanline() => _console.StepScanline();
    private void OnStepFrame() => _console.StepFrame();
    private void OnReset() => _console.Reset();

    private static void RenderWindowViewMenuItem(IClosableWindow window) =>
        ImGui.MenuItem(window.Name, shortcut: null, ref window.Open);

    private static readonly System.Drawing.Color s_clearColor = System.Drawing.Color.CornflowerBlue;
}
