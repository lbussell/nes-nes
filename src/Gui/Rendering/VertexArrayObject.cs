// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using Silk.NET.OpenGL;
using System.Runtime.CompilerServices;

namespace Common;

public sealed class VertexArrayObject<TVertexType> : IDisposable where TVertexType : unmanaged
{
    private readonly uint _handle;
    private readonly GL _openGl;

    public VertexArrayObject(
        GL openGl,
        BufferObject<TVertexType> vertexBuffer,
        BufferObject<uint>? elementBuffer = null
    )
    {
        //Saving the GL instance.
        _openGl = openGl;

        //Setting out handle and binding the VBO and EBO to this VAO.
        _handle = _openGl.GenVertexArray();
        Bind();
        vertexBuffer.Bind();

        if (elementBuffer is not null)
        {
            elementBuffer.Bind();
        }
    }

    public void VertexAttributePointer(
        uint index,
        int count,
        VertexAttribPointerType type,
        uint vertexSize,
        int offset
    )
    {
        _openGl.VertexAttribPointer(
            index: index,
            size: count,
            type: type,
            normalized: false,
            stride: vertexSize * (uint)Unsafe.SizeOf<TVertexType>(),
            pointer: offset * Unsafe.SizeOf<TVertexType>()
        );

        _openGl.EnableVertexAttribArray(index);
    }

    public void Bind() => _openGl.BindVertexArray(_handle);

    public void Unbind() => _openGl.BindVertexArray(0);

    public void Dispose() => _openGl.DeleteVertexArray(_handle);
}
