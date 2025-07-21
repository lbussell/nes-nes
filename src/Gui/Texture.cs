// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace NesNes.Gui;

public class Texture : IDisposable
{
    private readonly GL _openGl;

    // OpenGL handle for the texture
    private readonly uint _handle;

    // Raw pixel data in RGBA format
    private readonly byte[] _pixelData;

    private Vector2D<int> _size;

    public Texture(GL openGl, Vector2D<int> size)
    {
        _openGl = openGl;
        _size = size;
        _handle = _openGl.GenTexture();

        Bind();
        _openGl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _openGl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _openGl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        _openGl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);

        // Allocate enough memory for RGBA format texture data
        _pixelData = new byte[_size.X * _size.Y * 4];
        Random.Shared.NextBytes(_pixelData);

        _openGl.TexImage2D(
            target: TextureTarget.Texture2D,
            level: 0,
            internalformat: InternalFormat.Rgba,
            width: (uint)_size.X,
            height: (uint)_size.Y,
            border: 0,
            format: PixelFormat.Rgba,
            type: PixelType.UnsignedByte,
            pixels: _pixelData.AsSpan()
        );
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 0xFF)
    {
        // Calculate the index in the pixel data array
        int index = (y * _size.X + x) * 4;

        // Set the pixel color
        _pixelData[index] = r;
        _pixelData[index + 1] = g;
        _pixelData[index + 2] = b;
        _pixelData[index + 3] = a;
    }

    public void UpdateTextureData()
    {
        Bind();
        _openGl.TexSubImage2D(
            target: TextureTarget.Texture2D,
            level: 0,
            xoffset: 0,
            yoffset: 0,
            width: (uint)_size.X,
            height: (uint)_size.Y,
            format: PixelFormat.Rgba,
            type: PixelType.UnsignedByte,
            pixels: _pixelData.AsSpan()
        );
    }

    public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
    {
        //When we bind a texture we can choose which textureslot we can bind it to.
        _openGl.ActiveTexture(textureSlot);
        _openGl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose()
    {
        //In order to dispose we need to delete the opengl handle for the texure.
        _openGl.DeleteTexture(_handle);
    }
}
