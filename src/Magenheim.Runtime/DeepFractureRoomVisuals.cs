using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Procedural first-pass geometry for the twenty canonical Deep Fracture district families.
/// These are Magenheim-owned Room prefabs; vanilla random room selection never owns their order.
/// Boundaries remain deliberately broken/open so exact-plan passages can approach districts from
/// arbitrary graph directions without colliding with a sealed decorative wall.
/// </summary>
internal static class DeepFractureRoomVisuals
{
    internal const string PassagePrefabName = "Magenheim_DF_Passage";
    internal const string TraversalNodePrefabName = "Magenheim_DF_TraversalNode";
    internal const float NominalDistrictSize = 96f;
    internal const float PassageLength = 16f;

    internal static string RoomPrefabName(string pieceFamilyId)
    {
        if (string.IsNullOrWhiteSpace(pieceFamilyId) || !pieceFamilyId.StartsWith("DF-", StringComparison.Ordinal))
            throw new ArgumentException("A canonical Deep Fracture piece family ID is required.", nameof(pieceFamilyId));
        return "Magenheim_DF_" + pieceFamilyId.Replace('-', '_');
    }

    internal static GameObject CreateDistrictPrefab(DeepFracturePieceFamily family)
    {
        if (family is null)
            throw new ArgumentNullException(nameof(family));
        family.Validate();

        var materials = CreateMaterials(family.Id);
        var root = CreateRoomRoot(RoomPrefabName(family.Id), new Vector3Int(96, 32, 96));
        AddBox(root.transform, materials.Stone, "DistrictFloor", new Vector3(0f, -1.1f, 0f), new Vector3(88f, 2.2f, 88f));
        AddOpenBoundary(root.transform, materials);

        switch (family.Id)
        {
            case "DF-01": FractureDescent(root.transform, materials); break;
            case "DF-02": SplitStrata(root.transform, materials); break;
            case "DF-03": GeodeCathedral(root.transform, materials); break;
            case "DF-04": BuriedRiver(root.transform, materials); break;
            case "DF-05": ThermalVeins(root.transform, materials); break;
            case "DF-06": CrucibleCavern(root.transform, materials); break;
            case "DF-07": RimeFault(root.transform, materials); break;
            case "DF-08": GlacierVault(root.transform, materials); break;
            case "DF-09": ConductorChasm(root.transform, materials); break;
            case "DF-10": FulminationGallery(root.transform, materials); break;
            case "DF-11": CompressionHall(root.transform, materials); break;
            case "DF-12": SeismicBasin(root.transform, materials); break;
            case "DF-13": ContaminatedGrotto(root.transform, materials); break;
            case "DF-14": DissolutionWorks(root.transform, materials); break;
            case "DF-15": PrismHall(root.transform, materials); break;
            case "DF-16": SanctifiedVault(root.transform, materials); break;
            case "DF-17": EchoingDeep(root.transform, materials); break;
            case "DF-18": HollowOssuary(root.transform, materials); break;
            case "DF-19": ShapingWorks(root.transform, materials); break;
            case "DF-20": ConfluenceHeart(root.transform, materials); break;
            default: throw new InvalidOperationException($"No runtime geometry is defined for Deep Fracture piece '{family.Id}'.");
        }

        return root;
    }

    internal static GameObject CreatePassagePrefab()
    {
        var m = CreateMaterials("Passage");
        var root = CreateRoomRoot(PassagePrefabName, new Vector3Int(10, 10, 16));
        AddBox(root.transform, m.Stone, "PassageFloor", new Vector3(0f, -0.6f, 0f), new Vector3(8f, 1.2f, 16f));
        AddBox(root.transform, m.DarkStone, "PassageLeft", new Vector3(-4.2f, 3.1f, 0f), new Vector3(1.2f, 7.4f, 16f));
        AddBox(root.transform, m.DarkStone, "PassageRight", new Vector3(4.2f, 3.1f, 0f), new Vector3(1.2f, 7.4f, 16f));
        AddBox(root.transform, m.Stone, "PassageRoof", new Vector3(0f, 7f, 0f), new Vector3(9f, 1.2f, 16f));
        AddCrystal(root.transform, m.Crystal, "PassageSeam", new Vector3(3.65f, 4f, 0f), new Vector3(.25f, 5f, 8f), 0f, 0f);
        return root;
    }

    internal static GameObject CreateTraversalNodePrefab()
    {
        var m = CreateMaterials("Traversal");
        var root = CreateRoomRoot(TraversalNodePrefabName, new Vector3Int(12, 10, 12));
        AddCylinder(root.transform, m.DarkStone, "TraversalPlinth", new Vector3(0f, 0f, 0f), new Vector3(4.5f, .8f, 4.5f));
        AddCylinder(root.transform, m.Stone, "TraversalRing", new Vector3(0f, .9f, 0f), new Vector3(3.4f, .45f, 3.4f));
        AddCrystal(root.transform, m.Crystal, "TraversalCore", new Vector3(0f, 4.2f, 0f), new Vector3(1.4f, 5.8f, 1.4f), 45f, 0f);
        root.AddComponent<DeepFractureTraversalPortal>();
        return root;
    }

    internal static void ApplyElementalState(Room placedRoom, IReadOnlyList<ElementalAlignment> elementalStates)
    {
        if (placedRoom is null)
            throw new ArgumentNullException(nameof(placedRoom));
        if (elementalStates is null || elementalStates.Count == 0)
            return;

        var red = 0f;
        var green = 0f;
        var blue = 0f;
        foreach (var element in elementalStates)
        {
            var tint = ElementVisualPalette.Tint(element);
            red += tint.r;
            green += tint.g;
            blue += tint.b;
        }

        var blend = new Color(red / elementalStates.Count, green / elementalStates.Count, blue / elementalStates.Count, 1f);
        foreach (var renderer in placedRoom.GetComponentsInChildren<Renderer>(true)
                     .Where(candidate => candidate.gameObject.name.IndexOf("Crystal", StringComparison.OrdinalIgnoreCase) >= 0
                         || candidate.gameObject.name.IndexOf("Seam", StringComparison.OrdinalIgnoreCase) >= 0
                         || candidate.gameObject.name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            if (renderer.sharedMaterial is null)
                continue;
            var material = new Material(renderer.sharedMaterial) { name = renderer.sharedMaterial.name + "_Placed" };
            TintMaterial(material, blend, blend * .35f);
            renderer.sharedMaterial = material;
        }
    }

    private static GameObject CreateRoomRoot(string name, Vector3Int size)
    {
        var root = new GameObject(name);
        root.SetActive(false);
        var room = root.AddComponent<Room>();
        room.m_enabled = true;
        room.m_size = size;
        return root;
    }

    private static MaterialSet CreateMaterials(string identity)
    {
        var shader = Shader.Find("Standard");
        if (shader is null)
            throw new InvalidOperationException("Unity Standard shader is unavailable; Deep Fracture room materials cannot be created safely.");

        var stone = new Material(shader) { name = $"Magenheim_DF_{identity}_Stone" };
        var dark = new Material(shader) { name = $"Magenheim_DF_{identity}_DarkStone" };
        var crystal = new Material(shader) { name = $"Magenheim_DF_{identity}_Crystal" };
        TintMaterial(stone, new Color(.29f, .30f, .31f, 1f), null);
        TintMaterial(dark, new Color(.095f, .105f, .12f, 1f), null);
        var crystalColor = new Color(.34f, .60f, .75f, 1f);
        TintMaterial(crystal, crystalColor, crystalColor * .28f);
        return new MaterialSet(stone, dark, crystal);
    }

    private static void AddOpenBoundary(Transform root, MaterialSet m)
    {
        var corners = new[]
        {
            new Vector3(-40f, 5f, -40f), new Vector3(40f, 5f, -40f),
            new Vector3(-40f, 5f, 40f), new Vector3(40f, 5f, 40f),
        };
        for (var i = 0; i < corners.Length; i++)
            AddBox(root, i % 2 == 0 ? m.DarkStone : m.Stone, $"BoundaryMass{i}", corners[i], new Vector3(13f, 12f, 13f), new Vector3(0f, i * 21f, i % 2 == 0 ? 8f : -8f));

        AddBox(root, m.DarkStone, "BoundaryNorthWest", new Vector3(-23f, 4f, 43f), new Vector3(20f, 10f, 5f));
        AddBox(root, m.Stone, "BoundaryNorthEast", new Vector3(23f, 4f, 43f), new Vector3(20f, 10f, 5f));
        AddBox(root, m.Stone, "BoundarySouthWest", new Vector3(-23f, 4f, -43f), new Vector3(20f, 10f, 5f));
        AddBox(root, m.DarkStone, "BoundarySouthEast", new Vector3(23f, 4f, -43f), new Vector3(20f, 10f, 5f));
    }

    private static void FractureDescent(Transform root, MaterialSet m)
    {
        for (var i = 0; i < 6; i++)
            AddBox(root, m.Stone, $"DescentStep{i}", new Vector3(0f, i * -1.15f, -31f + i * 10.5f), new Vector3(27f, 1.2f, 10f), new Vector3(7f, 0f, 0f));
        AddCrystal(root, m.Crystal, "DescentSeam", new Vector3(16f, 4f, 15f), new Vector3(1.2f, 11f, 1.2f), 18f, 8f);
    }

    private static void SplitStrata(Transform root, MaterialSet m)
    {
        AddBox(root, m.DarkStone, "SplitMassA", new Vector3(-19f, 5f, 5f), new Vector3(22f, 14f, 58f), new Vector3(0f, 18f, 0f));
        AddBox(root, m.Stone, "SplitMassB", new Vector3(20f, 3f, -6f), new Vector3(20f, 10f, 56f), new Vector3(0f, -20f, 0f));
        AddCrystal(root, m.Crystal, "SplitSeam", new Vector3(1f, 6f, 0f), new Vector3(1.3f, 15f, 25f), 12f, 0f);
    }

    private static void GeodeCathedral(Transform root, MaterialSet m)
    {
        Ring(root, 8, 26f, (index, position) => AddCrystal(root, m.Crystal, $"CathedralCrystal{index}", position + Vector3.up * 8f, new Vector3(2.6f, 17f + (index % 3) * 3f, 2.6f), 45f, index * 4f));
        AddCylinder(root, m.DarkStone, "CathedralDais", new Vector3(0f, .4f, 0f), new Vector3(10f, 1.1f, 10f));
    }

    private static void BuriedRiver(Transform root, MaterialSet m)
    {
        AddBox(root, m.DarkStone, "RiverCut", new Vector3(0f, -.5f, 0f), new Vector3(18f, 1f, 78f));
        AddBox(root, m.Stone, "RiverBridge", new Vector3(0f, 2f, 0f), new Vector3(34f, 2f, 9f));
        AddBox(root, m.Stone, "BankWest", new Vector3(-27f, 1f, 0f), new Vector3(26f, 4f, 70f));
        AddBox(root, m.Stone, "BankEast", new Vector3(27f, 1f, 0f), new Vector3(26f, 4f, 70f));
    }

    private static void ThermalVeins(Transform root, MaterialSet m)
    {
        for (var i = 0; i < 7; i++)
            AddCylinder(root, i % 2 == 0 ? m.Crystal : m.DarkStone, $"ThermalVent{i}", new Vector3(-30f + i * 10f, 2f, i % 2 == 0 ? 18f : -18f), new Vector3(3.6f, 4f + i % 3, 3.6f));
        AddCrystal(root, m.Crystal, "ThermalVein", new Vector3(0f, 4f, 0f), new Vector3(1.6f, 8f, 44f), 0f, 0f);
    }

    private static void CrucibleCavern(Transform root, MaterialSet m)
    {
        AddCylinder(root, m.DarkStone, "CrucibleBasin", new Vector3(0f, -.3f, 0f), new Vector3(20f, 1f, 20f));
        Ring(root, 6, 30f, (index, position) => AddCylinder(root, m.Stone, $"CrucibleButtress{index}", position + Vector3.up * 5f, new Vector3(4f, 10f, 4f)));
        AddCrystal(root, m.Crystal, "CrucibleCore", new Vector3(0f, 6f, 0f), new Vector3(4f, 13f, 4f), 45f, 0f);
    }

    private static void RimeFault(Transform root, MaterialSet m)
    {
        for (var i = 0; i < 9; i++)
            AddCrystal(root, m.Crystal, $"RimeFin{i}", new Vector3(-32f + i * 8f, 5f + i % 3, i % 2 == 0 ? -20f : 20f), new Vector3(1.8f, 11f + i % 4, 4f), i % 2 == 0 ? 18f : -18f, 0f);
        AddBox(root, m.DarkStone, "RimeFaultFloor", new Vector3(0f, -.3f, 0f), new Vector3(11f, 1f, 70f));
    }

    private static void GlacierVault(Transform root, MaterialSet m)
    {
        for (var i = -2; i <= 2; i++)
            AddBox(root, m.Stone, $"VaultRib{i}", new Vector3(i * 15f, 8f, 0f), new Vector3(4f, 17f, 64f), new Vector3(0f, 0f, i * 5f));
        AddCrystal(root, m.Crystal, "VaultKeystone", new Vector3(0f, 10f, 0f), new Vector3(8f, 8f, 8f), 45f, 45f);
    }

    private static void ConductorChasm(Transform root, MaterialSet m)
    {
        AddBox(root, m.DarkStone, "ConductorVoid", new Vector3(0f, -.4f, 0f), new Vector3(28f, 1f, 74f));
        AddBox(root, m.Stone, "ConductorBridge", new Vector3(0f, 3f, 0f), new Vector3(11f, 2f, 72f));
        for (var i = 0; i < 6; i++)
            AddCrystal(root, m.Crystal, $"ConductorPylon{i}", new Vector3(i % 2 == 0 ? -20f : 20f, 7f, -30f + i * 12f), new Vector3(2.3f, 15f, 2.3f), 0f, 0f);
    }

    private static void FulminationGallery(Transform root, MaterialSet m)
    {
        AddBox(root, m.Stone, "GallerySpine", new Vector3(0f, 1f, 0f), new Vector3(23f, 3f, 76f));
        for (var i = 0; i < 8; i++)
            AddCrystal(root, m.Crystal, $"GalleryRod{i}", new Vector3(i % 2 == 0 ? -16f : 16f, 6f, -31f + i * 9f), new Vector3(1.4f, 12f, 1.4f), 0f, 0f);
    }

    private static void CompressionHall(Transform root, MaterialSet m)
    {
        AddBox(root, m.DarkStone, "CompressionWest", new Vector3(-19f, 5f, 0f), new Vector3(16f, 14f, 68f));
        AddBox(root, m.DarkStone, "CompressionEast", new Vector3(19f, 5f, 0f), new Vector3(16f, 14f, 68f));
        for (var i = 0; i < 5; i++)
            AddBox(root, m.Stone, $"CompressionBrace{i}", new Vector3(0f, 8f, -29f + i * 14.5f), new Vector3(21f, 4f, 3.5f));
    }

    private static void SeismicBasin(Transform root, MaterialSet m)
    {
        AddCylinder(root, m.DarkStone, "SeismicBasin", new Vector3(0f, -.6f, 0f), new Vector3(28f, 1f, 28f));
        Ring(root, 8, 25f, (index, position) => AddBox(root, m.Stone, $"SeismicRadial{index}", position + Vector3.up, new Vector3(7f, 3f, 22f), new Vector3(0f, index * 45f, 0f)));
    }

    private static void ContaminatedGrotto(Transform root, MaterialSet m)
    {
        for (var i = 0; i < 11; i++)
        {
            var x = -30f + (i % 6) * 12f;
            var z = i < 6 ? -22f : 22f;
            AddCylinder(root, i % 3 == 0 ? m.Crystal : m.DarkStone, $"GrottoNode{i}", new Vector3(x, 1f + i % 3, z), new Vector3(4f + i % 3, 2f + i % 4, 4f + i % 3));
        }
    }

    private static void DissolutionWorks(Transform root, MaterialSet m)
    {
        for (var i = 0; i < 4; i++)
        {
            var x = i % 2 == 0 ? -20f : 20f;
            var z = i < 2 ? -20f : 20f;
            AddCylinder(root, m.DarkStone, $"DissolutionVat{i}", new Vector3(x, 2f, z), new Vector3(8f, 3f, 8f));
            AddCrystal(root, m.Crystal, $"DissolutionCore{i}", new Vector3(x, 5f, z), new Vector3(1.8f, 6f, 1.8f), 45f, 0f);
        }
        AddBox(root, m.Stone, "WorksWalkway", new Vector3(0f, 3f, 0f), new Vector3(58f, 1.4f, 8f));
    }

    private static void PrismHall(Transform root, MaterialSet m)
    {
        for (var side = -1; side <= 1; side += 2)
            for (var i = 0; i < 6; i++)
                AddCrystal(root, m.Crystal, $"Prism_{side}_{i}", new Vector3(side * 20f, 7f, -30f + i * 12f), new Vector3(3.5f, 15f + (i % 2) * 5f, 3.5f), 45f, 0f);
        AddBox(root, m.Stone, "PrismProcessional", new Vector3(0f, 1f, 0f), new Vector3(15f, 2f, 74f));
    }

    private static void SanctifiedVault(Transform root, MaterialSet m)
    {
        Ring(root, 8, 28f, (index, position) => AddCylinder(root, m.Stone, $"SanctifiedColumn{index}", position + Vector3.up * 7f, new Vector3(3.5f, 14f, 3.5f)));
        AddCrystal(root, m.Crystal, "SanctifiedFocus", new Vector3(0f, 8f, 0f), new Vector3(3f, 17f, 3f), 45f, 0f);
    }

    private static void EchoingDeep(Transform root, MaterialSet m)
    {
        AddBox(root, m.DarkStone, "EchoVoid", new Vector3(0f, -.5f, 0f), new Vector3(52f, 1f, 52f));
        for (var i = 0; i < 5; i++)
            AddBox(root, m.Stone, $"EchoLedge{i}", new Vector3(i % 2 == 0 ? -30f : 30f, 2f + i * 2f, -28f + i * 14f), new Vector3(18f, 2f, 11f));
        AddCrystal(root, m.Crystal, "EchoBeacon", new Vector3(0f, 5f, 0f), new Vector3(2f, 11f, 2f), 0f, 0f);
    }

    private static void HollowOssuary(Transform root, MaterialSet m)
    {
        for (var side = -1; side <= 1; side += 2)
            for (var i = 0; i < 6; i++)
                AddBox(root, m.DarkStone, $"OssuaryAlcove_{side}_{i}", new Vector3(side * 31f, 4f, -30f + i * 12f), new Vector3(11f, 8f, 7f));
        AddBox(root, m.Stone, "OssuarySpine", new Vector3(0f, 1f, 0f), new Vector3(17f, 2f, 70f));
    }

    private static void ShapingWorks(Transform root, MaterialSet m)
    {
        AddBox(root, m.Stone, "ShapingPlatformA", new Vector3(-20f, 1.5f, 0f), new Vector3(25f, 3f, 52f));
        AddBox(root, m.Stone, "ShapingPlatformB", new Vector3(20f, 1.5f, 0f), new Vector3(25f, 3f, 52f));
        for (var i = 0; i < 6; i++)
            AddCrystal(root, m.Crystal, $"ShapingFixture{i}", new Vector3(i % 2 == 0 ? -20f : 20f, 5f, -24f + i * 10f), new Vector3(2.5f, 7f, 2.5f), 45f, 0f);
    }

    private static void ConfluenceHeart(Transform root, MaterialSet m)
    {
        AddCylinder(root, m.DarkStone, "HeartArena", new Vector3(0f, 0f, 0f), new Vector3(30f, 1.3f, 30f));
        AddCylinder(root, m.Stone, "HeartInnerRing", new Vector3(0f, 1.5f, 0f), new Vector3(17f, .8f, 17f));
        Ring(root, 8, 28f, (index, position) => AddCrystal(root, m.Crystal, $"HeartMonolith{index}", position + Vector3.up * 8f, new Vector3(3.2f, 18f + (index % 3) * 3f, 3.2f), 45f, 0f));
        AddCrystal(root, m.Crystal, "HeartCore", new Vector3(0f, 10f, 0f), new Vector3(6f, 21f, 6f), 45f, 45f);
    }

    private static void Ring(Transform root, int count, float radius, Action<int, Vector3> add)
    {
        for (var i = 0; i < count; i++)
        {
            var direction = Quaternion.Euler(0f, i * (360f / count), 0f) * Vector3.forward;
            add(i, direction * radius);
        }
    }

    private static GameObject AddBox(Transform parent, Material material, string name, Vector3 localPosition, Vector3 localScale, Vector3? euler = null)
    {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = localScale;
        gameObject.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
        var renderer = gameObject.GetComponent<Renderer>() ?? throw new InvalidOperationException($"Generated Deep Fracture object '{name}' has no renderer.");
        renderer.sharedMaterial = material;
        return gameObject;
    }

    private static GameObject AddCylinder(Transform parent, Material material, string name, Vector3 localPosition, Vector3 localScale)
    {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = localScale;
        var renderer = gameObject.GetComponent<Renderer>() ?? throw new InvalidOperationException($"Generated Deep Fracture object '{name}' has no renderer.");
        renderer.sharedMaterial = material;
        return gameObject;
    }

    private static GameObject AddCrystal(Transform parent, Material material, string name, Vector3 localPosition, Vector3 localScale, float yaw, float roll)
        => AddBox(parent, material, name, localPosition, localScale, new Vector3(0f, yaw, roll));

    private static void TintMaterial(Material material, Color color, Color? emission)
    {
        if (material.HasProperty("_Color"))
            material.color = color;
        if (emission.HasValue && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
        }
    }

    private sealed class MaterialSet
    {
        internal MaterialSet(Material stone, Material darkStone, Material crystal)
        {
            Stone = stone;
            DarkStone = darkStone;
            Crystal = crystal;
        }

        internal Material Stone { get; }
        internal Material DarkStone { get; }
        internal Material Crystal { get; }
    }
}
