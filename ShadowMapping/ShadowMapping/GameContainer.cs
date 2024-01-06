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

public sealed class GameContainer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    private const int ShadowHeight = 2048;

    private const int ShadowWidth = 2048;

    private Camera camera;

    private ImGuiController controller;

    private int cubeVAO;

    private ShaderProgram debugShader;

    private int depthMap;

    private int depthMapFBO;

    private ShaderProgram depthShader;

    private Vector3 lightColor;

    private Vector3 lightPosition;

    private ModelLoader loader;

    private ModelResource modelResource;

    private int planeVAO;

    private int quadVAO;

    private int quadVBO;

    private ShaderProgram shader;

    private float temp = 0;

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

        this.woodTexture = Texture.LoadTexture("Resources\\Textures\\texture.png");

        this.depthMapFBO = GL.GenFramebuffer();
        this.depthMap = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, this.depthMap);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, ShadowWidth, ShadowHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
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

        this.modelResource = this.loader.LoadResource("Resources\\Models\\Sponza\\sponza.obj");
        this.controller = new ImGuiController(this, ClientSize.X, ClientSize.Y);
        base.OnLoad();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var lightTransform = new Transform()
        {
            Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(45.0f)),
        };

        var lightProjection = Matrix4.CreateOrthographicOffCenter(-40, 40, -40, 40, -40, 40);
        var lightView = Matrix4.LookAt(lightPosition, Vector3.Zero, Vector3.UnitY);
        var lightSpace = lightView * lightProjection;

        depthShader.Use();
        depthShader.SetMatrix4("lightSpaceMatrix", lightSpace);

        GL.Viewport(0, 0, ShadowWidth, ShadowHeight);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.depthMapFBO);

        GL.Clear(ClearBufferMask.DepthBufferBit);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Front);

        this.RenderScene(this.depthShader);

        GL.Disable(EnableCap.CullFace);

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

        var model = Matrix4.CreateScale(1f);
        shader.SetMatrix4("model", model);

        foreach (var res in modelResource.Models)
        {
            var mesh = res.Mesh;
            var material = res.Texture;

            if (material == null)
            {
                woodTexture.Bind(0);
            }
            else
            {
                material.Bind(0);
            }

            mesh.Draw();
        }

        //GL.BindVertexArray(this.planeVAO);
        //GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        //model = Matrix4.Identity;
        //model *= Matrix4.CreateTranslation(0, 1.5f, 0) * Matrix4.CreateScale(0.5f);
        //shader.SetMatrix4("model", model);

        //GL.BindVertexArray(this.cubeVAO);
        //GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        //model = Matrix4.Identity;
        //model *= Matrix4.CreateTranslation(10.0f, 4, 1.0f) * Matrix4.CreateScale(0.5f);
        //shader.SetMatrix4("model", model);

        //GL.BindVertexArray(this.cubeVAO);
        //GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }
}
