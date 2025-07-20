// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

class WindowManager<TWindow> : IDisposable where TWindow : IGameWindow
{
    private readonly IWindow _window;
    private readonly ServiceCollection _services = new();
    private readonly Func<GL, IInputContext, ImGuiController, TWindow> _createWindow;

    public WindowManager(
        Vector2D<int> size,
        Func<GL, IInputContext, ImGuiController, TWindow> createWindow,
        string title = "New Window"
    )
    {
        _createWindow = createWindow;

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

        var serviceProvider = _services.BuildServiceProvider();
        TWindow window = _createWindow(
            serviceProvider.GetRequiredService<GL>(),
            serviceProvider.GetRequiredService<IInputContext>(),
            serviceProvider.GetRequiredService<ImGuiController>()
        );

        _window.Render += window.Render;
        _window.Closing += window.OnClose;
        _window.Update += window.Update;
        _window.FramebufferResize += window.OnFramebufferResize;
    }
}
