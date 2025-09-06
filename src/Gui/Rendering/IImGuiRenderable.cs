// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Maths;

namespace NesNes.Gui.Rendering;

/// <summary>
/// An OpenGL texture that is ready to be rendered in an ImGui window.
/// </summary>
internal interface IImGuiRenderable
{
    nint Handle { get; }
    Vector2D<int> Size { get; }
}
