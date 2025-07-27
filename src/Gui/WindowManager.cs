// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

class WindowManager
{
    private readonly IWindow _window;
    private readonly GameWindowFactory _gameWindowFactory;

    private readonly ServiceCollection _services = new();

    public WindowManager(
        GameWindowFactory gameWindowFactory,
        int scale = 1,
        string title = "New Window"
    )
    {
        _gameWindowFactory = gameWindowFactory;

        // Needed for Native AOT. Usually Silk.NET does this automatically with
        // reflection, but that doesn't work with Native AOT, so we have to
        // do it manually.
        GlfwWindowing.RegisterPlatform();
        GlfwInput.RegisterPlatform();

        var windowOptions = new WindowOptions(
            isVisible: true,
            position: new Vector2D<int>(50, 50),
            size: scale * gameWindowFactory.DisplaySize,
            framesPerSecond: 60.0,
            updatesPerSecond: 60.0,
            api: GraphicsAPI.Default,
            title: title,
            windowState: WindowState.Normal,
            windowBorder: WindowBorder.Resizable,
            isVSync: true,
            shouldSwapAutomatically: true,
            videoMode: VideoMode.Default
        );

        _window = Window.Create(windowOptions);
        _window.Load += OnLoad;
    }

    public void Run()
    {
        _window.Run();
        _window.Dispose();
    }

    private void OnLoad()
    {
        _services.AddSingleton(_window);

        GL glContext = _window.CreateOpenGL();
        _services.AddSingleton(glContext);

        IInputContext inputContext = _window.CreateInput();
        _services.AddSingleton(inputContext);

        _services.AddSingleton(serviceProvider =>
        {
            var imGuiController = new ImGuiController(
                serviceProvider.GetRequiredService<GL>(),
                serviceProvider.GetRequiredService<IWindow>(),
                serviceProvider.GetRequiredService<IInputContext>()
            );

            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            return imGuiController;
        });

        var serviceProvider = _services.BuildServiceProvider();
        IGameWindow window = _gameWindowFactory.CreateGameWindow(
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
