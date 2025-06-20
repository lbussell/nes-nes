// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NesNes.Core;
using Console = NesNes.Core.Console;

namespace NesNes.Host;

internal class EmulatorGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Cartridge _cartridge;
    private readonly Console _console;
    private readonly StringBuilder _log = new();

    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;

    private int _cpuCycles = 0;

    public EmulatorGame(Cartridge cartridge)
    {
        // MonoGame stuff
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // NesNes stuff
        _cartridge = cartridge;
        _console = Console.Create(Trace);
    }

    protected override void Initialize()
    {
        _console.InsertCartridge(_cartridge);
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

        var elapsedCycles = _console.StepCpu();
        _cpuCycles += elapsedCycles;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here
        _spriteBatch?.Begin();

        _spriteBatch?.DrawString(_font, _cpuCycles.ToString(), new Vector2(10, 10), Color.White);

        _spriteBatch?.End();

        base.Draw(gameTime);
    }

    /// <summary>
    /// Prints the current CPU state.
    /// </summary>
    private void Trace(Cpu cpu, IMemory memory)
    {
        /**

        TODO: Trace CPU state. Must match the following format precisely:

        C000  4C F5 C5  JMP $C5F5                       A:00 X:00 Y:00 P:24 SP:FD PPU:  0, 21 CYC:7
        C5F5  A2 00     LDX #$00                        A:00 X:00 Y:00 P:24 SP:FD PPU:  0, 30 CYC:10
        C5F7  86 00     STX $00 = 00                    A:00 X:00 Y:00 P:26 SP:FD PPU:  0, 36 CYC:12
        C5F9  86 10     STX $10 = 00                    A:00 X:00 Y:00 P:26 SP:FD PPU:  0, 45 CYC:15
        C5FB  86 11     STX $11 = 00                    A:00 X:00 Y:00 P:26 SP:FD PPU:  0, 54 CYC:18
        ...

        */

        // var registers = cpu.Registers;

        // _log.AppendLine();
    }
}
