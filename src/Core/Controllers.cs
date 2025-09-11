// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public delegate (byte controller1, byte controller2) ReadControllers();

public class Controllers
{
    // Two controllers
    private byte _controller1;
    private byte _controller2;

    /// <summary>
    /// This callback is called to read the state of the controllers. It should
    /// return a tuple containing the state of controller 1 and controller 2,
    /// where each controller's complete state is represented by a byte.
    /// </summary>
    /// <remarks>
    /// Bit to button mapping: 7:A, 6:B, 5:Select, 4:Start, 3:Up, 2:Down,
    /// 1:Left, 0:Right
    /// </remarks>
    public ReadControllers ReadControllers { get; set; } = () => (0, 0);

    public bool ListenRead(ushort address, out byte value)
    {
        // Open bus behavior: reading from the controller registers only
        // affects bits 4-0 of the value. The higher bits are are mapped to
        // the open bus. This usually contains the corresponding bits from the
        // previous read. Since we're reading from the controller register,
        // that value is usually 0x40.
        value = 0x40;

        // TODO: Controller handling

        return true;
    }

    public bool ListenWrite(ushort address, byte value)
    {
        // TODO: Controller handling
        return true;
    }
}
