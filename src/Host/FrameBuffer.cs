// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace NesNes.Host;

internal sealed class FrameBuffer : IRenderTarget
{
    private Color[] _buffer;

    public FrameBuffer(int width, int height)
    {
        _buffer = new Color[width * height];
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public void CopyTo(RenderTarget2D target)
    {
        target.SetData(_buffer);
    }

    public void SetPixel(int x, int y, Color color)
    {
        int index = y * Width + x;

        if (index < 0 || index >= _buffer.Length)
        {
            // If the index is out of bounds, we ignore it.
            return;
        }

        _buffer[index] = color;
    }

    internal void Clear(Color color)
    {
        for (int i = 0; i < _buffer.Length; i++)
        {
            _buffer[i] = color;
        }
    }
}
