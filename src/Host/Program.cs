// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using NesNes.Core;
using static System.Console;

var app = ConsoleApp.Create();
app.Add<Emulator>();
await app.RunAsync(args);

class Emulator
{
    /// <summary>
    /// Starts the emulator with the specified ROM file.
    /// </summary>
    /// <param name="rom">Path to NES ROM file.</param>
    [Command("")]
    public async Task Start(string rom)
    {
        var romData = await File.ReadAllBytesAsync(rom);
        var cartridge = new Cartridge(romData);
        WriteLine(
            $"""
            Loaded ROM: {Path.GetFileName(rom)}
            {cartridge}
            """
        );

        // using var game = new NesNes.Host.Game1();
        // game.Run();
    }
}
