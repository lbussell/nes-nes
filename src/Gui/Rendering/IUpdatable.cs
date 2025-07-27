// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Gui.Rendering;

internal interface IUpdatable
{
    /// <summary>
    /// Happens before <see cref="Render"/>.
    /// </summary>
    /// <param name="deltaTimeSeconds">
    /// Time in seconds since the last time this method was called.
    /// </param>
    void Update(double deltaTimeSeconds);
}
