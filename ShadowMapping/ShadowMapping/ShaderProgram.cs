// <copyright file="ShaderProgram.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

public sealed class ShaderProgram : IDisposable
{
    private bool isDisposed;

    private int? shaderProgram;

    public ShaderProgram(string vertexShaderSource, string fragmentShaderSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vertexShaderSource, nameof(vertexShaderSource));
        ArgumentException.ThrowIfNullOrWhiteSpace(fragmentShaderSource, nameof(fragmentShaderSource));

        int vertexShader = CreateShader(ShaderType.VertexShader, vertexShaderSource);
        int fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentShaderSource);

        this.shaderProgram = GL.CreateProgram();

        GL.AttachShader((int)this.shaderProgram, vertexShader);
        GL.AttachShader((int)this.shaderProgram, fragmentShader);

        GL.LinkProgram((int)this.shaderProgram);
        GL.ValidateProgram((int)this.shaderProgram);

        string log = GL.GetProgramInfoLog((int)this.shaderProgram);

        if (!string.IsNullOrWhiteSpace(log))
        {
            throw new InvalidOperationException(log);
        }

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    ~ShaderProgram()
    {
        this.Dispose(false);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void SetInt(string name, int value)
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.Uniform1(this.GetUniformLocation(name), value);
    }

    public void SetMatrix4(string name, Matrix4 matrix)
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.UniformMatrix4(this.GetUniformLocation(name), false, ref matrix);
    }

    public void SetVector3(string name, Vector3 vector)
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.Uniform3(this.GetUniformLocation(name), vector.X, vector.Y, vector.Z);
    }

    public void Use()
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.UseProgram((int)this.shaderProgram!);
    }

    private static int CreateShader(ShaderType type, string shaderSource)
    {
        int shader = GL.CreateShader(type);

        GL.ShaderSource(shader, shaderSource);
        GL.CompileShader(shader);

        string log = GL.GetShaderInfoLog(shader);

        if (!string.IsNullOrWhiteSpace(log))
        {
            throw new InvalidOperationException(log);
        }

        return shader;
    }

    private void Dispose(bool disposing)
    {
        if (this.isDisposed)
        {
            return;
        }

        if (disposing)
        {
            if (this.shaderProgram != null)
            {
                GL.DeleteProgram((int)this.shaderProgram);
                this.shaderProgram = null;
            }
        }

        this.isDisposed = true;
    }

    private int GetUniformLocation(string name)
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        return GL.GetUniformLocation((int)this.shaderProgram!, name);
    }
}
