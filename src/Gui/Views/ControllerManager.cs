// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Input;

namespace NesNes.Gui.Views;

internal sealed class ControllerManager(IInputContext inputContext)
{
    public IKeyboard Keyboard => inputContext.Keyboards[0];

    public Key[] Mapping { get; } =
    [
        Key.Z,
        Key.X,
        Key.BackSlash,
        Key.Enter,
        Key.Up,
        Key.Down,
        Key.Left,
        Key.Right,
    ];

    /// <summary>
    /// Returns the current controller state as a bitfield as expected by the NES
    /// </summary>
    public byte Scan()
    {
        byte state = 0;
        for (int i = 0; i < Mapping.Length; i++)
        {
            if (Keyboard.IsKeyPressed(Mapping[i]))
            {
                state |= (byte)(1 << i);
            }
        }

        return state;
    }
}
