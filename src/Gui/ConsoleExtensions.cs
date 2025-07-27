// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Maths;
using NesNes.Core;

internal static class ConsoleExtensions
{
    private static readonly Vector2D<int> s_displaySize = new(Ppu.DisplayWidth, Ppu.DisplayHeight);

    public static Vector2D<int> GetDisplaySize(this NesConsole console) => s_displaySize;
}
