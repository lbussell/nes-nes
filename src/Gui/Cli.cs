// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using NesNes.Core;
using NesNes.Gui.Rendering;
using static System.Console;

namespace NesNes.Gui;

internal sealed class Cli
{
    /// <summary>
    /// Starts the emulator with the specified ROM file.
    /// </summary>
    /// <param name="rom">Path to NES ROM file.</param>
    [Command("")]
    public void Start(string rom)
    {
        var romFileStream = File.OpenRead(rom);
        var cartridge = new CartridgeData(romFileStream);
        var romFileName = Path.GetFileName(rom);

        WriteLine(
            $"""
            Loaded ROM: {romFileName}
            {cartridge}
            """
        );

        var console = new NesConsole();
        console.InsertCartridge(cartridge);

        var gameWindowFactory = new GameWindowFactory(console);
        var window = new WindowManager(gameWindowFactory, title: romFileName);

        window.Run();
    }
}
