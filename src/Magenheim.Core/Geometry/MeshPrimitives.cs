using System;
using System.Collections.Generic;

namespace Magenheim.Core.Geometry;

/// <summary>A position at the pure geometry boundary; the runtime converts it to a Unity vector.</summary>
public readonly record struct GeometryVector3(float X, float Y, float Z)
{
    public static GeometryVector3 Zero => new(0f, 0f, 0f);
}

/// <summary>
/// An indexed triangle surface. Winding is authoritative: for every triangle the normal
/// <c>cross(v1 - v0, v2 - v0)</c> must point out of the solid, which is the convention Unity's
/// <c>Mesh.RecalculateNormals</c> uses. Reversed winding produces inward normals, and a
/// back-face-culled material then renders the interior, so the viewer sees straight through the
/// near face onto the inside of the far one.
/// </summary>
public sealed class MeshPrimitive
{
    public MeshPrimitive(string name, IReadOnlyList<GeometryVector3> vertices, IReadOnlyList<int> triangles)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A mesh primitive requires a name.", nameof(name));
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
        if (Triangles.Count == 0 || Triangles.Count % 3 != 0)
            throw new ArgumentException($"Mesh primitive '{name}' must contain whole triangles.", nameof(triangles));
        foreach (var index in Triangles)
            if (index < 0 || index >= Vertices.Count)
                throw new ArgumentOutOfRangeException(nameof(triangles), $"Mesh primitive '{name}' references vertex {index}.");
        Name = name;
    }

    public string Name { get; }
    public IReadOnlyList<GeometryVector3> Vertices { get; }
    public IReadOnlyList<int> Triangles { get; }
}

/// <summary>
/// Canonical Magenheim solids. Every registrar builds its geometry from these so winding is
/// defined once and covered by deterministic tests, rather than copied between visual files.
/// </summary>
public static class MeshPrimitives
{
    public const int MinimumSides = 3;

    /// <summary>Unit cube centred on the origin.</summary>
    public static MeshPrimitive Box(string name = "magenheim.box")
    {
        var vertices = new[]
        {
            new GeometryVector3(-.5f, -.5f, -.5f), new GeometryVector3(.5f, -.5f, -.5f),
            new GeometryVector3(.5f, .5f, -.5f), new GeometryVector3(-.5f, .5f, -.5f),
            new GeometryVector3(-.5f, -.5f, .5f), new GeometryVector3(.5f, -.5f, .5f),
            new GeometryVector3(.5f, .5f, .5f), new GeometryVector3(-.5f, .5f, .5f)
        };
        var triangles = new[]
        {
            0, 3, 2, 0, 2, 1,
            4, 5, 6, 4, 6, 7,
            0, 4, 7, 0, 7, 3,
            1, 2, 6, 1, 6, 5,
            0, 1, 5, 0, 5, 4,
            3, 7, 6, 3, 6, 2
        };
        return new MeshPrimitive(name, vertices, triangles);
    }

    /// <summary>Closed cylinder with flat caps, centred on the Y axis.</summary>
    public static MeshPrimitive Cylinder(
        int sides,
        float radius = .5f,
        float bottomY = -.5f,
        float topY = .5f,
        string? name = null)
    {
        RequireSides(sides);
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius), "Cylinder radius must be positive.");
        if (topY <= bottomY) throw new ArgumentOutOfRangeException(nameof(topY), "Cylinder top must sit above its bottom.");

        var vertices = new List<GeometryVector3>(sides * 2 + 2);
        AppendRing(vertices, sides, radius, bottomY);
        AppendRing(vertices, sides, radius, topY);
        var bottomCentre = vertices.Count; vertices.Add(new GeometryVector3(0f, bottomY, 0f));
        var topCentre = vertices.Count; vertices.Add(new GeometryVector3(0f, topY, 0f));

        var triangles = new List<int>(sides * 12);
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            // Side quad, wound so the normal points away from the axis.
            triangles.Add(i); triangles.Add(sides + i); triangles.Add(next);
            triangles.Add(next); triangles.Add(sides + i); triangles.Add(sides + next);
            // Bottom cap faces -Y, top cap faces +Y.
            triangles.Add(bottomCentre); triangles.Add(i); triangles.Add(next);
            triangles.Add(topCentre); triangles.Add(sides + next); triangles.Add(sides + i);
        }
        return new MeshPrimitive(name ?? "magenheim.cylinder." + sides, vertices, triangles);
    }

    /// <summary>
    /// Crystal body: a tapered band between two rings closed by an apex above and a point below.
    /// This is the shape every crystal visual grew its own copy of.
    /// </summary>
    public static MeshPrimitive Prism(
        int sides,
        float lowerRadius = .46f,
        float lowerY = -.50f,
        float upperRadius = .50f,
        float upperY = .34f,
        float apexY = .58f,
        float baseY = -.52f,
        string? name = null)
    {
        RequireSides(sides);
        if (lowerRadius <= 0f) throw new ArgumentOutOfRangeException(nameof(lowerRadius), "Prism lower radius must be positive.");
        if (upperRadius <= 0f) throw new ArgumentOutOfRangeException(nameof(upperRadius), "Prism upper radius must be positive.");
        if (upperY <= lowerY) throw new ArgumentOutOfRangeException(nameof(upperY), "Prism upper ring must sit above the lower ring.");
        if (apexY <= upperY) throw new ArgumentOutOfRangeException(nameof(apexY), "Prism apex must sit above the upper ring.");
        if (baseY >= lowerY) throw new ArgumentOutOfRangeException(nameof(baseY), "Prism base point must sit below the lower ring.");

        var vertices = new List<GeometryVector3>(sides * 2 + 2);
        AppendRing(vertices, sides, lowerRadius, lowerY);
        AppendRing(vertices, sides, upperRadius, upperY);
        var apex = vertices.Count; vertices.Add(new GeometryVector3(0f, apexY, 0f));
        var basePoint = vertices.Count; vertices.Add(new GeometryVector3(0f, baseY, 0f));

        var triangles = new List<int>(sides * 12);
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.Add(basePoint); triangles.Add(i); triangles.Add(next);
            triangles.Add(i); triangles.Add(sides + i); triangles.Add(next);
            triangles.Add(next); triangles.Add(sides + i); triangles.Add(sides + next);
            triangles.Add(apex); triangles.Add(sides + next); triangles.Add(sides + i);
        }
        return new MeshPrimitive(name ?? "magenheim.prism." + sides, vertices, triangles);
    }

    private static void AppendRing(ICollection<GeometryVector3> vertices, int sides, float radius, float y)
    {
        for (var i = 0; i < sides; i++)
        {
            var angle = Math.PI * 2d * i / sides;
            vertices.Add(new GeometryVector3((float)(radius * Math.Cos(angle)), y, (float)(radius * Math.Sin(angle))));
        }
    }

    private static void RequireSides(int sides)
    {
        if (sides < MinimumSides)
            throw new ArgumentOutOfRangeException(nameof(sides), $"A solid of revolution needs at least {MinimumSides} sides.");
    }
}
