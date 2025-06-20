// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add<EmulatorCli>();
await app.RunAsync(args);
