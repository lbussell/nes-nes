// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace NesNes.Host;

internal sealed class ScalableBufferedDisplay(GraphicsDevice graphicsDevice, int width, int height)
    : IRenderTarget,
        IScalable,
        IBuffered
{
    private readonly RenderTarget2D _texture = new(graphicsDevice, width, height);
    private readonly FrameBuffer _frameBuffer = new FrameBuffer(width, height);
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly SpriteBatch _spriteBatch = new(graphicsDevice);
    private Rectangle _renderLocation = new Rectangle(x: 0, y: 0, width, height);

    public int X
    {
        get => _renderLocation.X;
        set => _renderLocation.X = value;
    }

    public int Y
    {
        get => _renderLocation.Y;
        set => _renderLocation.Y = value;
    }

    public int RenderWidth
    {
        get => _renderLocation.Width;
        set => _renderLocation.Width = value;
    }

    public int RenderHeight
    {
        get => _renderLocation.Height;
        set => _renderLocation.Height = value;
    }

    public int Width => _frameBuffer.Width;

    public int Height => _frameBuffer.Height;

    public void Render()
    {
        // Draw the framebuffer to the target render texture
        _graphicsDevice.SetRenderTarget(_texture);
        _frameBuffer.CopyTo(_texture);
        _graphicsDevice.SetRenderTarget(null);

        // Draw the texture to the screen at the render location
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_texture, _renderLocation, Color.White);
        _spriteBatch.End();
    }

    public void SetPixel(int x, int y, Color color)
    {
        _frameBuffer.SetPixel(x, y, color);
    }
}
