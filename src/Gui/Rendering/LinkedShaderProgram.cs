// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Common;


public sealed class LinkedShaderProgram(GL gl, uint handle) : IDisposable
{
    private readonly GL _gl = gl;

    public uint Handle { get; } = handle;

    public void Dispose() => _gl.DeleteProgram(Handle);

    public void SetUniform(string name, int value)
    {
        var location = GetUniformLocation(name);
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, float value)
    {
        var location = GetUniformLocation(name);
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, double value)
    {
        var location = GetUniformLocation(name);
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, Vector2D<int> value)
    {
        var location = GetUniformLocation(name);
        _gl.Uniform2(location, value.X, value.Y);
    }

    private int GetUniformLocation(string name)
    {
        int location = _gl.GetUniformLocation(Handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        return location;
    }
}
