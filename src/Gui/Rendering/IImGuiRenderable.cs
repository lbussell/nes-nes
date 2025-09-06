// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Maths;

namespace NesNes.Gui.Rendering;

/// <summary>
/// An OpenGL texture that is ready to be rendered in an ImGui window.
/// </summary>
internal interface IImGuiRenderable
{
    /// <summary>
    /// The OpenGL texture handle (Can be cast to uint as well)
    /// </summary>
    nint Handle { get; }

    /// <summary>
    /// Size of the texture in pixels (width, height) - not scaled
    /// </summary>
    Vector2D<int> Size { get; }
}
