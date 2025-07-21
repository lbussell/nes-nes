// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using NesNes.Core;
using Silk.NET.Maths;
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
        var romData = File.ReadAllBytes(rom);
        var cartridge = CartridgeData.FromBytes(romData);

        WriteLine(
            $"""
            Loaded ROM: {Path.GetFileName(rom)}
            {cartridge}
            """
        );

        var console = new NesConsole();
        console.InsertCartridge(cartridge);

        var gameWindowFactory = new GameWindowFactory(console);
        using var window = new WindowManager(
            gameWindowFactory,
            scale: 3,
            "JANE"
        );

        window.Run();
        window.Dispose();
    }
}
