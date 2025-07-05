// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

/// <summary>
/// Describes a range of memory.
/// </summary>
/// <param name="Start">The inclusive start of the memory range.</param>
/// <param name="End">The inclusive end of the memory range.</param>
public readonly record struct MemoryRange(ushort Start, ushort End);
