// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.OpenGL;

namespace NesNes.Gui;

public class BufferObject<T> : IDisposable where T : unmanaged
{
    private readonly uint _handle;
    private readonly BufferTargetARB _bufferType;
    private readonly GL _openGl;

    public BufferObject(GL openGl, Span<T> data, BufferTargetARB bufferType)
    {
        // Setting the gl instance and storing our buffer type.
        _openGl = openGl;
        _bufferType = bufferType;

        // Getting the handle, and then uploading the data to said handle.
        _handle = _openGl.GenBuffer();

        Bind();
        _openGl.BufferData(
            target: _bufferType,
            data: data,
            usage: BufferUsageARB.StaticDraw
        );
    }

    public void Bind() => _openGl.BindBuffer(_bufferType, _handle);

    public void Dispose() => _openGl.DeleteBuffer(_handle);
}
