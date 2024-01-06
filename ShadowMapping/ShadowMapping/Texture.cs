// <copyright file="Texture.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public sealed class Texture : IDisposable
{
    private bool isDisposed;

    private int? texture;

    public Texture(
        int width,
        int height,
        IntPtr data,
        PixelFormat format,
        PixelInternalFormat internalFormat,
        PixelType type,
        TextureMinFilter minFilter,
        TextureMagFilter magFilter,
        TextureWrapMode wrapS,
        TextureWrapMode wrapT,
        bool generateMipmaps)
    {
        this.texture = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, (int)this.texture);

        GL.TextureParameter((int)this.texture, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TextureParameter((int)this.texture, TextureParameterName.TextureMagFilter, (int)magFilter);
        GL.TextureParameter((int)this.texture, TextureParameterName.TextureWrapS, (int)wrapS);
        GL.TextureParameter((int)this.texture, TextureParameterName.TextureWrapT, (int)wrapT);

        if (data != IntPtr.Zero)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, type, data);

            if (generateMipmaps)
            {
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }
        }
    }

    ~Texture()
    {
        this.Dispose(false);
    }

    public static Texture LoadTexture(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

        using (var stream = File.OpenRead(filePath))
        {
            using (var image = Image.Load<Rgba32>(stream))
            {
                int width = image.Width;
                int height = image.Height;

                var pixels = new List<byte>(4 * image.Width * image.Height);

                for (int y = 0; y < image.Height; y++)
                {
                    var row = image.GetPixelRowSpan(y);

                    for (int x = 0; x < image.Width; x++)
                    {
                        pixels.Add(row[x].R);
                        pixels.Add(row[x].G);
                        pixels.Add(row[x].B);
                        pixels.Add(row[x].A);
                    }
                }

                byte[] data = [.. pixels];
                var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                IntPtr ptr = data == null ? IntPtr.Zero : handle.AddrOfPinnedObject();

                var texture = new Texture(
                    width,
                    height,
                    ptr,
                    PixelFormat.Rgba,
                    PixelInternalFormat.Rgba,
                    PixelType.UnsignedByte,
                    TextureMinFilter.Nearest,
                    TextureMagFilter.Linear,
                    TextureWrapMode.Repeat,
                    TextureWrapMode.Repeat,
                    true);

                handle.Free();

                return texture;
            }
        }
    }

    public void Attach(FramebufferAttachment attachment)
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, (int)this.texture!, 0);
    }

    public void Bind(int unit)
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(TextureTarget.Texture2D, (int)this.texture!);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (this.isDisposed)
        {
            return;
        }

        if (disposing)
        {
            if (this.texture != null)
            {
                GL.DeleteTexture((int)this.texture);
                this.texture = null;
            }
        }

        this.isDisposed = true;
    }
}
