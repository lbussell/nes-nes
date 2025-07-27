// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using NesNes.Core;

namespace NesNes.Gui.Rendering;

internal class GameWindowFactory(NesConsole console)
{
    private readonly NesConsole _emulatorCore = console;

    private readonly Vector2D<int> _internalSize = new(256, 240);

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
            _internalSize,
            game: emulator
        );

        _emulatorCore.Ppu.RenderPixelCallback = (x, y, r, g, b) => gameWindow.SetPixel(x, y, r, g, b);

        return gameWindow;
    }
}
