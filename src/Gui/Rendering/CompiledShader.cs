// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.OpenGL;

namespace Common;

public sealed class CompiledShader(GL gl, uint handle) : IDisposable
{
    private readonly GL _gl = gl;

    public uint Handle { get; } = handle;

    public void Dispose() => _gl.DeleteShader(Handle);
}
