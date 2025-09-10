// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;
using Silk.NET.Input.Glfw;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

namespace NesNes.Gui.Rendering;

internal sealed class WindowHost
{
    private readonly IWindow _window;
    private readonly Func<IWindow, IGameWindow> _createWindow;

    public WindowHost(Func<IWindow, IGameWindow> createWindow, WindowOptions options)
    {
        GlfwWindowing.RegisterPlatform();
        GlfwInput.RegisterPlatform();

        _createWindow = createWindow;
        _window = Window.Create(options);
        _window.Load += Load;
    }

    public void Run()
    {
        _window.Run();
        _window.Dispose();
    }

    private void Load()
    {
        var gameWindow = _createWindow(_window);
        gameWindow.Load();
        _window.Update += gameWindow.Update;
        _window.Render += gameWindow.Render;
        _window.FramebufferResize += gameWindow.FramebufferResize;
    }
}
