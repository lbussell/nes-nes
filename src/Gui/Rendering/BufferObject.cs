// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.OpenGL;

namespace Common;

public sealed class BufferObject<T> : IDisposable where T : unmanaged
{
    private readonly GL _gl;
    private readonly BufferTargetARB _bufferType;
    private readonly uint _handle;

    public BufferObject(GL gl, BufferTargetARB bufferType, ReadOnlySpan<T> data)
    {
        _gl = gl;
        _bufferType = bufferType;

        _handle = _gl.GenBuffer();
        _gl.BindBuffer(_bufferType, _handle);
        _gl.BufferData(_bufferType, data, BufferUsageARB.StaticDraw);
    }

    public void Bind() => _gl.BindBuffer(_bufferType, _handle);

    public void Unbind() => _gl.BindBuffer(_bufferType, 0);

    public void Dispose() => _gl.DeleteBuffer(_handle);
}
