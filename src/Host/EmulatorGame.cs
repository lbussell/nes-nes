// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NesNes.Core;
using NesNes.Host.UI;
using Color = Microsoft.Xna.Framework.Color;
using Myra;
using Myra.Graphics2D.UI;

namespace NesNes.Host;

internal class EmulatorGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly CartridgeData _cartridge;
    private readonly NesConsole _console;
    private Desktop? _myraDesktop;

    private ScalableBufferedDisplay? _display;
    private ScalableBufferedDisplay? _nameTablesDisplay;
    private ScalableBufferedDisplay? _patternTablesDisplay;

    MenuItem? _startButton;
    MenuItem? _pauseButton;
    MenuItem? _resetButton;
    MenuItem? _stepInstructionButton;
    MenuItem? _stepPixelButton;
    MenuItem? _stepScanlineButton;
    MenuItem? _stepFrameButton;
    ListView _cpuLogView;

    private bool _emulationIsRunning;

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

    protected override void LoadContent()
    {
        MyraEnvironment.Game = this;
        _myraDesktop = new Desktop
        {
            Root = new DebuggerUI()
        };

        var debuggerMenu = _myraDesktop.Root.FindChildById<Menu>("emulation");
        _startButton = debuggerMenu.FindMenuItemById("start");
        _startButton.Enabled = !_emulationIsRunning;
        _startButton.Selected += (_, __) => Start();

        _pauseButton = debuggerMenu.FindMenuItemById("pause");
        _pauseButton.Enabled = _emulationIsRunning;
        _pauseButton.Selected += (_, __) => Pause();

        _resetButton = debuggerMenu.FindMenuItemById("reset");
        _resetButton.Enabled = true;
        _resetButton.Selected += (_, __) => Reset();

        var stepMenu = _myraDesktop.Root.FindChildById<Menu>("stepBy");
        _stepInstructionButton = stepMenu.FindMenuItemById("stepInstruction");
        _stepInstructionButton.Enabled = !_emulationIsRunning;

        _stepPixelButton = stepMenu.FindMenuItemById("stepPixel");
        _stepPixelButton.Enabled = !_emulationIsRunning;

        _stepScanlineButton = stepMenu.FindMenuItemById("stepScanline");
        _stepScanlineButton.Enabled = !_emulationIsRunning;

        _stepFrameButton = stepMenu.FindMenuItemById("stepFrame");
        _stepFrameButton.Enabled = !_emulationIsRunning;

        base.LoadContent();
    }

    /// <summary>
    /// Start the emulation, if it is currently paused.
    /// </summary>
    private void Start()
    {
        _emulationIsRunning = true;
        UpdateButtonStatus();
    }

    /// <summary>
    /// Pauses emulation if it is currently running.
    /// </summary>
    private void Pause()
    {
        _emulationIsRunning = false;
        UpdateButtonStatus();
    }

    /// <summary>
    /// Resets the game console to its initial state and restarts the game.
    /// This should not change whether or not the emulation is currently paused
    /// or running.
    /// </summary>
    private void Reset()
    {
        _console.Reset();
    }

    /// <summary>
    /// Update the enabled/disabled status of all debugger control buttons
    /// based on whether the emulation is currently running.
    /// </summary>
    private void UpdateButtonStatus()
    {
        _pauseButton?.Enabled = _emulationIsRunning;

        _startButton?.Enabled = !_emulationIsRunning;
        _stepInstructionButton?.Enabled = !_emulationIsRunning;
        _stepPixelButton?.Enabled = !_emulationIsRunning;
        _stepScanlineButton?.Enabled = !_emulationIsRunning;
        _stepFrameButton?.Enabled = !_emulationIsRunning;
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
        _display = new ScalableBufferedDisplay(
            _graphics.GraphicsDevice,
            Ppu.CyclesPerScanline,
            Ppu.Scanlines
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

    /// <inheritdoc/>
    protected override void Update(GameTime gameTime)
    {
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
        {
            Exit();
        }

        if (_emulationIsRunning)
        {
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
        }

        base.Update(gameTime);
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime)
    {
        if (_console.HasCartridge)
        {
            _display?.Render();
            _patternTablesDisplay?.Render();
            _nameTablesDisplay?.Render();
        }

        _myraDesktop?.Render();

        base.Draw(gameTime);
    }

    private void DrawPixel(ushort x, ushort y, byte r, byte g, byte b)
    {
        _display?.SetPixel(x, y, new Color(r, g, b));
    }

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
    /// This method is called whenever the window is resized by the user.
    /// </summary>
    private void OnClientSizeChanged(object? _, EventArgs __) => UpdateRenderScale();

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

            // Add space for Myra UI for now.
            _graphics.PreferredBackBufferHeight =
                _display.RenderHeight + _patternTablesDisplay.RenderHeight + 4 + 2 * Margin + 200;
            _graphics.PreferredBackBufferWidth =
                _display.RenderWidth + _nameTablesDisplay.RenderWidth + 4 + 2 * Margin;
            _graphics.ApplyChanges();
        }
    }
}
