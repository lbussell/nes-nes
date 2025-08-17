// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

internal static class DebugColors
{
    public static readonly Color Blank = new(102, 102, 102);
    public static readonly Color Flag = new(111, 215, 152);

    public static readonly Color NameTableFetch = new(212, 160, 121);
    public static readonly Color AttributeFetch = new(242, 190, 151);
    public static readonly Color BackgroundLowFetch = new(212, 160, 121);
    public static readonly Color BackgroundHighFetch = new(242, 190, 151);

    public static readonly Color VUpdate = new(253, 41, 38);
    public static readonly Color Other = new(131, 241, 255);
    public static readonly Color Misc = Flag;
}
