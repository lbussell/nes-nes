// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using System.Numerics;

namespace NesNes.Gui.Rendering;

internal static class ImGuiHelper
{
    public static void RenderTextureWithIntegerScaling(Texture texture)
    {
        Vector2 availableSize = ImGui.GetContentRegionAvail();

        // Calculate the maximum integer scale factor that fits in the available space
        int scaleX = Math.Max(1, (int)(availableSize.X / texture.Size.X));
        int scaleY = Math.Max(1, (int)(availableSize.Y / texture.Size.Y));

        // Use the smaller scale factor to maintain aspect ratio
        int scale = Math.Min(scaleX, scaleY);
        var scaledDisplaySize = (Vector2)(scale * texture.Size);

        // Center the image in the available space
        Vector2 initialCursorPosition = ImGui.GetCursorPos();
        Vector2 centerOffset = (availableSize - scaledDisplaySize) * 0.5f;
        Vector2 newCursorPosition = initialCursorPosition + centerOffset;

        // Vector2 is backed by floats. We need to truncate the cursor position
        // as close to ints as possible to avoid subpixel rendering issues.
        newCursorPosition.X = MathF.Truncate(newCursorPosition.X);
        newCursorPosition.Y = MathF.Truncate(newCursorPosition.Y);

        ImGui.SetCursorPos(newCursorPosition);
        ImGui.Image(texture.Handle, scaledDisplaySize);
    }
}
