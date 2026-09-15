using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Geometry;

internal static class MeshGeometryTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Mesh geometry assertion {assertions} failed: {message}");
        }

        // The box was the one primitive shipped with correct winding; it anchors the convention.
        var box = MeshPrimitives.Box();
        Assert(box.Vertices.Count == 8, "A box has eight vertices.");
        Assert(box.Triangles.Count == 36, "A box has twelve triangles.");
        Assert(MeshSurface.IsRenderable(box, out var boxError), $"Box must be renderable: {boxError}");
        Assert(Math.Abs(MeshSurface.SignedVolume(box) - 1d) < 1e-5, "A unit box must enclose unit volume.");

        // Solids of revolution across the full range of side counts the visuals use.
        foreach (var sides in new[] { 3, 4, 5, 6, 7, 8, 10, 12, 16, 24, 32 })
        {
            var cylinder = MeshPrimitives.Cylinder(sides);
            Assert(MeshSurface.IsRenderable(cylinder, out var cylinderError),
                $"Cylinder with {sides} sides must be renderable: {cylinderError}");
            Assert(cylinder.Vertices.Count == sides * 2 + 2, $"Cylinder with {sides} sides needs two rings plus two cap centres.");

            var prism = MeshPrimitives.Prism(sides);
            Assert(MeshSurface.IsRenderable(prism, out var prismError),
                $"Prism with {sides} sides must be renderable: {prismError}");
            Assert(prism.Vertices.Count == sides * 2 + 2, $"Prism with {sides} sides needs two rings plus apex and base.");
        }

        // Volume must grow with radius and height; guards against a primitive that validates
        // while being geometrically degenerate.
        var narrow = MeshSurface.SignedVolume(MeshPrimitives.Cylinder(24, radius: .25f));
        var wide = MeshSurface.SignedVolume(MeshPrimitives.Cylinder(24, radius: .5f));
        Assert(wide > narrow * 3d, "Doubling cylinder radius must roughly quadruple enclosed volume.");

        // Exact volume of a unit-height prism on an inscribed regular n-gon of radius r is
        // (1/2)*n*r^2*sin(2*pi/n). Comparing against that, rather than against pi/4, keeps the
        // tolerance tight and independent of how finely the ring is subdivided.
        foreach (var sides in new[] { 8, 24, 64 })
        {
            const double radius = .5d;
            var expected = .5d * sides * radius * radius * Math.Sin(2d * Math.PI / sides);
            Assert(Math.Abs(MeshSurface.SignedVolume(MeshPrimitives.Cylinder(sides)) - expected) < 1e-5,
                $"Cylinder with {sides} sides must enclose the exact inscribed-polygon volume {expected:F6}.");
        }

        // ---- Regression: the winding actually shipped in 0.0.49 and earlier ----
        // Eleven visual files carried this copy-pasted prism/cylinder winding. It produces inward
        // normals, so back-face culling renders the interior and the piece reads as see-through.
        // These assertions fail if the validator ever stops catching it.
        var shipped = ShippedInsideOutPrism(6);
        Assert(!MeshSurface.IsRenderable(shipped, out var shippedError),
            "The shipped inside-out prism winding must be rejected.");
        Assert(shippedError.Contains("inside-out") || shippedError.Contains("faces inward"),
            $"Rejection must name the winding defect, but reported: {shippedError}");
        Assert(MeshSurface.SignedVolume(shipped) < 0d,
            "The shipped winding must produce negative signed volume.");
        Assert(MeshSurface.IsClosed(shipped, out _),
            "The shipped winding is closed but inverted, so closedness alone cannot detect it.");

        // Reversing each triangle of the shipped mesh must recover a valid solid, proving the
        // defect was winding alone and not the vertex positions.
        Assert(MeshSurface.IsRenderable(ReverseWinding(shipped), out var recoveredError),
            $"Reversing the shipped winding must yield a renderable solid: {recoveredError}");

        // ---- Argument guards ----
        Assert(Throws(() => MeshPrimitives.Cylinder(2)), "Fewer than three sides must be rejected.");
        Assert(Throws(() => MeshPrimitives.Cylinder(8, radius: 0f)), "Zero radius must be rejected.");
        Assert(Throws(() => MeshPrimitives.Cylinder(8, bottomY: 1f, topY: 0f)), "Inverted cylinder extents must be rejected.");
        Assert(Throws(() => MeshPrimitives.Prism(8, apexY: 0f, upperY: .5f)), "An apex below the upper ring must be rejected.");
        Assert(Throws(() => MeshPrimitives.Prism(8, baseY: 0f, lowerY: -.5f)), "A base point above the lower ring must be rejected.");
        Assert(Throws(() => new MeshPrimitive("bad", new[] { GeometryVector3.Zero }, new[] { 0, 1 })),
            "A triangle list that is not a multiple of three must be rejected.");
        Assert(Throws(() => new MeshPrimitive("bad", new[] { GeometryVector3.Zero }, new[] { 0, 1, 2 })),
            "Out-of-range vertex indices must be rejected.");

        return assertions;
    }

    /// <summary>Reproduces the exact triangle order shipped in the eleven duplicated builders.</summary>
    private static MeshPrimitive ShippedInsideOutPrism(int sides)
    {
        var vertices = new List<GeometryVector3>();
        for (var i = 0; i < sides; i++)
        {
            var angle = Math.PI * 2d * i / sides;
            vertices.Add(new GeometryVector3((float)(.46d * Math.Cos(angle)), -.50f, (float)(.46d * Math.Sin(angle))));
        }
        for (var i = 0; i < sides; i++)
        {
            var angle = Math.PI * 2d * i / sides;
            vertices.Add(new GeometryVector3((float)(.50d * Math.Cos(angle)), .34f, (float)(.50d * Math.Sin(angle))));
        }
        var top = vertices.Count; vertices.Add(new GeometryVector3(0f, .58f, 0f));
        var bottom = vertices.Count; vertices.Add(new GeometryVector3(0f, -.52f, 0f));

        var triangles = new List<int>();
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[]
            {
                bottom, next, i,
                i, next, sides + i,
                next, sides + next, sides + i,
                sides + i, sides + next, top
            });
        }
        return new MeshPrimitive("shipped.inside-out.prism", vertices, triangles);
    }

    private static MeshPrimitive ReverseWinding(MeshPrimitive primitive)
    {
        var triangles = new List<int>(primitive.Triangles.Count);
        for (var i = 0; i < primitive.Triangles.Count; i += 3)
        {
            triangles.Add(primitive.Triangles[i]);
            triangles.Add(primitive.Triangles[i + 2]);
            triangles.Add(primitive.Triangles[i + 1]);
        }
        return new MeshPrimitive(primitive.Name + ".reversed", primitive.Vertices.ToArray(), triangles);
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (ArgumentException) { return true; }
    }
}
