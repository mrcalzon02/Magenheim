using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.DeepFractures;

public sealed record DeepFracturePassageRoute(
    string ConnectionId,
    IReadOnlyList<DeepFractureInteriorPoint> Points);

public static class DeepFracturePassageRouter
{
    public static IReadOnlyList<DeepFracturePassageRoute> Build(
        DeepFractureInteriorBlueprint blueprint,
        DeepFractureInteriorProjectionPolicy? policyOverride = null)
    {
        if (blueprint is null)
            throw new ArgumentNullException(nameof(blueprint));

        var policy = policyOverride ?? DeepFractureInteriorProjectionPolicy.Default;
        policy.Validate();
        var modules = blueprint.Modules.ToDictionary(module => module.ModuleInstanceId, StringComparer.Ordinal);
        var routes = new List<DeepFracturePassageRoute>();

        foreach (var connection in blueprint.Connections.Where(connection => connection.RuntimeMode == DeepFractureRuntimeConnectionMode.PhysicalPassage))
        {
            if (!modules.TryGetValue(connection.FromModuleId, out var from)
                || !modules.TryGetValue(connection.ToModuleId, out var to))
                throw new InvalidOperationException($"Physical connection '{connection.Id}' references an unplaced district.");

            var points = Route(connection.Id, from.Center, to.Center, blueprint.Modules, from.ModuleInstanceId, to.ModuleInstanceId, policy);
            routes.Add(new DeepFracturePassageRoute(connection.Id, points));
        }

        return routes;
    }

    private static IReadOnlyList<DeepFractureInteriorPoint> Route(
        string connectionId,
        DeepFractureInteriorPoint from,
        DeepFractureInteriorPoint to,
        IReadOnlyList<DeepFractureInteriorModulePlacement> modules,
        string fromId,
        string toId,
        DeepFractureInteriorProjectionPolicy policy)
    {
        var direct = new[] { from, to };
        if (Clear(direct, modules, fromId, toId, policy))
            return direct;

        var xFirst = new[] { from, new DeepFractureInteriorPoint(to.X, from.Y, from.Z), to };
        if (Clear(xFirst, modules, fromId, toId, policy))
            return Compact(xFirst);

        var zFirst = new[] { from, new DeepFractureInteriorPoint(from.X, from.Y, to.Z), to };
        if (Clear(zFirst, modules, fromId, toId, policy))
            return Compact(zFirst);

        var clearance = (policy.ModuleFootprint * .5d) + 8d;
        var minX = modules.Min(module => module.Center.X) - clearance;
        var maxX = modules.Max(module => module.Center.X) + clearance;
        var minZ = modules.Min(module => module.Center.Z) - clearance;
        var maxZ = modules.Max(module => module.Center.Z) + clearance;
        var candidates = new[]
        {
            new[] { from, new DeepFractureInteriorPoint(minX, from.Y, from.Z), new DeepFractureInteriorPoint(minX, to.Y, to.Z), to },
            new[] { from, new DeepFractureInteriorPoint(maxX, from.Y, from.Z), new DeepFractureInteriorPoint(maxX, to.Y, to.Z), to },
            new[] { from, new DeepFractureInteriorPoint(from.X, from.Y, minZ), new DeepFractureInteriorPoint(to.X, to.Y, minZ), to },
            new[] { from, new DeepFractureInteriorPoint(from.X, from.Y, maxZ), new DeepFractureInteriorPoint(to.X, to.Y, maxZ), to },
        };

        foreach (var candidate in candidates)
        {
            var compact = Compact(candidate);
            if (Clear(compact, modules, fromId, toId, policy))
                return compact;
        }

        throw new InvalidOperationException($"No collision-safe physical route exists for Deep Fracture connection '{connectionId}'.");
    }

    private static IReadOnlyList<DeepFractureInteriorPoint> Compact(IReadOnlyList<DeepFractureInteriorPoint> points)
    {
        var result = new List<DeepFractureInteriorPoint>();
        foreach (var point in points)
        {
            if (result.Count == 0 || result[result.Count - 1] != point)
                result.Add(point);
        }
        return result;
    }

    private static bool Clear(
        IReadOnlyList<DeepFractureInteriorPoint> points,
        IReadOnlyList<DeepFractureInteriorModulePlacement> modules,
        string fromId,
        string toId,
        DeepFractureInteriorProjectionPolicy policy)
    {
        for (var segment = 0; segment + 1 < points.Count; segment++)
        {
            var a = points[segment];
            var b = points[segment + 1];
            foreach (var module in modules)
            {
                if (string.Equals(module.ModuleInstanceId, fromId, StringComparison.Ordinal)
                    || string.Equals(module.ModuleInstanceId, toId, StringComparison.Ordinal))
                    continue;

                if (SegmentIntersectsFootprint(a, b, module.Center, policy.ModuleFootprint * .5d + 4d))
                    return false;
            }
        }
        return true;
    }

    internal static bool SegmentIntersectsFootprint(
        DeepFractureInteriorPoint a,
        DeepFractureInteriorPoint b,
        DeepFractureInteriorPoint center,
        double halfExtent)
    {
        var dx = b.X - a.X;
        var dz = b.Z - a.Z;
        var lengthSquared = dx * dx + dz * dz;
        var t = lengthSquared <= 0d ? 0d : ((center.X - a.X) * dx + (center.Z - a.Z) * dz) / lengthSquared;
        t = Math.Max(0d, Math.Min(1d, t));
        var x = a.X + dx * t;
        var z = a.Z + dz * t;
        return Math.Abs(x - center.X) < halfExtent && Math.Abs(z - center.Z) < halfExtent;
    }
}