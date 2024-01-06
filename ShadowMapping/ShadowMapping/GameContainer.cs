// <copyright file="GameContainer.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using System.Drawing;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

public sealed class GameContainer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    private Camera camera;

    private ShaderProgram debugShaderProgram;

    private ShaderProgram depthShaderProgram;

    private Texture depthTexture;

    private Vector3 lightPosition;

    private Mesh planeMesh;

    private Mesh quadMesh;

    private RenderTarget renderTarget;

    private ShaderProgram shaderProgram;

    private Texture texture;

    protected override void OnLoad()
    {
        float fieldDepth = 10.0f;
        float fieldWidth = 10.0f;

        MeshVertex[] vertices = [
            new MeshVertex()
            {
                Position = new Vector3(-fieldWidth, 0.0f, -fieldDepth),
                TextureCoordinate = new Vector2(0.0f),
                Color = new Vector3(1, 1, 1),
            },

            new MeshVertex()
            {
                Position = new Vector3(-fieldWidth, 0.0f, fieldDepth * 3),
                TextureCoordinate = new Vector2(0.0f, 1.0f),
                Color = new Vector3(1, 1, 1),
            },

            new MeshVertex()
            {
                Position = new Vector3(fieldWidth * 3, 0.0f, -fieldDepth),
                TextureCoordinate = new Vector2(1.0f, 0.0f),
                Color = new Vector3(1, 1, 1),
            },

            new MeshVertex()
            {
                Position = new Vector3(fieldWidth * 3, 0.0f, fieldDepth * 3),
                TextureCoordinate = new Vector2(1.0f, 1.0f),
                Color = new Vector3(1, 1, 1),
            },
        ];

        int[] indices =
        [
            0,
            1,
            2,
            2,
            1,
            3
        ];

        MeshVertex[] quadVertices =
        [
            new MeshVertex() { Position = new Vector3(0.5f, 0.5f, 0.0f), Color = new Vector3(1.0f), TextureCoordinate = new Vector2(1.0f, 1.0f) },
            new MeshVertex() { Position = new Vector3(0.5f, -0.5f, 0.0f), Color = new Vector3(1.0f), TextureCoordinate = new Vector2(1.0f, 0.0f) },
            new MeshVertex() { Position = new Vector3(-0.5f, -0.5f, 0.0f), Color = new Vector3(1.0f), TextureCoordinate = new Vector2(0.0f, 0.0f) },
            new MeshVertex() { Position = new Vector3(-0.5f, 0.5f, 0.0f), Color = new Vector3(1.0f), TextureCoordinate = new Vector2(0.0f, 1.0f) },
        ];

        int[] quadIndices =
        [
            0,
            1,
            3,
            1,
            2,
            3,
        ];

        this.planeMesh = new Mesh(vertices, indices);
        this.quadMesh = new Mesh(quadVertices, quadIndices);

        this.texture = Texture.LoadTexture("Resources\\Textures\\texture.png");

        this.depthTexture = new Texture(
            width: 1024,
            height: 1024,
            IntPtr.Zero,
            PixelFormat.DepthComponent,
            PixelInternalFormat.DepthComponent,
            PixelType.Float,
            TextureMinFilter.Nearest,
            TextureMagFilter.Nearest,
            TextureWrapMode.Repeat,
            TextureWrapMode.Repeat,
            false);

        this.renderTarget = new RenderTarget(this.depthTexture);

        this.shaderProgram = new ShaderProgram(
            File.ReadAllText("Resources\\Shaders\\shader.vert"),
            File.ReadAllText("Resources\\Shaders\\shader.frag"));

        this.depthShaderProgram = new ShaderProgram(
            File.ReadAllText("Resources\\Shaders\\depth.vert"),
            File.ReadAllText("Resources\\Shaders\\depth.frag"));

        this.debugShaderProgram = new ShaderProgram(
            File.ReadAllText("Resources\\Shaders\\debug.vert"),
            File.ReadAllText("Resources\\Shaders\\debug.frag"));

        this.camera = new Camera(this, this.ClientSize.X, this.ClientSize.Y);

        this.lightPosition = new Vector3(10);

        base.OnLoad();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Enable(EnableCap.DepthTest);

        GL.ClearColor(Color.Black);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var lightProjection = Matrix4.CreateOrthographicOffCenter(-1000.0f, 1000.0f, -1000.0f, 1000.0f, 1.0f, 7.5f);
        var lightView = Matrix4.LookAt(this.lightPosition, Vector3.Zero, Vector3.UnitY);
        var lightSpace = lightProjection * lightView;

        this.depthShaderProgram.Use();
        this.depthShaderProgram.SetMatrix4("u_lightSpace", lightSpace);

        GL.Viewport(0, 0, 1024, 1024);
        this.renderTarget.Bind();

        this.RenderScene(this.depthShaderProgram);

        this.renderTarget.Unbind();

        GL.Viewport(0, 0, this.ClientSize.X, this.ClientSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        this.debugShaderProgram.Use();
        this.debugShaderProgram.SetInt("depthMap", 0);

        this.depthTexture.Bind(0);
        this.quadMesh.Draw();

        this.SwapBuffers();

        base.OnRenderFrame(args);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        this.camera.Update(this.KeyboardState, this.MouseState);
        base.OnUpdateFrame(args);
    }

    private void RenderScene(ShaderProgram shaderProgram)
    {
        shaderProgram.SetMatrix4("u_transform", Matrix4.Identity);

        this.texture.Bind(0);
        this.planeMesh.Draw();

        shaderProgram.SetMatrix4("u_transform", Matrix4.CreateTranslation(0, 5f, 0) * Matrix4.CreateScale(0.25f));

        this.planeMesh.Draw();

        shaderProgram.SetMatrix4("u_transform", Matrix4.CreateTranslation(this.lightPosition) * Matrix4.CreateScale(0.10f));

        this.planeMesh.Draw();
    }
}
