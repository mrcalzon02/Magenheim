using System;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DeepFractureEncounterAnchors
{
    private const string RootName = "Magenheim_EncounterAnchors";
    private const int AuthoredSlotsPerRole = 16;

    internal static void BakePrefab(Transform district, string pieceFamilyId)
    {
        if (district is null) throw new ArgumentNullException(nameof(district));
        var root = EnsureRoot(district, pieceFamilyId);
        if (!IsAuthoredDistrict(pieceFamilyId)) return;
        foreach (var role in new[] { "Control", "Heavy", "Ambush", "Air", "Flank", "Line" })
        {
            var roleRoot = FindOrCreate(root, role);
            for (var slot = 0; slot < AuthoredSlotsPerRole; slot++)
            {
                var name = $"Authored_{slot:00}";
                if (roleRoot.Find(name) is not null) continue;
                var pose = AuthoredPose(pieceFamilyId, role, slot);
                var anchor = new GameObject(name).transform;
                anchor.SetParent(roleRoot, false);
                anchor.localPosition = pose.Position;
                anchor.localRotation = Quaternion.Euler(0f, pose.YawDegrees, 0f);
            }
        }
    }

    internal static Transform Resolve(Transform district, string pieceFamilyId, CreatureChassis chassis, int groupIndex, int unitIndex, int groupSize)
    {
        if (district is null) throw new ArgumentNullException(nameof(district));
        var root = EnsureRoot(district, pieceFamilyId);
        var role = Role(chassis);
        var roleRoot = FindOrCreate(root, role);
        if (IsAuthoredDistrict(pieceFamilyId))
        {
            var slot = (groupIndex * 7 + unitIndex) % AuthoredSlotsPerRole;
            return roleRoot.Find($"Authored_{slot:00}") ?? throw new InvalidOperationException($"Deep Fracture prefab '{pieceFamilyId}' is missing authored {role} encounter anchor {slot:00}.");
        }
        var anchorName = $"{role}_{groupIndex:00}_{unitIndex:00}";
        var existing = roleRoot.Find(anchorName);
        if (existing is not null) return existing;
        var pose = ProceduralPose(pieceFamilyId, chassis, groupIndex, unitIndex, groupSize);
        var anchor = new GameObject(anchorName).transform;
        anchor.SetParent(roleRoot, false);
        anchor.localPosition = pose.Position;
        anchor.localRotation = Quaternion.Euler(0f, pose.YawDegrees, 0f);
        return anchor;
    }

    private static bool IsAuthoredDistrict(string id) => id is "DF-01" or "DF-02" or "DF-03" or "DF-04" or "DF-05" or "DF-06" or "DF-07" or "DF-08" or "DF-09" or "DF-10";

    private static SpawnPose AuthoredPose(string id, string role, int slot)
    {
        var ring = slot % 8; var layer = slot / 8; var angle = ring * 45f * Mathf.Deg2Rad;
        var featureRadius = id switch { "DF-01" => 19f, "DF-02" => 25f, "DF-03" => 24f, "DF-04" => 27f, "DF-05" => 25f, "DF-06" => 25f, "DF-07" => 27f, "DF-08" => 23f, "DF-09" => 30f, "DF-10" => 29f, _ => 22f };
        var center = id switch { "DF-01" => new Vector3(0f,0f,10f), "DF-04" => new Vector3(0f,0f,3f), _ => Vector3.zero };
        var radius = featureRadius + layer * 7f;
        var position = center + new Vector3(Mathf.Cos(angle) * radius, .35f, Mathf.Sin(angle) * radius);
        switch (role)
        {
            case "Air": position.y = 4.2f + layer * 1.4f; break;
            case "Heavy": position = Vector3.Lerp(center + Vector3.up * .35f, position, .42f); break;
            case "Control": position = center + new Vector3(Mathf.Cos(angle) * (radius + 4f), .6f, Mathf.Sin(angle) * (radius + 4f)); break;
            case "Ambush": position = center + new Vector3(Mathf.Cos(angle) * (radius + 2f), .05f, Mathf.Sin(angle) * (radius + 2f)); break;
            case "Flank": position += new Vector3(Mathf.Sin(angle) * 4f, 0f, -Mathf.Cos(angle) * 4f); break;
        }
        if (id == "DF-01") position.z = Mathf.Clamp(position.z, -18f, 30f);
        if (id == "DF-02") position.x += position.z > 0f ? -5f : 5f;
        if (id == "DF-03" && role == "Heavy") position *= .72f;
        if (id == "DF-04") position.x = Mathf.Abs(position.x) < 12f ? (position.x < 0f ? -18f : 18f) : position.x;
        if (id == "DF-05" && role == "Ambush") position.z = (slot & 1) == 0 ? 23f : -23f;
        if (id == "DF-06")
        {
            if (role == "Heavy") position = new Vector3((ring - 3.5f) * 2.2f, .35f, layer == 0 ? 3f : -5f);
            else if (role == "Ambush") position = new Vector3((ring & 1) == 0 ? -24f : 24f, .05f, (ring - 3.5f) * 6f);
        }
        if (id == "DF-07")
        {
            position.z += (ring & 1) == 0 ? 7f : -7f;
            if (role == "Air") position.y = 6f + layer * 1.5f;
        }
        if (id == "DF-08")
        {
            if (role == "Heavy") position = new Vector3((ring - 3.5f) * 1.8f, .35f, 9f - layer * 8f);
            if (role == "Control") position = new Vector3((ring & 1) == 0 ? -27f : 27f, 1.1f, (ring - 3.5f) * 5f);
        }
        if (id == "DF-09" || id == "DF-10")
        {
            if (role == "Control") { position.y = 4.8f + layer * 2.2f; position.x *= .92f; position.z *= .92f; }
            if (role == "Flank") position = center + new Vector3((ring & 1) == 0 ? -21f : 21f, .35f, (ring - 3.5f) * 7f);
        }
        return new SpawnPose(position, Face(center, position));
    }

    private static Transform EnsureRoot(Transform district, string pieceFamilyId) { var existing = district.Find(RootName); if (existing is not null) return existing; _ = Profile(pieceFamilyId); var root = new GameObject(RootName).transform; root.SetParent(district, false); return root; }
    private static Transform FindOrCreate(Transform parent, string name) { var existing = parent.Find(name); if (existing is not null) return existing; var child = new GameObject(name).transform; child.SetParent(parent, false); return child; }
    private static string Role(CreatureChassis chassis) => chassis switch { CreatureChassis.FacetSentry or CreatureChassis.ObeliskWarden => "Control", CreatureChassis.StoneSentinel or CreatureChassis.StoneGuardian or CreatureChassis.CrystalGolem or CreatureChassis.DeepColossus => "Heavy", CreatureChassis.Burrower or CreatureChassis.CrystalParasite => "Ambush", CreatureChassis.AnnoyanceWisp => "Air", CreatureChassis.CrystalHound or CreatureChassis.Shardling => "Flank", _ => "Line" };

    private static SpawnPose ProceduralPose(string pieceFamilyId, CreatureChassis chassis, int groupIndex, int unitIndex, int groupSize)
    {
        var profile = Profile(pieceFamilyId); var lane = unitIndex - (groupSize - 1) * .5f; var flank = lane * profile.Spacing; var depth = ((unitIndex & 1) == 0 ? 1f : -1f) * profile.DepthSpread; var position = profile.Center + new Vector3(flank, .35f, depth); var yaw = Face(Vector3.zero, position);
        switch (chassis) { case CreatureChassis.FacetSentry: case CreatureChassis.ObeliskWarden: position = profile.Center + Radial(groupIndex, unitIndex, profile.EdgeRadius); yaw = Face(Vector3.zero, position); break; case CreatureChassis.StoneSentinel: case CreatureChassis.StoneGuardian: case CreatureChassis.CrystalGolem: case CreatureChassis.DeepColossus: position = profile.Center + new Vector3(flank * .45f, .35f, depth * .25f); yaw = Face(Vector3.zero, position); break; case CreatureChassis.Burrower: case CreatureChassis.CrystalParasite: position = profile.Center + Radial(groupIndex + 3, unitIndex, profile.EdgeRadius * .82f); yaw = Face(Vector3.zero, position); break; case CreatureChassis.AnnoyanceWisp: position.y = 2.8f + (unitIndex % 3) * .7f; break; case CreatureChassis.CrystalHound: case CreatureChassis.Shardling: position.x += ((unitIndex & 1) == 0 ? -1f : 1f) * 4f; yaw = Face(Vector3.zero, position); break; }
        return new SpawnPose(position, yaw);
    }
    private static float Face(Vector3 center, Vector3 position) => Mathf.Atan2(center.x - position.x, center.z - position.z) * Mathf.Rad2Deg;
    private static Vector3 Radial(int groupIndex, int unitIndex, float radius) { var angle = ((groupIndex * 137.5f) + (unitIndex * 61f)) * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(angle) * radius, .35f, Mathf.Sin(angle) * radius); }
    private static DistrictProfile Profile(string id) => id switch { "DF-01" => new(new Vector3(0f,0f,12f),5.5f,6f,24f), "DF-02" => new(Vector3.zero,5f,10f,27f), "DF-03" => new(Vector3.zero,6f,7f,29f), "DF-04" => new(new Vector3(0f,0f,3f),5f,14f,25f), "DF-05" => new(Vector3.zero,6f,9f,29f), "DF-06" => new(Vector3.zero,7f,5f,25f), "DF-07" => new(Vector3.zero,5f,13f,27f), "DF-08" => new(Vector3.zero,7f,5f,24f), "DF-09" => new(Vector3.zero,5f,12f,31f), "DF-10" => new(Vector3.zero,5f,11f,30f), "DF-11" => new(Vector3.zero,6f,7f,25f), "DF-12" => new(Vector3.zero,7f,5f,25f), "DF-13" => new(Vector3.zero,5f,10f,29f), "DF-14" => new(Vector3.zero,5f,9f,28f), "DF-15" => new(Vector3.zero,6f,8f,27f), "DF-16" => new(Vector3.zero,6f,7f,26f), "DF-17" => new(Vector3.zero,5f,12f,30f), "DF-18" => new(Vector3.zero,5f,10f,29f), "DF-19" => new(Vector3.zero,6f,8f,27f), "DF-20" => new(Vector3.zero,9f,4f,30f), _ => throw new InvalidOperationException($"No encounter anchor profile exists for Deep Fracture district '{id}'.") };
    private readonly struct SpawnPose { internal SpawnPose(Vector3 position, float yawDegrees) { Position = position; YawDegrees = yawDegrees; } internal Vector3 Position { get; } internal float YawDegrees { get; } }
    private readonly struct DistrictProfile { internal DistrictProfile(Vector3 center, float spacing, float depthSpread, float edgeRadius) { Center = center; Spacing = spacing; DepthSpread = depthSpread; EdgeRadius = edgeRadius; } internal Vector3 Center { get; } internal float Spacing { get; } internal float DepthSpread { get; } internal float EdgeRadius { get; } }
}
