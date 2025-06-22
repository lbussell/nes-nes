// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Xna.Framework;

namespace NesNes.Host;

enum ScalingOptions
{
    KeepAspectRatio,
    Stretch,
}

internal interface IScalable
{
    int X { get; set; }
    int Y { get; set; }
    int RenderWidth { get; set; }
    int RenderHeight { get; set; }
    int Width { get; }
    int Height { get; }

    public void ScaleTo(Point viewportSize)
    {
        float scaleY = viewportSize.Y / (float)Height;

        // Uncomment to stretch to fill width of viewport.
        // TODO: Add option to stretch instead of maintaining aspect ratio.
        // float scaleX = viewportSize.X / (float)_display.Width;

        // Keep aspect ratio
        float scaleX = scaleY;

        RenderWidth = (int)(Width * scaleX);
        RenderHeight = (int)(Height * scaleY);

        X = (viewportSize.X - RenderWidth) / 2;
        Y = (viewportSize.Y - RenderHeight) / 2;
    }
}
