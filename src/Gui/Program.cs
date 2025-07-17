// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Windowing.Glfw;
using Silk.NET.Input.Glfw;
using NesNes.Gui;

// Needed for Native AOT. Usually this is done with Reflection in Silk.NET, but
// that doesn't work with Native AOT, so we have to do it manually.
GlfwWindowing.RegisterPlatform();
GlfwInput.RegisterPlatform();

using (var window = GameWindow.Create())
{
    window.Run();
}
