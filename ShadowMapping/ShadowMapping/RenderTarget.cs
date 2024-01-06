// <copyright file="RenderTarget.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using OpenTK.Graphics.OpenGL4;

public sealed class RenderTarget : IDisposable
{
    private int? frameBuffer;

    private bool isDisposed;

    public RenderTarget(Texture target)
    {
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        this.frameBuffer = GL.GenFramebuffer();

        this.Bind();

        target.Attach(FramebufferAttachment.DepthAttachment);

        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        this.Unbind();
    }

    ~RenderTarget()
    {
        this.Dispose(false);
    }

    public void Bind()
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, (int)this.frameBuffer!);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Unbind()
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void Dispose(bool disposing)
    {
        if (this.isDisposed)
        {
            return;
        }

        if (disposing)
        {
            if (this.frameBuffer != null)
            {
                GL.DeleteFramebuffer((int)this.frameBuffer);
                this.frameBuffer = null;
            }
        }

        this.isDisposed = true;
    }
}
