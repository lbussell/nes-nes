// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Common;
using NesNes.Core;
using NesNes.Gui.Rendering;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Texture = NesNes.Gui.Rendering.Texture;

namespace NesNes.Gui.Views;

/// <summary>
/// Composes a single nametable (32x30 tiles) into a texture on the GPU by sampling from the
/// already-uploaded pattern table atlas. We avoid CPU-side per-pixel blits; instead we upload only
/// the nametable tile indices (960 bytes) each frame and let a fragment shader perform the tile
/// lookup.
/// </summary>
internal sealed class NameTableTexture : IImGuiRenderable, IDisposable
{
    // Simple full-screen quad (two triangles) in clip space. Each vertex is a vec2 position,
    // interleaved as [x,y].
    private static readonly float[] s_fullscreenQuadVertices = [-1f, -1f, 1f, -1f, 1f, 1f, -1f, 1f];
    private static readonly uint[] s_quadIndices = [0, 1, 2, 2, 3, 0];

    private readonly GL _gl;
    private readonly NesConsole _console;
    private readonly PatternTableTexture _patternTableAtlas;
    private readonly Texture _outputTexture;
    private readonly Texture _tileIndexTexture;
    private readonly LinkedShaderProgram _program;
    private readonly BufferObject<float> _vertexBuffer;
    private readonly BufferObject<uint> _indexBuffer;
    private readonly VertexArrayObject<float> _vao;
    private readonly uint _framebufferHandle;

    public NameTableTexture(GL gl, NesConsole console, PatternTableTexture patternTableTexture)
    {
        _gl = gl;
        _console = console;
        _patternTableAtlas = patternTableTexture;

        _outputTexture = new Texture(gl, new Vector2D<int>(64 * 8, 60 * 8));
        _tileIndexTexture = new Texture(gl, new Vector2D<int>(64, 60));

        _framebufferHandle = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferHandle);
        gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D,
            (uint)_outputTexture.Handle,
            0
        );

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // Build shaders
        var vertexShader = new Common.Shader(ShaderType.VertexShader, VertexShaderSource);
        var fragmentShader = new Common.Shader(ShaderType.FragmentShader, FragmentShaderSource);
        _program = new ShaderProgram(vertexShader, fragmentShader).Build(gl);

        _vertexBuffer = new BufferObject<float>(
            gl,
            BufferTargetARB.ArrayBuffer,
            s_fullscreenQuadVertices
        );

        _indexBuffer = new BufferObject<uint>(
            gl,
            BufferTargetARB.ElementArrayBuffer,
            s_quadIndices
        ); // retained (not strictly needed now)

        _vao = new VertexArrayObject<float>(gl, _vertexBuffer, _indexBuffer);
        _vao.VertexAttributePointer(
            index: 0,
            count: 2,
            type: VertexAttribPointerType.Float,
            vertexSize: 2,
            offset: 0
        );
        _vao.Unbind();

        // Ensure pattern atlas has data at least once (if viewer not opened yet)
        _patternTableAtlas.UpdateTextureData();
    }

    public nint Handle => _outputTexture.Handle;
    public Vector2D<int> Size => _outputTexture.Size;

    /// <summary>
    /// Uploads tile indices then renders the composed nametable into the output texture.
    /// </summary>
    public void RenderToTexture()
    {
        if (_console.Bus.Mapper is null)
        {
            return;
        }

        UploadTileIndices();
        Draw();
    }

    private void UploadTileIndices()
    {
        // Four nametables: $2000, $2400, $2800, $2C00
        Span<ushort> baseAddresses = [0x2000, 0x2400, 0x2800, 0x2C00];

        for (int table = 0; table < 4; table += 1)
        {
            int offsetX = (table % 2) * 32;
            int offsetY = (table / 2) * 30;

            var nameTableData = _console.Bus.Mapper!.PpuRead(baseAddresses[table], PpuConsts.NameTableSize);

            // First 960 bytes = 32*30 tile indices. Attribute table (last 64 bytes) ignored for now.
            for (int y = 0; y < 30; y += 1)
            {
                int rowOffset = y * 32;
                int destY = offsetY + y;
                for (int x = 0; x < 32; x += 1)
                {
                    int destX = offsetX + x;
                    byte index = nameTableData[rowOffset + x];
                    _tileIndexTexture.SetPixel(destX, destY, index, 0, 0, 0xFF);
                }
            }
        }

        _tileIndexTexture.UpdateTextureData();
    }

    private void Draw()
    {
        // Save existing viewport so we can restore after off-screen render
        Span<int> prevViewport = stackalloc int[4]; // x, y, width, height
        _gl.GetInteger(GLEnum.Viewport, prevViewport);

        // Configure state for off-screen render
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferHandle);
        _gl.Viewport(_outputTexture.Size);
        _gl.ClearColor(0f, 0f, 0f, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _gl.UseProgram(_program.Handle);

        // Set uniforms & bind textures
        int useSecondTable = _console.Ppu.BackgroundPatternTableAddress > 0 ? 1 : 0;
        _program.SetUniform("uUseSecondTable", useSecondTable);
        _program.SetUniform("uPatternAtlas", 0);
        _program.SetUniform("uTileIndices", 1);

        // Bind pattern atlas and tile index textures to units 0 and 1.
        // Bind pattern table atlas (pattern atlas texture handle exposed via IImGuiRenderable Handle)
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, (uint)_patternTableAtlas.Handle);
        _tileIndexTexture.Bind(TextureUnit.Texture1);

        _vao.Bind();
        _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        _vao.Unbind();

        // Unbind FBO and restore previous viewport so the rest of the UI isn't constrained
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        var restoreSize = new Vector2D<int>(prevViewport[2], prevViewport[3]);
        _gl.Viewport(restoreSize);
    }

    public void Dispose()
    {
        _vao.Dispose();
        _indexBuffer.Dispose();
        _vertexBuffer.Dispose();
        _program.Dispose();
        _gl.DeleteFramebuffer(_framebufferHandle);
        _tileIndexTexture.Dispose();
        _outputTexture.Dispose();
    }

    private const string VertexShaderSource =
        """
        #version 330 core
        layout(location=0) in vec2 aPosition;
        void main() {
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
        """;

    private const string FragmentShaderSource =
        """
        #version 330 core
        out vec4 FragColor;

        uniform sampler2D uPatternAtlas;
        uniform sampler2D uTileIndices;
        uniform int uUseSecondTable;

        // Pattern atlas is 16 tiles wide by 32 tiles tall (two 16x16 pattern tables stacked).
        void main() {
            ivec2 pixelCoord = ivec2(int(gl_FragCoord.x), int(gl_FragCoord.y));
            int tileX = pixelCoord.x / 8;
            int tileY = pixelCoord.y / 8;

            // Fetch tile index (stored in red channel 0..1).
            float raw = texelFetch(uTileIndices, ivec2(tileX, tileY), 0).r;
            int tileIndex = int(raw * 255.0 + 0.5);

            int patternIndex = tileIndex + (uUseSecondTable * 256);
            int patternX = patternIndex % 16;
            int patternY = patternIndex / 16;

            int localX = pixelCoord.x % 8;
            int localY = pixelCoord.y % 8;

            int atlasX = patternX * 8 + localX;
            int atlasY = patternY * 8 + localY;

            vec2 atlasSize = vec2(textureSize(uPatternAtlas, 0));
            vec2 uv = (vec2(atlasX, atlasY) + vec2(0.5)) / atlasSize;
            FragColor = texture(uPatternAtlas, uv);
        }
        """;
}
