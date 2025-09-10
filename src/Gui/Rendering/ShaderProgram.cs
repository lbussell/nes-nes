// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.OpenGL;

namespace Common;

public sealed class ShaderProgram(Shader vertexShader, Shader fragmentShader)
{
    private readonly Shader _vertexShader = vertexShader;
    private readonly Shader _fragmentShader = fragmentShader;

    public LinkedShaderProgram Build(GL gl)
    {
        CompiledShader compiledVertexShader = _vertexShader.Compile(gl);
        CompiledShader compiledFragmentShader = _fragmentShader.Compile(gl);

        uint programHandle = gl.CreateProgram();
        gl.AttachShader(programHandle, compiledVertexShader.Handle);
        gl.AttachShader(programHandle, compiledFragmentShader.Handle);
        gl.LinkProgram(programHandle);

        // Verify program linked successfully
        gl.GetProgram(programHandle, GLEnum.LinkStatus, out var linkStatus);
        if (linkStatus == 0)
        {
            var infoLog = gl.GetProgramInfoLog(programHandle);
            throw new Exception($"Error linking shader program: {infoLog}");
        }

        gl.DetachShader(programHandle, compiledVertexShader.Handle);
        gl.DeleteShader(compiledVertexShader.Handle);

        gl.DetachShader(programHandle, compiledFragmentShader.Handle);
        gl.DeleteShader(compiledFragmentShader.Handle);

        return new LinkedShaderProgram(gl, programHandle);
    }
}
