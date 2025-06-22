// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NesNes.Core;
using Color = Microsoft.Xna.Framework.Color;

namespace NesNes.Host;

internal class EmulatorGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly CartridgeData _cartridge;
    private readonly NesConsole _console;

    private ScalableBufferedDisplay? _display;

    public EmulatorGame(CartridgeData cartridge)
    {
        // MonoGame stuff
        Content.RootDirectory = "Content";

        // Display stuff
        _graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;

        // NesNes stuff
        _cartridge = cartridge;
        _console = NesConsole.Create(DrawPixel);
    }

    protected override void Initialize()
    {
        _display = new ScalableBufferedDisplay(
            _graphics.GraphicsDevice,
            Ppu.DisplayWidth,
            Ppu.DisplayHeight
        );
        UpdateRenderScale();

        _console.InsertCartridge(_cartridge);

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
        {
            Exit();
        }

        for (int i = 0; i < Ppu.Scanlines; i += 1)
        {
            _console.StepScanline();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _display?.Render();
        base.Draw(gameTime);
    }

    private void DrawPixel(ushort x, ushort y, byte r, byte g, byte b)
    {
        _display?.SetPixel(x, y, new Color(r, g, b));
    }

    /// <summary>
    /// This method is called whenever the window is resized by the user.
    /// </summary>
    private void OnClientSizeChanged(object? _, EventArgs __)
    {
        // Ensure that we don't get any weird divide by zero errors.
        if (_display is not null && Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0)
        {
            Point viewportSize = GraphicsDevice.Viewport.Bounds.Size;
            ((IScalable)_display).ScaleTo(viewportSize);
        }
    }

    private void UpdateRenderScale()
    {
        Point viewportSize = GraphicsDevice.Viewport.Bounds.Size;
        ((IScalable?)_display)?.ScaleTo(viewportSize);
    }
}
