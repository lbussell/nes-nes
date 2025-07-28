// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Gui.Views;

internal interface IClosableWindow : IImGuiWindow
{
    string Name { get; }
    ref bool Open { get; }
}
