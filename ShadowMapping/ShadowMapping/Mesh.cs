// <copyright file="Mesh.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using OpenTK.Graphics.OpenGL4;

public sealed class Mesh : IDisposable
{
    private readonly int length;

    private int? elementBufferObject;

    private bool isDisposed;

    private int? vertexArrayObject;

    private int? vertexBufferObject;

    public Mesh(MeshVertex[] vertices, int[] indices)
    {
        ArgumentNullException.ThrowIfNull(vertices, nameof(vertices));
        ArgumentNullException.ThrowIfNull(indices, nameof(indices));

        this.vertexArrayObject = GL.GenVertexArray();
        this.vertexBufferObject = GL.GenBuffer();
        this.elementBufferObject = GL.GenBuffer();
        this.length = indices.Length;

        GL.BindVertexArray((int)this.vertexArrayObject);

        GL.BindBuffer(BufferTarget.ArrayBuffer, (int)this.vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * MeshVertex.SizeInBytes, vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, (int)this.elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(0);
    }

    ~Mesh()
    {
        this.Dispose(false);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Draw()
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.BindVertexArray((int)this.vertexArrayObject!);
        GL.DrawElements(PrimitiveType.Triangles, this.length, DrawElementsType.UnsignedInt, 0);
    }

    private void Dispose(bool disposing)
    {
        if (this.isDisposed)
        {
            return;
        }

        if (disposing)
        {
            if (this.elementBufferObject != null)
            {
                GL.DeleteBuffer((int)this.elementBufferObject);
                this.elementBufferObject = null;
            }

            if (this.vertexBufferObject != null)
            {
                GL.DeleteBuffer((int)this.vertexBufferObject);
                this.vertexBufferObject = null;
            }

            if (this.vertexArrayObject != null)
            {
                GL.DeleteVertexArray((int)this.vertexArrayObject);
                this.vertexArrayObject = null;
            }
        }

        this.isDisposed = true;
    }
}
