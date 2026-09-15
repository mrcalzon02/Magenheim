using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.DeepFractures;

public sealed record DeepFracturePassageSegment(
    string ConnectionId,
    int SegmentIndex,
    DeepFractureInteriorPoint Center,
    double Length,
    double YawDegrees);

public sealed record DeepFracturePassageAssemblyPlan(
    IReadOnlyList<DeepFracturePassageSegment> Segments)
{
    public IReadOnlyList<DeepFracturePassageSegment> ForConnection(string connectionId)
        => Segments.Where(segment => string.Equals(segment.ConnectionId, connectionId, StringComparison.Ordinal)).ToArray();
}

public static class DeepFracturePassageAssemblyCompiler
{
    public const double RuntimePassageLength = 16d;

    public static DeepFracturePassageAssemblyPlan Build(
        DeepFractureInteriorBlueprint blueprint,
        DeepFractureInteriorProjectionPolicy? policyOverride = null)
    {
        if (blueprint is null)
            throw new ArgumentNullException(nameof(blueprint));

        var routes = DeepFracturePassageRouter.Build(blueprint, policyOverride);
        var segments = new List<DeepFracturePassageSegment>();
        foreach (var route in routes)
        {
            var segmentIndex = 0;
            for (var leg = 0; leg + 1 < route.Points.Count; leg++)
            {
                var from = route.Points[leg];
                var to = route.Points[leg + 1];
                var dx = to.X - from.X;
                var dz = to.Z - from.Z;
                var length = Math.Sqrt((dx * dx) + (dz * dz));
                if (length <= 0.000001d)
                    continue;

                var count = Math.Max(1, (int)Math.Ceiling(length / RuntimePassageLength));
                var yaw = Math.Atan2(dx, dz) * (180d / Math.PI);
                for (var index = 0; index < count; index++)
                {
                    var t0 = (double)index / count;
                    var t1 = (double)(index + 1) / count;
                    var midpoint = (t0 + t1) * .5d;
                    var center = new DeepFractureInteriorPoint(
                        from.X + dx * midpoint,
                        from.Y + (to.Y - from.Y) * midpoint,
                        from.Z + dz * midpoint);
                    segments.Add(new DeepFracturePassageSegment(
                        route.ConnectionId,
                        segmentIndex++,
                        center,
                        length / count,
                        yaw));
                }
            }
        }

        return new DeepFracturePassageAssemblyPlan(segments.AsReadOnly());
    }
}