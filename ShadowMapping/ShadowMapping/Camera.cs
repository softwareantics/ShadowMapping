// <copyright file="Camera.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using System.Drawing;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public sealed class Camera
{
    private readonly float height;

    private readonly float speed = 0.5f;

    private readonly float width;

    private readonly GameWindow window;

    private bool isLocked;

    public Camera(GameWindow window, int width, int height)
    {
        this.window = window ?? throw new ArgumentNullException(nameof(window));

        this.width = width;
        this.height = height;
        this.Transform = new Transform()
        {
            Position = new Vector3(0, 50, 0),
            Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(45.0f)),
        };

        this.isLocked = false;
    }

    public Matrix4 Projection
    {
        get { return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(70.0f), this.width / this.height, 0.1f, 1000.0f); }
    }

    public Transform Transform { get; }

    public Matrix4 View
    {
        get { return this.Transform.CreateViewMatrix(Vector3.UnitY); }
    }

    private Vector2 CenterPosition
    {
        get
        {
            var viewport = new Rectangle(0, 0, (int)this.width, (int)this.height);
            var centerPosition = new Vector2(viewport.Width / 2, viewport.Height / 2);

            return centerPosition;
        }
    }

    public void Update(KeyboardState keyboard, MouseState mouse)
    {
        ArgumentNullException.ThrowIfNull(keyboard, nameof(keyboard));
        ArgumentNullException.ThrowIfNull(mouse, nameof(mouse));

        float moveAmount = this.speed;

        if (keyboard.IsKeyDown(Keys.W))
        {
            this.Transform.Translate(this.Transform.Forward, moveAmount);
        }

        if (keyboard.IsKeyDown(Keys.S))
        {
            this.Transform.Translate(this.Transform.Forward, -moveAmount);
        }

        if (keyboard.IsKeyDown(Keys.A))
        {
            this.Transform.Translate(this.Transform.Left, -moveAmount);
        }

        if (keyboard.IsKeyDown(Keys.D))
        {
            this.Transform.Translate(this.Transform.Left, moveAmount);
        }

        if (keyboard.IsKeyDown(Keys.Z))
        {
            this.Transform.Translate(this.Transform.Up, moveAmount);
        }

        if (keyboard.IsKeyDown(Keys.X))
        {
            this.Transform.Translate(this.Transform.Down, moveAmount);
        }

        if (keyboard.IsKeyReleased(Keys.Escape))
        {
            this.isLocked = false;
        }

        if (mouse.IsButtonReleased(MouseButton.Right))
        {
            this.window.MousePosition = new Vector2(this.CenterPosition.X, this.CenterPosition.Y);
            this.isLocked = true;
        }

        if (this.isLocked)
        {
            var deltaPosition = new Vector2(this.window.MousePosition.X - this.CenterPosition.X, this.window.MousePosition.Y - this.CenterPosition.Y);

            bool canRotateX = deltaPosition.X != 0;
            bool canRotateY = deltaPosition.Y != 0;

            if (canRotateX)
            {
                this.Transform.Rotate(this.Transform.Left, -MathHelper.DegreesToRadians(deltaPosition.Y * this.speed));
            }

            if (canRotateY)
            {
                this.Transform.Rotate(Vector3.UnitY, -MathHelper.DegreesToRadians(deltaPosition.X * this.speed));
            }

            if (canRotateX || canRotateY)
            {
                this.window.MousePosition = new Vector2(
                    this.CenterPosition.X,
                    this.CenterPosition.Y);
            }
        }
    }
}
