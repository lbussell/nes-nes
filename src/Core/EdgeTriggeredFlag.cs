// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public class EdgeTriggeredFlag
{
    private bool _currentState = false;
    private bool _previousState = false;

    public void Update(bool value)
    {
        _previousState = _currentState;
        _currentState = value;
    }

    public bool Current => _currentState;
    public bool Rising => !_previousState && _currentState;
    public bool Falling => _previousState && !_currentState;
    public bool JustChanged => _previousState != _currentState;
}
