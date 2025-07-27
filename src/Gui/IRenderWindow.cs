// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

internal interface IRenderWindow
{
    void Render(double deltaTimeSeconds);

    /// <summary>
    /// Happens before <see cref="Render"/>.
    /// </summary>
    /// <param name="deltaTimeSeconds">
    /// Time in seconds since the last time this method was called.
    /// </param>
    void Update(double deltaTimeSeconds);
}
