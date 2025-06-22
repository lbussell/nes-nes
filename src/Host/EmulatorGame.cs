// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NesNes.Core;
using NesNes.Host.Web;

namespace NesNes.Host;

internal class EmulatorGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly CartridgeData _cartridge;
    private readonly NesConsole _console;
    private readonly DebuggerWebApp _debuggerWebApp;

    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private bool _isPaused = false;

    // This is the raw display data that the PPU renders to. Whenever this
    // class decides to render the display, it copies this buffer to the
    // _display texture. This could happen whenever - every pixel, scanline, or
    // frame.
    private Color[] _frameBuffer = new Color[Ppu.DisplayWidth * Ppu.DisplayHeight];

    // This is the texture that the display will be rendered to.
    private RenderTarget2D? _display;

    // Holds the destination for rendering the display to the viewport. The
    // game screen will be scaled to fit whatever size this rectangle is.
    private Rectangle _renderDestination;

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

        // Debugger stuff
        _debuggerWebApp = new DebuggerWebApp(
            new DebuggerControls
            {
                Pause = () => _isPaused = !_isPaused,
                GetRegisters = () => _console.Cpu.Registers,
            }
        );
    }

    protected override void Initialize()
    {
        _display = new RenderTarget2D(
            _graphics.GraphicsDevice,
            Ppu.DisplayWidth,
            Ppu.DisplayHeight
        );
        UpdateRenderDestination();

        _console.InsertCartridge(_cartridge);

        _debuggerWebApp.Start();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Fonts/consolas");
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
            if (!_isPaused)
            {
                _console.StepScanline();
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_spriteBatch is null || _display is null)
        {
            return;
        }

        // Render all game visuals to the display.
        GraphicsDevice.SetRenderTarget(_display);

        _display.SetData(_frameBuffer);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.DrawString(
            _font,
            _console.CpuCycles.ToString(),
            new Vector2(0, 0),
            Color.White
        );
        _spriteBatch.End();

        // Stop rendering to the display target. Start rendering to the
        // viewport instead.
        GraphicsDevice.SetRenderTarget(null);

        // TODO: Add different options for texture filtering. PointClamp is
        // just linear filtering.
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Only draw the display to the viewport, but using the render
        // destination rectangle to scale the display.
        _spriteBatch.Draw(_display, _renderDestination, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawPixel(ushort x, ushort y, byte r, byte g, byte b)
    {
        // Convert the pixel coordinates to a 1D index in the frame buffer.
        int index = y * Ppu.DisplayWidth + x;

        if (index < 0 || index >= _frameBuffer.Length)
        {
            // If the index is out of bounds, we ignore it.
            return;
        }

        _frameBuffer[index] = new Color(r, g, b);
    }

    /// <summary>
    /// This method is called whenever the window is resized by the user.
    /// </summary>
    private void OnClientSizeChanged(object? _, EventArgs __)
    {
        // Ensure that we don't get any weird divide by zero errors.
        if (_display is not null && Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0)
        {
            UpdateRenderDestination();
        }
    }

    /// <summary>
    /// Updates the render destination rectangle based on the current viewport
    /// size. This sets the render destination's size to be as large as possible
    /// in the viewport.
    /// </summary>
    private void UpdateRenderDestination()
    {
        if (_display is null)
        {
            return;
        }

        Point viewportSize = GraphicsDevice.Viewport.Bounds.Size;

        float scaleY = viewportSize.Y / (float)_display.Height;

        // Uncomment to stretch to fill width of viewport.
        // TODO: Add option to stretch instead of maintaining aspect ratio.
        // float scaleX = viewportSize.X / (float)_display.Width;

        // Keep aspect ratio
        float scaleX = scaleY;

        _renderDestination.Width = (int)(_display.Width * scaleX);
        _renderDestination.Height = (int)(_display.Height * scaleY);

        _renderDestination.X = (viewportSize.X - _renderDestination.Width) / 2;
        _renderDestination.Y = (viewportSize.Y - _renderDestination.Height) / 2;
    }
}
