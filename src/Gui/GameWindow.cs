// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using NesNes.Gui;

interface IGameWindow
{
    void OnClose();
    void OnFramebufferResize(Vector2D<int> newSize);
    void Render(double deltaTimeSeconds);
    void Update(double deltaTimeSeconds);
}

class GameWindow : IGameWindow
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

    private static readonly Color s_clearColor = Color.CornflowerBlue;

    public GameWindow(
        GL openGl,
        IInputContext inputContext,
        ImGuiController imGuiController,
        Vector2D<int> internalSize
    )
    {
        _openGl = openGl;
        _inputContext = inputContext;
        _imGuiController = imGuiController;
        _internalSize = internalSize;

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
    }

    public unsafe void Render(double deltaTimeSeconds)
    {
        // Do any necessary updates
        // _imGuiController.Update((float)deltaTimeSeconds);

        // This is where you'll do any rendering beneath the ImGui context
        _openGl.Clear(ClearBufferMask.ColorBufferBit);
        _vertexArrayObject.Bind();
        _shader.Use();
        _texture.Bind();
        _texture.UpdateTextureData();
        _openGl.DrawElements(
            mode: PrimitiveType.Triangles,
            count: (uint)s_indices.Length,
            type: DrawElementsType.UnsignedInt,
            indices: (void*)0);

        // Do all ImGui rendering
        // ImGui.ShowUserGuide();
        // _imGuiController.Render();
    }

    public void OnClose()
    {
    }

    public void OnFramebufferResize(Vector2D<int> newSize) => throw new NotImplementedException();

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
