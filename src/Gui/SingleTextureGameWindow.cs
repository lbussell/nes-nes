// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using NesNes.Gui;
using NesNes.Core;
using ImGuiNET;

internal interface IGameWindow : IRenderWindow
{
    void OnClose();
    void OnFramebufferResize(Vector2D<int> newSize);
}

internal interface IRenderWindow
{
    void Render(double deltaTimeSeconds);

    /// <summary>
    /// Happens before <see cref="Render"/>.
    /// </summary>
    /// <param name="deltaTimeSeconds">
    /// Time in seconds since the last time this method was called.
    /// </param>
    void Update(double deltaTimeSeconds);
}

internal class EmulatorGameWindow(NesConsole console) : IRenderWindow
{
    private readonly NesConsole _console = console;

    public void Update(double deltaTimeSeconds)
    {
        // Run one frame of emulation
        for (int i = 0; i < Ppu.Scanlines; i += 1)
        {
            _console.StepScanline();
        }
    }

    public void Render(double deltaTimeSeconds)
    {
        ImGui.ShowAboutWindow();
    }
}

internal static class ConsoleExtensions
{
    private static readonly Vector2D<int> s_displaySize = new(Ppu.DisplayWidth, Ppu.DisplayHeight);

    public static Vector2D<int> GetDisplaySize(this NesConsole console) => s_displaySize;
}

internal class GameWindowFactory(
    NesConsole console
)
{
    private readonly NesConsole _console = console;

    public IGameWindow CreateGameWindow(
        GL openGl,
        IInputContext inputContext,
        ImGuiController imGuiController
    )
    {
        var emulatorGameWindow = new EmulatorGameWindow(_console);

        var gameWindow = new SingleTextureGameWindow(
            openGl,
            inputContext,
            imGuiController,
            _console.GetDisplaySize(),
            emulatorGameWindow
        );

        _console.Ppu.RenderPixelCallback = (x, y, r, g, b) => gameWindow.SetPixel(x, y, r, g, b);

        return gameWindow;
    }

    public Vector2D<int> DisplaySize => _console.GetDisplaySize();
}

/// <summary>
/// <see cref="SingleTextureGameWindow"/> manages all of the graphics for the running game.
/// </summary>
internal class SingleTextureGameWindow : IGameWindow
{
    private readonly GL _openGl;
    private readonly IInputContext _inputContext;
    private readonly ImGuiController _imGuiController;
    private readonly NesNes.Gui.Shader _shader;
    private readonly NesNes.Gui.Texture _texture;
    private readonly BufferObject<float> _vertexBuffer;
    private readonly BufferObject<uint> _elementBuffer;
    private readonly VertexArrayObject<float, uint> _vertexArrayObject;
    private readonly Vector2D<int> _internalSize;
    private readonly IRenderWindow[] _subWindows;

    private static readonly System.Drawing.Color s_clearColor = System.Drawing.Color.CornflowerBlue;

    public SingleTextureGameWindow(
        GL openGl,
        IInputContext inputContext,
        ImGuiController imGuiController,
        Vector2D<int> internalSize,
        params IRenderWindow[] subWindows
    )
    {
        _openGl = openGl;
        _inputContext = inputContext;
        _imGuiController = imGuiController;
        _internalSize = internalSize;
        _subWindows = subWindows;

        _elementBuffer = new BufferObject<uint>(_openGl, s_indices, BufferTargetARB.ElementArrayBuffer);
        _vertexBuffer = new BufferObject<float>(_openGl, s_vertices, BufferTargetARB.ArrayBuffer);
        _vertexArrayObject = new VertexArrayObject<float, uint>(_openGl, _vertexBuffer, _elementBuffer);

        // Set up vertex attributes
        // Position attribute (location = 0): 3 floats starting at offset 0
        _vertexArrayObject.VertexAttributePointer(
            index: 0,
            count: 3,
            type: VertexAttribPointerType.Float,
            vertexSize: 5,
            offset: 0);
        // Texture coordinate attribute (location = 1): 2 floats starting at offset 3
        _vertexArrayObject.VertexAttributePointer(
            index: 1,
            count: 2,
            type: VertexAttribPointerType.Float,
            vertexSize: 5,
            offset: 3);

        _shader = new NesNes.Gui.Shader(_openGl, VertexShaderCode, FragmentShaderCode);
        _texture = new NesNes.Gui.Texture(_openGl, _internalSize);

        _shader.SetUniform("uTexture", 0);

        _openGl.ClearColor(s_clearColor);
    }

    public void Update(double deltaTimeSeconds)
    {
        foreach (var window in _subWindows)
        {
            window.Update(deltaTimeSeconds);
        }
    }

    public unsafe void Render(double deltaTimeSeconds)
    {
        // Do any necessary updates
        _imGuiController.Update((float)deltaTimeSeconds);

        // This is where you'll do any rendering beneath the ImGui context
        _openGl.Clear(ClearBufferMask.ColorBufferBit);
        _vertexArrayObject.Bind();
        _shader.Use();
        _texture.UpdateTextureData();
        _openGl.DrawElements(
            mode: PrimitiveType.Triangles,
            count: (uint)s_indices.Length,
            type: DrawElementsType.UnsignedInt,
            indices: (void*)0);

        foreach (var window in _subWindows)
        {
            window.Render(deltaTimeSeconds);
        }

        // Do all ImGui rendering
        _imGuiController.Render();
    }

    public void OnClose()
    {
    }

    public void OnFramebufferResize(Vector2D<int> newSize) => throw new NotImplementedException();

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 0xFF) =>
        _texture.SetPixel(x, y, r, g, b, a);

    private static readonly float[] s_vertices =
    [
        // aPosition------   aTexCoords
         1.0f,  1.0f, 0.0f,  1.0f, 1.0f,
         1.0f, -1.0f, 0.0f,  1.0f, 0.0f,
        -1.0f, -1.0f, 0.0f,  0.0f, 0.0f,
        -1.0f,  1.0f, 0.0f,  0.0f, 1.0f
    ];

    private static readonly uint[] s_indices =
    [
        0, 1, 3, // first triangle (top-right, bottom-right, top-left)
        1, 2, 3  // second triangle (bottom-right, bottom-left, top-left)
    ];

    private const string VertexShaderCode =
        """
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        // On top of our aPosition attribute, we now create an aTexCoords attribute for our texture coordinates.
        layout (location = 1) in vec2 aTexCoords;

        // Likewise, we also assign an out attribute to go into the fragment shader.
        out vec2 frag_texCoords;

        void main()
        {
            gl_Position = vec4(aPosition, 1.0);

            // This basic vertex shader does no additional processing of texture coordinates, so we can pass them
            // straight to the fragment shader.
            frag_texCoords = aTexCoords;
        }
        """;

    private const string FragmentShaderCode =
        """
        #version 330 core

        // This in attribute corresponds to the out attribute we defined in the vertex shader.
        in vec2 frag_texCoords;

        out vec4 out_color;

        // Now we define a uniform value!
        // A uniform in OpenGL is a value that can be changed outside of the shader by modifying its value.
        // A sampler2D contains both a texture and information on how to sample it.
        // Sampling a texture is basically calculating the color of a pixel on a texture at any given point.
        uniform sampler2D uTexture;

        void main()
        {
            // We use GLSL's texture function to sample from the texture at the given input texture coordinates.
            out_color = texture(uTexture, frag_texCoords);
        }
        """;
}
