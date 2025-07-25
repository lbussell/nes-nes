// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using NesNes.Core;
using NesNes.Host;
using static System.Console;

internal class EmulatorCli
{
    /// <summary>
    /// Starts the emulator with the specified ROM file.
    /// </summary>
    /// <param name="rom">Path to NES ROM file.</param>
    [Command("")]
    public async Task Start(string rom, string? logFile = null, int? logUntil = null)
    {
        var romData = await File.ReadAllBytesAsync(rom);
        var cartridge = new CartridgeData(romData);
        WriteLine(
            $"""
            Loaded ROM: {Path.GetFileName(rom)}
            {cartridge}
            """
        );

        using var game = new EmulatorGame(
            cartridge,
            enableLogging: logFile is not null,
            runUntilCpuCycle: logUntil
        );
        {
            game.Run();
            if (logFile is not null)
            {
                WriteLine($"Writing log to {logFile}");
                File.WriteAllText(logFile, game.TextLog);
            }
        }
    }
}
