// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using System.Drawing;

namespace NesNes.Gui;

internal sealed class GameWindow : IDisposable
{
    private const string WindowName = "nesnes";
    private static readonly Vector2D<int> s_windowSize = new(256, 240);
    private static readonly Vector2D<int> s_displaySize = s_windowSize;

    private static readonly WindowOptions s_windowOptions = new(
        isVisible: true,
        position: new Vector2D<int>(50, 50),
        size: new Vector2D<int>(256, 240),
        framesPerSecond: 0.0,
        updatesPerSecond: 0.0,
        api: GraphicsAPI.Default,
        title: WindowName,
        windowState: WindowState.Normal,
        windowBorder: WindowBorder.Fixed,
        isVSync: true,
        shouldSwapAutomatically: true,
        videoMode: VideoMode.Default
    );

    private static readonly Color s_backgroundColor = Color.FromArgb(
        alpha: 255,
        red: (int)(.45f * 255),
        green: (int)(.55f * 255),
        blue: (int)(.60f * 255)
    );

    private readonly IWindow _window;
    private GL? _openGl;
    private IInputContext? _inputContext;
    private ImGuiController? _imGuiController;

    public GameWindow(IWindow window)
    {
        _window = window;
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Update += OnUpdate;
        _window.FramebufferResize += OnFramebufferResize;
    }

    public static GameWindow Create()
    {
        var window = Window.Create(s_windowOptions);
        return new GameWindow(window);
    }

    public void Run() => _window.Run();

    public void Dispose() => _window.Dispose();

    private void OnLoad()
    {
        _openGl = _window.CreateOpenGL();

        _inputContext = _window.CreateInput();
        for (int i = 0; i < _inputContext.Keyboards.Count; i++)
        {
            _inputContext.Keyboards[i].KeyDown += OnKeyDown;
        }

        _imGuiController = new ImGuiController(_openGl, _window, _inputContext);
    }

    private static void OnRender(double obj)
    {
        //Here all rendering should be done.
    }

    private static void OnUpdate(double obj)
    {
        //Here all updates to the program should be done.
    }

    private static void OnFramebufferResize(Vector2D<int> newSize)
    {
        //Update aspect ratios, clipping regions, viewports, etc.
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        //Check to close the window on escape.
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }
}
