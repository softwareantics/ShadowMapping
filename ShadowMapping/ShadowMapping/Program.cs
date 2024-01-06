// <copyright file="Program.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

internal static class Program
{
    internal static void Main()
    {
        var nativeWindowSettings = new NativeWindowSettings()
        {
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            AutoLoadBindings = true,
            ClientSize = new Vector2i(1280, 720),
            Flags = ContextFlags.ForwardCompatible,
            NumberOfSamples = 16,
            Profile = ContextProfile.Core,
            StartVisible = true,
            Title = "OpenTK Shadow Mapping",
            Vsync = VSyncMode.On,
            WindowBorder = WindowBorder.Fixed,
            WindowState = WindowState.Normal,
        };

        var gameWindowSettings = new GameWindowSettings()
        {
            UpdateFrequency = 60.0f,
        };

        using (var game = new GameContainer(gameWindowSettings, nativeWindowSettings))
        {
            game.Run();
        }
    }
}
