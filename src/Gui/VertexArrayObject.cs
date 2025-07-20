// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace NesNes.Gui;

public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
    where TVertexType : unmanaged
    where TIndexType : unmanaged
{
    //Our handle and the GL instance this class will use, these are private because they have no reason to be public.
    //Most of the time you would want to abstract items to make things like this invisible.
    private uint _handle;
    private readonly GL _openGl;

    public VertexArrayObject(
        GL openGl,
        BufferObject<TVertexType> vertexBuffer,
        BufferObject<TIndexType> elementBuffer
    )
    {
        //Saving the GL instance.
        _openGl = openGl;

        //Setting out handle and binding the VBO and EBO to this VAO.
        _handle = _openGl.GenVertexArray();
        Bind();
        vertexBuffer.Bind();
        elementBuffer.Bind();
    }

    public void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offset)
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

    public void Bind()
    {
        _openGl.BindVertexArray(_handle);
    }

    public void Dispose()
    {
        // Don't delete the VBO and EBO here, as you can have one VBO stored under multiple VAO's.
        _openGl.DeleteVertexArray(_handle);
    }
}
