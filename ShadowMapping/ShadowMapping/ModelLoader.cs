// <copyright file="ModelLoader.cs" company="Software Antics">
//     Copyright (c) Software Antics. All rights reserved.
// </copyright>

namespace ShadowMapping;

using System;
using System.Collections.Generic;
using System.IO;
using Assimp;
using OpenTK.Mathematics;

public class Model
{
    public Mesh Mesh { get; set; }

    public Texture? Texture { get; set; }
}

public class ModelResource
{
    public IEnumerable<Model> Models { get; set; }
}

public class ModelLoader
{
    public ModelResource LoadResource(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException($"The specified {nameof(filePath)} parameter cannot be null, empty or consist of only whitespace characters.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The specified {nameof(filePath)} parameter cannot be located.", filePath);
        }

        using (var context = new AssimpContext())
        {
            var scene = context.ImportFile(
                filePath,
                PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace);

            if (scene == null || scene.SceneFlags.HasFlag(SceneFlags.Incomplete) || scene.RootNode == null)
            {
                throw new AssimpException($"Failed to load model using Assimp from path: '{filePath}'");
            }

            string? directory = Path.GetDirectoryName(filePath);

            return new ModelResource()
            {
                Models = this.ProcessNode(scene, scene.RootNode, directory),
            };
        }
    }

    private Texture? LoadMaterial(Assimp.Mesh mesh, List<Assimp.Material> materials, string? directory)
    {
        var result = new Material();

        if (mesh.MaterialIndex >= 0)
        {
            var assimpMaterial = materials[mesh.MaterialIndex];

            if (assimpMaterial.HasTextureDiffuse)
            {
                return Texture.LoadTexture("Resources\\Models\\Sponza\\" + assimpMaterial.TextureDiffuse.FilePath);
            }
        }

        return null;
    }

    private Model ProcessMesh(Scene scene, Assimp.Mesh mesh, string? directory)
    {
        var vertices = new List<MeshVertex>();
        var indices = new List<int>();

        if (mesh.HasVertices)
        {
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                MeshVertex vertex = default;

                vertex.Position = new Vector3(
                    mesh.Vertices[i].X,
                    mesh.Vertices[i].Y,
                    mesh.Vertices[i].Z);

                if (mesh.HasTextureCoords(0))
                {
                    vertex.TextureCoordinate = new Vector2(
                        mesh.TextureCoordinateChannels[0][i].X,
                        mesh.TextureCoordinateChannels[0][i].Y);
                }

                if (mesh.HasNormals)
                {
                    vertex.Normal = new Vector3(
                        mesh.Normals[i].X,
                        mesh.Normals[i].Y,
                        mesh.Normals[i].Z);
                }

                vertices.Add(vertex);
            }
        }

        for (int i = 0; i < mesh.FaceCount; i++)
        {
            var face = mesh.Faces[i];

            for (int j = 0; j < face.IndexCount; j++)
            {
                indices.Add(face.Indices[j]);
            }
        }

        return new Model()
        {
            Mesh = new Mesh([.. vertices], [.. indices]),
            Texture = this.LoadMaterial(mesh, scene.Materials, directory),
        };
    }

    private IList<Model> ProcessNode(Scene scene, Node node, string? directory)
    {
        var meshes = new List<Model>();

        for (int i = 0; i < node.MeshCount; i++)
        {
            var mesh = scene.Meshes[node.MeshIndices[i]];
            meshes.Add(this.ProcessMesh(scene, mesh, directory));
        }

        for (int i = 0; i < node.ChildCount; i++)
        {
            meshes.AddRange(this.ProcessNode(scene, node.Children[i], directory));
        }

        return meshes;
    }
}
