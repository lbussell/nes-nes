// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Maths;

namespace NesNes.Gui;

internal interface IGameWindow
{
    void Load();
    void Update(double deltaTime);
    void Render(double deltaTime);
    void FramebufferResize(Vector2D<int> newSize);
}
