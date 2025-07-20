// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

class WindowManager : IDisposable
{
    private readonly IWindow _window;
    private readonly ServiceCollection _services = new();

    public WindowManager(Vector2D<int> size, string title = "New Window")
    {
        var windowOptions = new WindowOptions(
            isVisible: true,
            position: new Vector2D<int>(50, 50),
            size: size,
            framesPerSecond: 0.0,
            updatesPerSecond: 0.0,
            api: GraphicsAPI.Default,
            title: title,
            windowState: WindowState.Normal,
            windowBorder: WindowBorder.Fixed,
            isVSync: true,
            shouldSwapAutomatically: true,
            videoMode: VideoMode.Default
        );

        _window = Window.Create(windowOptions);
        _window.Load += OnLoad;
    }

    public void Run() => _window.Run();

    public void Dispose() => _window.Dispose();

    private void OnLoad()
    {
        _services.AddSingleton(_window);

        GL glContext = _window.CreateOpenGL();
        _services.AddSingleton(glContext);

        IInputContext inputContext = _window.CreateInput();
        _services.AddSingleton(inputContext);

        _services.AddSingleton(serviceProvider => new ImGuiController(
            serviceProvider.GetRequiredService<GL>(),
            serviceProvider.GetRequiredService<IWindow>(),
            serviceProvider.GetRequiredService<IInputContext>()
        ));
        _services.AddSingleton<GameWindow>();

        var serviceProvider = _services.BuildServiceProvider();
        var gameWindow = serviceProvider.GetRequiredService<GameWindow>();

        _window.Render += gameWindow.Render;
        _window.Closing += gameWindow.OnClose;
        _window.Update += gameWindow.Update;
        _window.FramebufferResize += gameWindow.OnFramebufferResize;
    }
}
