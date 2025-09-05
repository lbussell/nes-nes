// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using NesNes.Gui.Rendering;
using Texture = NesNes.Gui.Rendering.Texture;
using Silk.NET.Windowing;

namespace NesNes.Gui;

/// <summary>
/// <see cref="ImGuiGameWindow"/> manages all of the graphics for the running game.
/// </summary>
internal class ImGuiGameWindow : IGameWindow
{
    private readonly GL _openGl;
    private readonly IWindow _window;
    private readonly IInputContext _inputContext;
    private readonly ImGuiController _imGuiController;
    private readonly Texture _renderTexture;
    private readonly Vector2D<int> _internalSize;
    private readonly IGame _game;

    private static readonly System.Drawing.Color s_clearColor = System.Drawing.Color.CornflowerBlue;

    public ImGuiGameWindow(
        GL openGl,
        IWindow window,
        IInputContext inputContext,
        ImGuiController imGuiController,
        Vector2D<int> internalSize,
        IGame game
    )
    {
        _openGl = openGl;
        _window = window;
        _inputContext = inputContext;
        _imGuiController = imGuiController;
        _internalSize = internalSize;
        _game = game;

        _renderTexture = new Texture(_openGl, _internalSize);

        _openGl.ClearColor(s_clearColor);
    }

    public void Update(double deltaTimeSeconds)
    {
        _game.Update(deltaTimeSeconds);
    }

    public unsafe void Render(double deltaTimeSeconds)
    {
        // Do any necessary updates
        _imGuiController.Update((float)deltaTimeSeconds);

        _openGl.Viewport(_window.FramebufferSize);

        // This is where you'll do any rendering beneath the ImGui context
        _openGl.Clear(ClearBufferMask.ColorBufferBit);
        _renderTexture.UpdateTextureData();

        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport());

        ImGui.Begin("Game", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGuiHelper.RenderTextureWithIntegerScaling(_renderTexture);
        ImGui.End();

        _game.Render(deltaTimeSeconds);

        // Do all ImGui rendering
        _imGuiController.Render();
    }

    public void OnFramebufferResize(Vector2D<int> newSize)
    {
        _openGl.Viewport(newSize);
    }

    public void OnClose() { }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 0xFF) =>
        _renderTexture.SetPixel(x, y, r, g, b, a);
}
