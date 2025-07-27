// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using NesNes.Core;

internal class GameWindowFactory(NesConsole console)
{
    private readonly NesConsole _emulatorCore = console;

    public IGameWindow CreateGameWindow(
        GL openGl,
        IInputContext inputContext,
        ImGuiController imGuiController
    )
    {
        var emulator = new Emulator(_emulatorCore);

        var gameWindow = new ImGuiGameWindow(
            openGl,
            inputContext,
            imGuiController,
            _emulatorCore.GetDisplaySize(),
            game: emulator
        );

        _emulatorCore.Ppu.RenderPixelCallback = (x, y, r, g, b) => gameWindow.SetPixel(x, y, r, g, b);

        return gameWindow;
    }

    public Vector2D<int> DisplaySize => _emulatorCore.GetDisplaySize();
}
