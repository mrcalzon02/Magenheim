using System;
using System.Collections.Generic;

namespace Magenheim.Core.Geometry;

/// <summary>
/// Deterministic surface checks for generated geometry.
///
/// Magenheim builds its world objects procedurally, so a reversed triangle produces no compile
/// error, no exception and no log line. It only shows up in-world as a piece you can see through
/// onto the inside of its far face. These checks turn that into a test failure.
/// </summary>
public static class MeshSurface
{
    /// <summary>
    /// A closed surface uses every edge exactly twice and in opposite directions. Two triangles
    /// traversing the same edge the same way means one of them is wound backwards relative to its
    /// neighbour.
    /// </summary>
    public static bool IsClosed(MeshPrimitive primitive, out string error)
    {
        if (primitive is null) throw new ArgumentNullException(nameof(primitive));
        var directed = new HashSet<(int From, int To)>();
        for (var i = 0; i < primitive.Triangles.Count; i += 3)
        {
            var a = primitive.Triangles[i];
            var b = primitive.Triangles[i + 1];
            var c = primitive.Triangles[i + 2];
            if (a == b || b == c || a == c)
            {
                error = $"{primitive.Name}: triangle {i / 3} is degenerate ({a},{b},{c}).";
                return false;
            }
            foreach (var edge in new[] { (a, b), (b, c), (c, a) })
            {
                if (!directed.Add(edge))
                {
                    error = $"{primitive.Name}: edge {edge.Item1}->{edge.Item2} is traversed twice in the same direction, so adjacent triangles disagree on winding.";
                    return false;
                }
            }
        }

        foreach (var edge in directed)
        {
            if (!directed.Contains((edge.To, edge.From)))
            {
                error = $"{primitive.Name}: edge {edge.From}->{edge.To} has no opposing triangle, so the surface is not closed.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Signed volume under the convention that <c>cross(v1 - v0, v2 - v0)</c> points outward.
    /// A correctly wound closed solid yields a positive value; an inside-out one yields its
    /// negation.
    /// </summary>
    public static double SignedVolume(MeshPrimitive primitive)
    {
        if (primitive is null) throw new ArgumentNullException(nameof(primitive));
        var total = 0d;
        for (var i = 0; i < primitive.Triangles.Count; i += 3)
        {
            var a = primitive.Vertices[primitive.Triangles[i]];
            var b = primitive.Vertices[primitive.Triangles[i + 1]];
            var c = primitive.Vertices[primitive.Triangles[i + 2]];
            total += Determinant(a, b, c);
        }
        return total / 6d;
    }

    /// <summary>
    /// Every face normal must point away from the solid's interior. Checked against the mesh
    /// centroid, which is decisive for the convex solids Magenheim generates and complements the
    /// signed-volume test rather than restating it.
    /// </summary>
    public static bool FacesPointOutward(MeshPrimitive primitive, out string error)
    {
        if (primitive is null) throw new ArgumentNullException(nameof(primitive));
        var centre = Centroid(primitive);
        for (var i = 0; i < primitive.Triangles.Count; i += 3)
        {
            var a = primitive.Vertices[primitive.Triangles[i]];
            var b = primitive.Vertices[primitive.Triangles[i + 1]];
            var c = primitive.Vertices[primitive.Triangles[i + 2]];
            var normal = Cross(Subtract(b, a), Subtract(c, a));
            var faceCentre = new GeometryVector3((a.X + b.X + c.X) / 3f, (a.Y + b.Y + c.Y) / 3f, (a.Z + b.Z + c.Z) / 3f);
            if (Dot(normal, Subtract(faceCentre, centre)) <= 0d)
            {
                error = $"{primitive.Name}: triangle {i / 3} ({primitive.Triangles[i]},{primitive.Triangles[i + 1]},{primitive.Triangles[i + 2]}) faces inward.";
                return false;
            }
        }
        error = string.Empty;
        return true;
    }

    /// <summary>Runs every surface check; the single call registrars and tests should use.</summary>
    public static bool IsRenderable(MeshPrimitive primitive, out string error)
    {
        if (!IsClosed(primitive, out error)) return false;
        if (SignedVolume(primitive) <= 0d)
        {
            error = $"{primitive.Name}: signed volume is not positive, so the solid is inside-out.";
            return false;
        }
        return FacesPointOutward(primitive, out error);
    }

    private static GeometryVector3 Centroid(MeshPrimitive primitive)
    {
        double x = 0, y = 0, z = 0;
        foreach (var vertex in primitive.Vertices) { x += vertex.X; y += vertex.Y; z += vertex.Z; }
        var count = primitive.Vertices.Count;
        return new GeometryVector3((float)(x / count), (float)(y / count), (float)(z / count));
    }

    private static GeometryVector3 Subtract(GeometryVector3 left, GeometryVector3 right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    private static GeometryVector3 Cross(GeometryVector3 left, GeometryVector3 right) =>
        new(left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);

    private static double Dot(GeometryVector3 left, GeometryVector3 right) =>
        (double)left.X * right.X + (double)left.Y * right.Y + (double)left.Z * right.Z;

    private static double Determinant(GeometryVector3 a, GeometryVector3 b, GeometryVector3 c) =>
        (double)a.X * ((double)b.Y * c.Z - (double)b.Z * c.Y)
        - (double)a.Y * ((double)b.X * c.Z - (double)b.Z * c.X)
        + (double)a.Z * ((double)b.X * c.Y - (double)b.Y * c.X);
}
