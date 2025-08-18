// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

namespace NesNes.Core;

public interface IPpu : ICpuReadable, ICpuWritable
{
    long Frame { get; }
    int Scanline { get; }
    int Cycle { get; }

    bool NonMaskableInterruptPin { get; }

    IMapper? Mapper { get; set; }

    public RenderPixel? OnRenderPixel { get; set; }

    void WriteOam(byte address, byte value);

    void Step();

    public void Step(int cycles)
    {
        for (int i = 0; i < cycles; i += 1)
        {
            Step();
        }
    }
}
