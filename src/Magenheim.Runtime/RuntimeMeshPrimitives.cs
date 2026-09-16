using System;
using Magenheim.Core.Geometry;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Unity boundary for Magenheim.Core's tested canonical solids. Runtime visuals request their
/// required proportions here instead of carrying private triangle builders whose winding can
/// diverge from the geometry authority.
/// </summary>
internal static class RuntimeMeshPrimitives
{
    internal static Mesh Box(string name) =>
        ToUnity(MeshPrimitives.Box(name));

    internal static Mesh Cylinder(int sides, string name) =>
        ToUnity(MeshPrimitives.Cylinder(sides, name: name));

    internal static Mesh Prism(
        int sides,
        string name,
        float lowerRadius = .46f,
        float lowerY = -.50f,
        float upperRadius = .50f,
        float upperY = .34f,
        float apexY = .58f,
        float baseY = -.52f) =>
        ToUnity(MeshPrimitives.Prism(
            sides,
            lowerRadius,
            lowerY,
            upperRadius,
            upperY,
            apexY,
            baseY,
            name));

    private static Mesh ToUnity(MeshPrimitive primitive)
    {
        if (primitive is null) throw new ArgumentNullException(nameof(primitive));
        var vertices = new Vector3[primitive.Vertices.Count];
        for (var i = 0; i < vertices.Length; i++)
        {
            var source = primitive.Vertices[i];
            vertices[i] = new Vector3(source.X, source.Y, source.Z);
        }

        var triangles = new int[primitive.Triangles.Count];
        for (var i = 0; i < triangles.Length; i++) triangles[i] = primitive.Triangles[i];

        var mesh = new Mesh
        {
            name = primitive.Name,
            vertices = vertices,
            triangles = triangles,
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
