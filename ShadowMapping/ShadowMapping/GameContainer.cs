// <copyright file="GameContainer.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using System.IO;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

//// TODO: Move all of this into Final Engine
//// TODO: Add cascaded shadow mapping to resolve issue with directional lighting.

public sealed class GameContainer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    private const int ShadowHeight = 8192;

    private const int ShadowWidth = 8192;

    private Camera camera;

    private ImGuiController controller;

    private ShaderProgram debugShader;

    private int depthMap;

    private int depthMapFBO;

    private ShaderProgram depthShader;

    private Vector3 lightColor;

    private Vector3 lightPosition;

    private ModelLoader loader;

    private ModelResource modelResource;

    private ShaderProgram shader;

    private float temp = 0;

    private Texture texture;

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

        this.texture = Texture.LoadTexture("Resources\\Textures\\texture.png");

        this.depthMapFBO = GL.GenFramebuffer();
        this.depthMap = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, this.depthMap);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, ShadowWidth, ShadowHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

        float[] borderColor = [1.0f, 1.0f, 1.0f, 1.0f];
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderColor);

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

        this.lightPosition = new Vector3(2.0f, 8.0f, -1.5f);
        this.lightColor = new Vector3(0.6f, 0.4f, 0.2f);

        this.camera = new Camera(this, ClientSize.X, ClientSize.Y);

        this.loader = new ModelLoader();

        this.controller = new ImGuiController(this, ClientSize.X, ClientSize.Y);

        this.modelResource = loader.LoadResource("Resources\\Models\\Sponza\\sponza.obj");

        base.OnLoad();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var lightProjection = Matrix4.CreateOrthographicOffCenter(-40, 40, -40, 40, -40, 40);
        var lightView = Matrix4.LookAt(lightPosition, Vector3.Zero, Vector3.UnitY);
        var lightSpace = lightView * lightProjection;

        depthShader.Use();
        depthShader.SetMatrix4("lightSpaceMatrix", lightSpace);

        GL.Viewport(0, 0, ShadowWidth, ShadowHeight);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.depthMapFBO);

        GL.Clear(ClearBufferMask.DepthBufferBit);

        GL.CullFace(CullFaceMode.Front);

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
        shader.SetVector3("lightColor", this.lightColor);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, this.depthMap);

        this.RenderScene(this.shader);

        ImGui.Begin("Tools");

        var pos = new System.Numerics.Vector3(lightPosition.X, lightPosition.Y, lightPosition.Z);
        var col = new System.Numerics.Vector3(lightColor.X, lightColor.Y, lightColor.Z);

        ImGui.DragFloat3("Light Position", ref pos, 0.5f);
        ImGui.DragFloat3("Light Color", ref col, 0.1f);

        this.lightPosition = new Vector3(pos.X, pos.Y, pos.Z);
        this.lightColor = new Vector3(col.X, col.Y, col.Z);

        ImGui.End();

        this.controller.Render();

        this.SwapBuffers();
        base.OnRenderFrame(args);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        controller.Update(KeyboardState, MouseState, 8.6f);
        camera.Update(this.KeyboardState, this.MouseState);
        base.OnUpdateFrame(args);
    }

    private void RenderScene(ShaderProgram shader)
    {
        var model = Matrix4.CreateScale(0.01f);
        shader.SetMatrix4("model", model);

        foreach (var res in modelResource.Models)
        {
            var mesh = res.Mesh;
            var material = res.Texture;

            if (material == null)
            {
                texture.Bind(0);
            }
            else
            {
                material.Bind(0);
            }

            mesh.Draw();
        }
    }
}
