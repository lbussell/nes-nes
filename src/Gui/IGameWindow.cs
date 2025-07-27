// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Maths;

internal interface IGameWindow : IUpdatable, IRenderable
{
    void OnClose();
    void OnFramebufferResize(Vector2D<int> newSize);
}
