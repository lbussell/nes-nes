// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using NesNes.Gui;

var app = ConsoleApp.Create();
app.Add<Cli>();
app.Run(["--rom", "/home/logan/src/nes-nes/roms/dk.nes"]);
