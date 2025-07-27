// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core.Mappers;

namespace NesNes.Core;

public static class MapperFactory
{
    public static IMapper Create(CartridgeData cartridge) =>
        cartridge.Header.Mapper switch
        {
            0 => new NromMapper(cartridge),
            _ => throw new NotSupportedException($"Mapper {cartridge.Header.Mapper} is not supported.")
        };
}
