// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Xna.Framework;
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
    private ScalableBufferedDisplay? _nameTablesDisplay;
    private ScalableBufferedDisplay? _patternTablesDisplay;

    public EmulatorGame(CartridgeData cartridge)
    {
        // MonoGame stuff
        Content.RootDirectory = "Content";

        // Display stuff
        _graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        // For now, don't allow resizing
        Window.AllowUserResizing = false;
        Window.ClientSizeChanged += OnClientSizeChanged;

        // NesNes stuff
        _cartridge = cartridge;

        // When appropriate, the console calls renderPixelCallback to draw a
        // pixel to the screen. The console does not know or care how fast it's
        // running, nor does it know anything about what what we do with the
        // pixels. This class (EmulatorGame) handles all of the emulation
        // speed/synchronization, display buffering, etc.
        _console = NesConsole.Create(renderPixelCallback: DrawPixel);
    }

    protected override void Initialize()
    {
        _display = new ScalableBufferedDisplay(
            _graphics.GraphicsDevice,
            Ppu.DisplayWidth,
            Ppu.DisplayHeight
        );
        _patternTablesDisplay = new ScalableBufferedDisplay(
            _graphics.GraphicsDevice,
            2 * Ppu.PatternTableTilesWidth * Ppu.PatternSize,
            Ppu.PatternTableTilesHeight * Ppu.PatternSize
        );
        _nameTablesDisplay = new ScalableBufferedDisplay(
            _graphics.GraphicsDevice,
            32 * 2 * Ppu.PatternSize,
            30 * 2 * Ppu.PatternSize
        );

        UpdateRenderScale();

        _console.InsertCartridge(_cartridge);

        UpdatePatternTablesDisplay();

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

        // Draw one frame per update. By default updates are at 60hz. Later,
        // this should synchronize to audio.
        if (_display is not null && _console.HasCartridge)
        {
            for (int i = 0; i < Ppu.Scanlines; i += 1)
            {
                _console.StepScanline();
            }
        }

        UpdatePatternTablesDisplay();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // _display?.Clear(Color.Blue);
        // _leftPatternTable?.Clear(Color.BlueViolet);
        // _nameTablesDisplay?.Clear(Color.DarkGoldenrod);

        if (_console.HasCartridge)
        {
            _display?.Render();
            _patternTablesDisplay?.Render();
            _nameTablesDisplay?.Render();
        }

        base.Draw(gameTime);
    }

    private void DrawPixel(ushort x, ushort y, byte r, byte g, byte b)
    {
        _display?.SetPixel(x, y, new Color(r, g, b));
    }

    /// <summary>
    /// This method is called whenever the window is resized by the user.
    /// </summary>
    private void OnClientSizeChanged(object? _, EventArgs __) => UpdateRenderScale();

    private void UpdatePatternTablesDisplay()
    {
        if (_console.HasCartridge && _patternTablesDisplay is not null)
        {
            for (int row = 0; row < Ppu.PatternTableTilesHeight * Ppu.PatternSize; row += 1)
            {
                for (int col = 0; col < 2 * Ppu.PatternTableTilesWidth * Ppu.PatternSize; col += 1)
                {
                    var color = _console.Ppu.GetPatternTablePixel(row, col);
                    _patternTablesDisplay.SetPixel(col, row, new Color(color.R, color.G, color.B));
                }
            }
        }
    }

    /// <summary>
    /// Resize all screen elements as necessary based on the current viewport
    /// size and dimensions.
    /// </summary>
    private void UpdateRenderScale()
    {
        const int Margin = 8;

        if (
            _display is not null
            && _patternTablesDisplay is not null
            && _nameTablesDisplay is not null
        )
        {
            _display.SetScale(2);
            _patternTablesDisplay.SetScale(2);
            _nameTablesDisplay.SetScale(1);

            _display.X = Margin;
            _display.Y = Margin;

            _nameTablesDisplay.SetNextTo(_display, Side.Right, Align.Top, gap: 4);
            _patternTablesDisplay.SetNextTo(_nameTablesDisplay, Side.Bottom, Align.Right, gap: 4);

            _graphics.PreferredBackBufferHeight =
                _display.RenderHeight + _patternTablesDisplay.RenderHeight + 4 + 2 * Margin;
            _graphics.PreferredBackBufferWidth =
                _display.RenderWidth + _nameTablesDisplay.RenderWidth + 4 + 2 * Margin;
            _graphics.ApplyChanges();
        }
    }
}
