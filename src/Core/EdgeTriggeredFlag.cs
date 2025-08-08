// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class EdgeTriggeredFlag
{
    private bool _previousState = false;

    public void Update(bool value)
    {
        _previousState = Current;
        Current = value;
    }

    public bool Current { get; private set; } = false;
    public bool Rising => !_previousState && Current;
    public bool Falling => _previousState && !Current;
    public bool JustChanged => _previousState != Current;
}
