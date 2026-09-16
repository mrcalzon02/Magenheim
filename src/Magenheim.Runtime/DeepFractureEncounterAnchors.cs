using System;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DeepFractureEncounterAnchors
{
    private const string RootName = "Magenheim_EncounterAnchors";

    internal static Transform Resolve(Transform district, string pieceFamilyId, CreatureChassis chassis, int groupIndex, int unitIndex, int groupSize)
    {
        if (district is null) throw new ArgumentNullException(nameof(district));
        var root = EnsureRoot(district, pieceFamilyId);
        var role = Role(chassis);
        var roleRoot = FindOrCreate(root, role);
        var anchorName = $"{role}_{groupIndex:00}_{unitIndex:00}";
        var existing = roleRoot.Find(anchorName);
        if (existing is not null) return existing;

        var pose = Pose(pieceFamilyId, chassis, groupIndex, unitIndex, groupSize);
        var anchor = new GameObject(anchorName).transform;
        anchor.SetParent(roleRoot, false);
        anchor.localPosition = pose.Position;
        anchor.localRotation = Quaternion.Euler(0f, pose.YawDegrees, 0f);
        return anchor;
    }

    private static Transform EnsureRoot(Transform district, string pieceFamilyId)
    {
        var existing = district.Find(RootName);
        if (existing is not null) return existing;
        _ = Profile(pieceFamilyId); // fail closed before mutating the hierarchy
        var root = new GameObject(RootName).transform;
        root.SetParent(district, false);
        return root;
    }

    private static Transform FindOrCreate(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing is not null) return existing;
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static string Role(CreatureChassis chassis) => chassis switch
    {
        CreatureChassis.FacetSentry or CreatureChassis.ObeliskWarden => "Control",
        CreatureChassis.StoneSentinel or CreatureChassis.StoneGuardian or CreatureChassis.CrystalGolem or CreatureChassis.DeepColossus => "Heavy",
        CreatureChassis.Burrower or CreatureChassis.CrystalParasite => "Ambush",
        CreatureChassis.AnnoyanceWisp => "Air",
        CreatureChassis.CrystalHound or CreatureChassis.Shardling => "Flank",
        _ => "Line"
    };

    private static SpawnPose Pose(string pieceFamilyId, CreatureChassis chassis, int groupIndex, int unitIndex, int groupSize)
    {
        var profile = Profile(pieceFamilyId);
        var lane = unitIndex - (groupSize - 1) * .5f;
        var flank = lane * profile.Spacing;
        var depth = ((unitIndex & 1) == 0 ? 1f : -1f) * profile.DepthSpread;
        var position = profile.Center + new Vector3(flank, .35f, depth);
        var yaw = FaceCenter(position);

        switch (chassis)
        {
            case CreatureChassis.FacetSentry:
            case CreatureChassis.ObeliskWarden:
                position = profile.Center + Radial(groupIndex, unitIndex, profile.EdgeRadius);
                yaw = FaceCenter(position);
                break;
            case CreatureChassis.StoneSentinel:
            case CreatureChassis.StoneGuardian:
            case CreatureChassis.CrystalGolem:
            case CreatureChassis.DeepColossus:
                position = profile.Center + new Vector3(flank * .45f, .35f, depth * .25f);
                yaw = FaceCenter(position);
                break;
            case CreatureChassis.Burrower:
            case CreatureChassis.CrystalParasite:
                position = profile.Center + Radial(groupIndex + 3, unitIndex, profile.EdgeRadius * .82f);
                yaw = FaceCenter(position);
                break;
            case CreatureChassis.AnnoyanceWisp:
                position.y = 2.8f + (unitIndex % 3) * .7f;
                break;
            case CreatureChassis.CrystalHound:
            case CreatureChassis.Shardling:
                position.x += ((unitIndex & 1) == 0 ? -1f : 1f) * 4f;
                yaw = FaceCenter(position);
                break;
        }
        return new SpawnPose(position, yaw);
    }

    private static float FaceCenter(Vector3 position) => Mathf.Atan2(-position.x, -position.z) * Mathf.Rad2Deg;

    private static Vector3 Radial(int groupIndex, int unitIndex, float radius)
    {
        var angle = ((groupIndex * 137.5f) + (unitIndex * 61f)) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle) * radius, .35f, Mathf.Sin(angle) * radius);
    }

    private static DistrictProfile Profile(string id) => id switch
    {
        "DF-01" => new(new Vector3(0f,0f,12f),5.5f,6f,24f), "DF-02" => new(Vector3.zero,5f,10f,27f),
        "DF-03" => new(Vector3.zero,6f,7f,29f), "DF-04" => new(new Vector3(0f,0f,3f),5f,14f,25f),
        "DF-05" => new(Vector3.zero,6f,9f,29f), "DF-06" => new(Vector3.zero,7f,5f,25f),
        "DF-07" => new(Vector3.zero,5f,13f,27f), "DF-08" => new(Vector3.zero,7f,5f,24f),
        "DF-09" => new(Vector3.zero,5f,12f,31f), "DF-10" => new(Vector3.zero,5f,11f,30f),
        "DF-11" => new(Vector3.zero,6f,7f,25f), "DF-12" => new(Vector3.zero,7f,5f,25f),
        "DF-13" => new(Vector3.zero,5f,10f,29f), "DF-14" => new(Vector3.zero,5f,9f,28f),
        "DF-15" => new(Vector3.zero,6f,8f,27f), "DF-16" => new(Vector3.zero,6f,7f,26f),
        "DF-17" => new(Vector3.zero,5f,12f,30f), "DF-18" => new(Vector3.zero,5f,10f,29f),
        "DF-19" => new(Vector3.zero,6f,8f,27f), "DF-20" => new(Vector3.zero,9f,4f,30f),
        _ => throw new InvalidOperationException($"No encounter anchor profile exists for Deep Fracture district '{id}'.")
    };

    private readonly struct SpawnPose
    {
        internal SpawnPose(Vector3 position, float yawDegrees) { Position = position; YawDegrees = yawDegrees; }
        internal Vector3 Position { get; }
        internal float YawDegrees { get; }
    }

    private readonly struct DistrictProfile
    {
        internal DistrictProfile(Vector3 center, float spacing, float depthSpread, float edgeRadius) { Center = center; Spacing = spacing; DepthSpread = depthSpread; EdgeRadius = edgeRadius; }
        internal Vector3 Center { get; }
        internal float Spacing { get; }
        internal float DepthSpread { get; }
        internal float EdgeRadius { get; }
    }
}
