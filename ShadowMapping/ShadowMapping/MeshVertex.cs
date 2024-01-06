// <copyright file="MeshVertex.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;

[StructLayout(LayoutKind.Sequential)]
public struct MeshVertex : IEquatable<MeshVertex>
{
    public static readonly int SizeInBytes = Marshal.SizeOf<MeshVertex>();

    public Vector3 Position { get; set; }

    public Vector3 Color { get; set; }

    public Vector2 TextureCoordinate { get; set; }

    public static bool operator ==(MeshVertex left, MeshVertex right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MeshVertex left, MeshVertex right)
    {
        return !(left == right);
    }

    public readonly bool Equals(MeshVertex other)
    {
        return this.Position == other.Position &&
               this.Color == other.Color &&
               this.TextureCoordinate == other.TextureCoordinate;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is MeshVertex vertex && this.Equals(vertex);
    }

    public override readonly int GetHashCode()
    {
        const int accumulator = 17;

        return (this.Position.GetHashCode() * accumulator) +
               (this.Color.GetHashCode() * accumulator) +
               (this.TextureCoordinate.GetHashCode() * accumulator);
    }
}
