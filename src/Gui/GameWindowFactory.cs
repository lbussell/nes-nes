// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using NesNes.Core;

internal class GameWindowFactory(
    NesConsole console
)
{
    private readonly NesConsole _console = console;

    public IGameWindow CreateGameWindow(
        GL openGl,
        IInputContext inputContext,
        ImGuiController imGuiController
    )
    {
        var emulatorGameWindow = new EmulatorGameWindow(_console);

        var gameWindow = new SingleTextureGameWindow(
            openGl,
            inputContext,
            imGuiController,
            _console.GetDisplaySize(),
            emulatorGameWindow
        );

        _console.Ppu.RenderPixelCallback = (x, y, r, g, b) => gameWindow.SetPixel(x, y, r, g, b);

        return gameWindow;
    }

    public Vector2D<int> DisplaySize => _console.GetDisplaySize();
}
