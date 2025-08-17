// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using NesNes.Core;
using NesNes.Gui.Views;

namespace NesNes.Gui.Rendering;

internal class EmulatorWindowFactory(NesConsole console)
{
    private readonly NesConsole _emulatorCore = console;

    private readonly Vector2D<int> _internalSize = new(PpuV2.NumCycles, PpuV2.NumScanlines);

    public IGameWindow CreateEmulatorWindow(
        GL openGl,
        IInputContext inputContext,
        ImGuiController imGuiController
    )
    {
        var patternTableViewer = new PatternTableViewer(
            openGl,
            _emulatorCore
        );

        var emulator = new Emulator(_emulatorCore, patternTableViewer);

        var emulatorWindow = new ImGuiGameWindow(
            openGl,
            inputContext,
            imGuiController,
            _internalSize,
            game: emulator
        );

        _emulatorCore.Ppu.OnRenderPixel =
            (x, y, r, g, b) => emulatorWindow.SetPixel(x, y, r, g, b);

        return emulatorWindow;
    }
}
