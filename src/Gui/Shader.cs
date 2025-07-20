// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.OpenGL;

namespace NesNes.Gui;

public class Shader : IDisposable
{
    private readonly uint _handle;
    private readonly GL _openGl;

    public Shader(GL gl, string vertexShaderCode, string fragmentShaderCode)
    {
        _openGl = gl;

        // Load the individual shaders.
        uint vertexShaderHandle = LoadShader(ShaderType.VertexShader, vertexShaderCode);
        uint fragmentShaderHandle = LoadShader(ShaderType.FragmentShader, fragmentShaderCode);

        // Create the shader program.
        _handle = _openGl.CreateProgram();

        // Link the vertex and fragement shaders to the program.
        _openGl.AttachShader(_handle, vertexShaderHandle);
        _openGl.AttachShader(_handle, fragmentShaderHandle);
        _openGl.LinkProgram(_handle);

        // Check for linking errors.
        _openGl.GetProgram(_handle, GLEnum.LinkStatus, out int status);
        if (status == 0)
        {
            throw new Exception($"Program failed to link with error: {_openGl.GetProgramInfoLog(_handle)}");
        }

        // Detach and delete the shaders, since we have already linked them to the program.
        _openGl.DetachShader(_handle, vertexShaderHandle);
        _openGl.DetachShader(_handle, fragmentShaderHandle);
        _openGl.DeleteShader(vertexShaderHandle);
        _openGl.DeleteShader(fragmentShaderHandle);
    }

    /// <summary>
    /// Tell OpenGL to use this shader program for rendering.
    /// </summary>
    public void Use()
    {
        _openGl.UseProgram(_handle);
    }

    /// <summary>
    /// Uniforms are properties that applies to the entire geometry.
    /// </summary>
    public void SetUniform(string name, int value)
    {
        //Setting a uniform on a shader using a name.
        int location = _openGl.GetUniformLocation(_handle, name);
        if (location == -1) //If GetUniformLocation returns -1 the uniform is not found.
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _openGl.Uniform1(location, value);
    }

    /// <summary>
    /// Uniforms are properties that applies to the entire geometry.
    /// </summary>
    public void SetUniform(string name, float value)
    {
        int location = _openGl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _openGl.Uniform1(location, value);
    }

    public void Dispose()
    {
        _openGl.DeleteProgram(_handle);
    }

    private uint LoadShader(ShaderType type, string sourceCode)
    {
        var shaderHandle = _openGl.CreateShader(type);

        // Upload and compile the shader source code.
        _openGl.ShaderSource(shaderHandle, sourceCode);
        _openGl.CompileShader(shaderHandle);

        // Check for errors and exit if compilation failed.
        string infoLog = _openGl.GetShaderInfoLog(shaderHandle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
        }

        // Return the OpenGL handle for the compiled shader.
        return shaderHandle;
    }
}
