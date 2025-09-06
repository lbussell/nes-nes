// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.OpenGL;

namespace Common;

public sealed class Shader(ShaderType type, string source)
{
    private readonly string _source = source;
    private readonly ShaderType _shaderType = type;

    public CompiledShader Compile(GL gl)
    {
        uint shaderHandle = gl.CreateShader(_shaderType);
        gl.ShaderSource(shaderHandle, _source);
        gl.CompileShader(shaderHandle);

        // Verify shader compiled successfully
        var infoLog = gl.GetShaderInfoLog(shaderHandle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling {_shaderType} shader: {infoLog}");
        }

        return new CompiledShader(gl, shaderHandle);
    }
}
