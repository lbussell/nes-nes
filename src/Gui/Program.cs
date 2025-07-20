// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input.Glfw;
using Silk.NET.Maths;
using Silk.NET.Windowing.Glfw;


// Needed for Native AOT. Usually this is done with Reflection in Silk.NET, but
// that doesn't work with Native AOT, so we have to do it manually.
GlfwWindowing.RegisterPlatform();
GlfwInput.RegisterPlatform();

const int Scale = 3;
// NES display size
var displaySize = new Vector2D<int>(256, 240);
var windowSize = Scale * displaySize;

using var window = new WindowManager<GameWindow>(
    windowSize,
    (gl, input, imGui) => new GameWindow(
        gl,
        input,
        imGui,
        internalSize: displaySize
    ),
    "My Window"
);

window.Run();
window.Dispose();
