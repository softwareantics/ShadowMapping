// <copyright file="GameContainer.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using System.IO;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

public sealed class GameContainer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    private const int ShadowHeight = 1024;

    private const int ShadowWidth = 1024;

    private Camera camera;

    private int cubeVAO;

    private ShaderProgram debugShader;

    private int depthMap;

    private int depthMapFBO;

    private ShaderProgram depthShader;

    private Vector3 lightPosition;

    private int planeVAO;

    private int quadVAO;

    private int quadVBO;

    private ShaderProgram shader;

    private Texture woodTexture;

    protected override void OnLoad()
    {
        this.shader = new ShaderProgram(
            File.ReadAllText("Resources\\Shaders\\shader.vert"),
            File.ReadAllText("Resources\\Shaders\\shader.frag"));

        this.depthShader = new ShaderProgram(
            File.ReadAllText("Resources\\Shaders\\depth.vert"),
            File.ReadAllText("Resources\\Shaders\\depth.frag"));

        this.debugShader = new ShaderProgram(
            File.ReadAllText("Resources\\Shaders\\debug.vert"),
            File.ReadAllText("Resources\\Shaders\\debug.frag"));

        float[] planeVertices =
        {
             25.0f, -0.5f,  25.0f,  0.0f, 1.0f, 0.0f,  25.0f,  0.0f,
            -25.0f, -0.5f,  25.0f,  0.0f, 1.0f, 0.0f,   0.0f,  0.0f,
            -25.0f, -0.5f, -25.0f,  0.0f, 1.0f, 0.0f,   0.0f, 25.0f,

             25.0f, -0.5f,  25.0f,  0.0f, 1.0f, 0.0f,  25.0f,  0.0f,
            -25.0f, -0.5f, -25.0f,  0.0f, 1.0f, 0.0f,   0.0f, 25.0f,
             25.0f, -0.5f, -25.0f,  0.0f, 1.0f, 0.0f,  25.0f, 25.0f,
        };

        float[] cubeVertices = {
			// back face
			-1.0f, -1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 0.0f, 0.0f, // bottom-left
			 1.0f,  1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 1.0f, 1.0f, // top-right
			 1.0f, -1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 1.0f, 0.0f, // bottom-right
			 1.0f,  1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 1.0f, 1.0f, // top-right
			-1.0f, -1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 0.0f, 0.0f, // bottom-left
			-1.0f,  1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 0.0f, 1.0f, // top-left
			// front face
			-1.0f, -1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f, 0.0f, // bottom-left
			 1.0f, -1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 1.0f, 0.0f, // bottom-right
			 1.0f,  1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 1.0f, 1.0f, // top-right
			 1.0f,  1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 1.0f, 1.0f, // top-right
			-1.0f,  1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f, 1.0f, // top-left
			-1.0f, -1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f, 0.0f, // bottom-left
			// left face
			-1.0f,  1.0f,  1.0f, -1.0f,  0.0f,  0.0f, 1.0f, 0.0f, // top-right
			-1.0f,  1.0f, -1.0f, -1.0f,  0.0f,  0.0f, 1.0f, 1.0f, // top-left
			-1.0f, -1.0f, -1.0f, -1.0f,  0.0f,  0.0f, 0.0f, 1.0f, // bottom-left
			-1.0f, -1.0f, -1.0f, -1.0f,  0.0f,  0.0f, 0.0f, 1.0f, // bottom-left
			-1.0f, -1.0f,  1.0f, -1.0f,  0.0f,  0.0f, 0.0f, 0.0f, // bottom-right
			-1.0f,  1.0f,  1.0f, -1.0f,  0.0f,  0.0f, 1.0f, 0.0f, // top-right
			// right face
			 1.0f,  1.0f,  1.0f,  1.0f,  0.0f,  0.0f, 1.0f, 0.0f, // top-left
			 1.0f, -1.0f, -1.0f,  1.0f,  0.0f,  0.0f, 0.0f, 1.0f, // bottom-right
			 1.0f,  1.0f, -1.0f,  1.0f,  0.0f,  0.0f, 1.0f, 1.0f, // top-right
			 1.0f, -1.0f, -1.0f,  1.0f,  0.0f,  0.0f, 0.0f, 1.0f, // bottom-right
			 1.0f,  1.0f,  1.0f,  1.0f,  0.0f,  0.0f, 1.0f, 0.0f, // top-left
			 1.0f, -1.0f,  1.0f,  1.0f,  0.0f,  0.0f, 0.0f, 0.0f, // bottom-left
			 // bottom face
			 -1.0f, -1.0f, -1.0f,  0.0f, -1.0f,  0.0f, 0.0f, 1.0f, // top-right
			  1.0f, -1.0f, -1.0f,  0.0f, -1.0f,  0.0f, 1.0f, 1.0f, // top-left
			  1.0f, -1.0f,  1.0f,  0.0f, -1.0f,  0.0f, 1.0f, 0.0f, // bottom-left
			  1.0f, -1.0f,  1.0f,  0.0f, -1.0f,  0.0f, 1.0f, 0.0f, // bottom-left
			 -1.0f, -1.0f,  1.0f,  0.0f, -1.0f,  0.0f, 0.0f, 0.0f, // bottom-right
			 -1.0f, -1.0f, -1.0f,  0.0f, -1.0f,  0.0f, 0.0f, 1.0f, // top-right
			 // top face
			 -1.0f,  1.0f, -1.0f,  0.0f,  1.0f,  0.0f, 0.0f, 1.0f, // top-left
			  1.0f,  1.0f , 1.0f,  0.0f,  1.0f,  0.0f, 1.0f, 0.0f, // bottom-right
			  1.0f,  1.0f, -1.0f,  0.0f,  1.0f,  0.0f, 1.0f, 1.0f, // top-right
			  1.0f,  1.0f,  1.0f,  0.0f,  1.0f,  0.0f, 1.0f, 0.0f, // bottom-right
			 -1.0f,  1.0f, -1.0f,  0.0f,  1.0f,  0.0f, 0.0f, 1.0f, // top-left
			 -1.0f,  1.0f,  1.0f,  0.0f,  1.0f,  0.0f, 0.0f, 0.0f  // bottom-left
		};

        float[] quadVertices = {
            -1.0f,  1.0f, 0.0f, 0.0f, 1.0f,
            -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,
             1.0f,  1.0f, 0.0f, 1.0f, 1.0f,
             1.0f, -1.0f, 0.0f, 1.0f, 0.0f,
        };

        this.quadVAO = GL.GenVertexArray();
        this.quadVBO = GL.GenBuffer();

        GL.BindVertexArray(quadVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        GL.BindVertexArray(0);

        this.planeVAO = CreateMesh(planeVertices);
        this.cubeVAO = CreateMesh(cubeVertices);

        this.woodTexture = Texture.LoadTexture("Resources\\Textures\\wood.png");

        this.depthMapFBO = GL.GenFramebuffer();
        this.depthMap = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, this.depthMap);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, ShadowWidth, ShadowHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.depthMapFBO);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, this.depthMap, 0);

        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        shader.Use();

        shader.SetInt("diffuseTexture", 0);
        shader.SetInt("shadowMap", 1);

        debugShader.Use();
        debugShader.SetInt("depthMap", 0);

        this.lightPosition = new Vector3(-2.0f, 4.0f, -1.0f);

        this.camera = new Camera(this, ClientSize.X, ClientSize.Y);

        base.OnLoad();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var lightProjection = Matrix4x4.CreateOrthographicOffCenter(-10.0f, 10.0f, -10.0f, 10.0f, 1.0f, 7.5f);
        var lightView = Matrix4x4.CreateLookAt(lightPosition, Vector3.Zero, Vector3.UnitY);
        var lightSpace = lightView * lightProjection;

        depthShader.Use();
        depthShader.SetMatrix4("lightSpaceMatrix", lightSpace);

        GL.Viewport(0, 0, ShadowWidth, ShadowHeight);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.depthMapFBO);

        GL.Clear(ClearBufferMask.DepthBufferBit);

        this.RenderScene(this.depthShader);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        shader.Use();

        shader.SetMatrix4("projection", camera.Projection);
        shader.SetMatrix4("view", camera.View);
        shader.SetVector3("viewPos", camera.Transform.Position);
        shader.SetVector3("lightPos", this.lightPosition);
        shader.SetMatrix4("lightSpaceMatrix", lightSpace);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, this.depthMap);

        this.RenderScene(this.shader);

        //debugShader.Use();

        //GL.ActiveTexture(TextureUnit.Texture0);
        //GL.BindTexture(TextureTarget.Texture2D, this.depthMap);

        //GL.BindVertexArray(quadVAO);
        //GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        this.SwapBuffers();
        base.OnRenderFrame(args);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        camera.Update(this.KeyboardState, this.MouseState);
        base.OnUpdateFrame(args);
    }

    private int CreateMesh(float[] vertices)
    {
        int vao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

        GL.BindVertexArray(0);

        return vao;
    }

    private void RenderScene(ShaderProgram shader)
    {
        this.woodTexture.Bind(0);

        var model = Matrix4x4.Identity;
        shader.SetMatrix4("model", model);

        GL.BindVertexArray(this.planeVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(0, 1.5f, 0) * Matrix4x4.CreateScale(0.5f);
        shader.SetMatrix4("model", model);

        GL.BindVertexArray(this.cubeVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(10.0f, 0, 1.0f) * Matrix4x4.CreateScale(0.5f);
        shader.SetMatrix4("model", model);

        GL.BindVertexArray(this.cubeVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }
}
