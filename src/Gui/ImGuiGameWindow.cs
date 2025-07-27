// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using System.Numerics;
using Texture = NesNes.Gui.Texture;

/// <summary>
/// <see cref="ImGuiGameWindow"/> manages all of the graphics for the running game.
/// </summary>
internal class ImGuiGameWindow : IGameWindow
{
    private readonly GL _openGl;
    private readonly IInputContext _inputContext;
    private readonly ImGuiController _imGuiController;
    private readonly Texture _texture;
    private readonly Vector2D<int> _internalSize;
    private readonly IGame _game;

    private static readonly System.Drawing.Color s_clearColor = System.Drawing.Color.CornflowerBlue;

    public ImGuiGameWindow(
        GL openGl,
        IInputContext inputContext,
        ImGuiController imGuiController,
        Vector2D<int> internalSize,
        IGame game
    )
    {
        _openGl = openGl;
        _inputContext = inputContext;
        _imGuiController = imGuiController;
        _internalSize = internalSize;
        _game = game;

        _texture = new Texture(_openGl, _internalSize);

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

        // This is where you'll do any rendering beneath the ImGui context
        _openGl.Clear(ClearBufferMask.ColorBufferBit);
        _texture.UpdateTextureData();

        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport());

        ImGui.Begin("Game", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar);
        RenderTextureWithIntegerScaling(_texture);
        ImGui.End();

        _game.Render(deltaTimeSeconds);

        // Do all ImGui rendering
        _imGuiController.Render();
    }

    private static void RenderTextureWithIntegerScaling(Texture texture)
    {
        Vector2 availableSize = ImGui.GetContentRegionAvail();

        // Calculate the maximum integer scale factor that fits in the available space
        int scaleX = Math.Max(1, (int)(availableSize.X / texture.Size.X));
        int scaleY = Math.Max(1, (int)(availableSize.Y / texture.Size.Y));

        // Use the smaller scale factor to maintain aspect ratio
        int scale = Math.Min(scaleX, scaleY);
        var scaledDisplaySize = (Vector2)(scale * texture.Size);

        // Center the image in the available space
        Vector2 initialCursorPosition = ImGui.GetCursorPos();
        Vector2 centerOffset = (availableSize - scaledDisplaySize) * 0.5f;
        Vector2 newCursorPosition = initialCursorPosition + centerOffset;

        // Vector2 is backed by floats. We need to truncate the cursor position
        // as close to ints as possible to avoid subpixel rendering issues.
        newCursorPosition.X = MathF.Truncate(newCursorPosition.X);
        newCursorPosition.Y = MathF.Truncate(newCursorPosition.Y);

        ImGui.SetCursorPos(newCursorPosition);
        ImGui.Image(texture.Handle, scaledDisplaySize);
    }

    public void OnFramebufferResize(Vector2D<int> newSize)
    {
        _openGl.Viewport(newSize);
    }

    public void OnClose() { }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 0xFF) =>
        _texture.SetPixel(x, y, r, g, b, a);
}
