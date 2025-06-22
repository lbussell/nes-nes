// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Color = Microsoft.Xna.Framework.Color;

namespace NesNes.Host;

internal interface IRenderTarget
{
    /// <summary>
    /// Width of the render target in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Height of the render target in pixels.
    /// </summary>
    int Height { get; }

    void SetPixel(int x, int y, Color color);
}
