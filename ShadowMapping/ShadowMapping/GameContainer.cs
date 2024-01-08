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

    private float density = 0.113561f;

    private int depthMap;

    private int depthMapFBO;

    private ShaderProgram depthShader;

    private float end;

    private float exposure;

    private Vector3 fogColor = new Vector3(0.3f);

    private float gamma;

    private int hdrFrameBuffer;

    private int hdrTexture;

    private Vector3 lightColor;

    private Vector3 lightDirection;

    private ModelLoader loader;

    private ModelResource modelResource;

    private ShaderProgram shader;

    private float start;

    private Texture texture;

    private int type = 2;

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

        this.lightDirection = new Vector3(2.0f, 8.0f, -1.5f);
        this.lightColor = new Vector3(0.6f, 0.4f, 0.2f);

        this.camera = new Camera(this, ClientSize.X, ClientSize.Y);

        this.loader = new ModelLoader();

        this.controller = new ImGuiController(this, ClientSize.X, ClientSize.Y);

        this.modelResource = loader.LoadResource("Resources\\Models\\Sponza\\sponza.obj");

        this.exposure = 0.1f;
        this.gamma = 2.2f;

        base.OnLoad();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var lightProjection = Matrix4.CreateOrthographicOffCenter(-40, 40, -40, 40, -40, 40);
        var lightView = Matrix4.LookAt(lightDirection, Vector3.Zero, Vector3.UnitY);
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
        shader.SetVector3("lightDir", this.lightDirection);
        shader.SetMatrix4("lightSpaceMatrix", lightSpace);
        shader.SetVector3("lightColor", this.lightColor);

        shader.SetVector3("fog.color", fogColor);
        shader.SetFloat("fog.density", density);
        shader.SetFloat("fog.start", start);
        shader.SetFloat("fog.end", end);
        shader.SetInt("fog.type", type);
        shader.SetFloat("gamma", gamma);
        shader.SetFloat("exposure", exposure);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, this.depthMap);

        this.RenderScene(this.shader);

        ImGui.Begin("Tools");

        var pos = new System.Numerics.Vector3(lightDirection.X, lightDirection.Y, lightDirection.Z);
        var col = new System.Numerics.Vector3(lightColor.X, lightColor.Y, lightColor.Z);

        var fogCol = new System.Numerics.Vector3(fogColor.X, fogColor.Y, fogColor.Z);

        ImGui.DragFloat3("Light Position", ref pos, 0.5f);
        ImGui.ColorEdit3("Light Color", ref col);
        ImGui.ColorEdit3("Fog Color", ref fogCol);
        ImGui.DragInt("Fog Type", ref type, 0.1f);
        ImGui.DragFloat("Fog Desntiy", ref density, 0.01f);
        ImGui.DragFloat("Fog Start", ref start, 0.1f);
        ImGui.DragFloat("Fog End", ref end, 0.1f);
        ImGui.DragFloat("exposure", ref exposure, 0.1f);
        ImGui.DragFloat("gamma", ref gamma, 0.1f);

        this.lightDirection = new Vector3(pos.X, pos.Y, pos.Z);
        this.lightColor = new Vector3(col.X, col.Y, col.Z);
        this.fogColor = new Vector3(fogCol.X, fogCol.Y, fogCol.Z);

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
