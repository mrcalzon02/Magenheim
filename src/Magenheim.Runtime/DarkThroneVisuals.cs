using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Magenheim.Core.DarkThrone;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Owned Dark Throne arena presentation. The vanilla rock prefab is only a shader/material donor;
/// visible geometry is Magenheim-owned and built from the tested canonical mesh authority.
/// </summary>
internal static class DarkThroneVisuals
{
    internal const string EncounterAnchorName = "Magenheim_DarkThrone_EncounterAnchor";
    internal const string KingAnchorName = "Magenheim_DarkThrone_KingAnchor";
    internal const string DaisRootName = "Magenheim_DarkThrone_Dais";

    private const string MaterialSourcePrefab = "Rock_4";
    private static readonly Mesh BoxMesh = RuntimeMeshPrimitives.Box("magenheim.dark-throne.box");
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static void Build(GameObject locationContainer)
    {
        if (locationContainer is null) throw new ArgumentNullException(nameof(locationContainer));

        var source = PrefabManager.Instance.GetPrefab(MaterialSourcePrefab)
            ?? throw new InvalidOperationException("Dark Throne requires vanilla stone material source 'Rock_4'.");
        var sourceRenderer = source.GetComponentsInChildren<Renderer>(true).FirstOrDefault()
            ?? throw new InvalidOperationException("Dark Throne stone material source has no renderer.");
        var sourceMaterial = sourceRenderer.sharedMaterial
            ?? throw new InvalidOperationException("Dark Throne stone material source has no material.");

        var basalt = Surface(sourceMaterial, "basalt", "basalt stone", new Color(.105f, .115f, .13f, 1f), .08f, .18f);
        var voidStone = Surface(sourceMaterial, "void-stone", "void basalt stone", new Color(.025f, .027f, .035f, 1f), .12f, .24f);
        var metal = Surface(sourceMaterial, "rune-metal", "ancient iron metal", new Color(.16f, .14f, .12f, 1f), .72f, .34f);
        var rune = Surface(sourceMaterial, "rune-inlay", "crystal rune metal", new Color(.29f, .07f, .37f, 1f), .48f, .46f, .34f);

        // Arena massing: broad rock courses with a distinct central approach instead of one giant
        // unbroken Unity cube. The extra courses give the arena readable scale from player height.
        Block(locationContainer.transform, "Arena_Foundation", new Vector3(0f, -.70f, 0f), new Vector3(52f, 1.25f, 60f), basalt);
        Block(locationContainer.transform, "Arena_InnerFloor", new Vector3(0f, -.03f, 3f), new Vector3(42f, .18f, 47f), voidStone);
        Block(locationContainer.transform, "Arena_Approach", new Vector3(0f, .08f, -18f), new Vector3(13f, .22f, 18f), basalt);

        var dais = new GameObject(DaisRootName) { layer = locationContainer.layer };
        dais.transform.SetParent(locationContainer.transform, false);
        BuildDais(dais.transform, basalt, voidStone, metal, rune);
        BuildThrone(dais.transform, basalt, voidStone, metal, rune);

        // Broken processional columns now use the canonical cylinder mesh and proper bases/collars,
        // rather than isolated low-detail Unity cylinders floating in the arena.
        for (var side = -1; side <= 1; side += 2)
        for (var row = 0; row < 4; row++)
        {
            var position = new Vector3(side * 20.5f, 0f, -18f + row * 11.5f);
            var height = row % 2 == 0 ? 5.8f : 4.4f;
            BuildBrokenColumn(locationContainer.transform, $"BrokenColumn_{side}_{row}", position, height, basalt, metal);
        }

        var encounter = BuildEncounterAuthority(locationContainer.transform);
        var interaction = dais.AddComponent<DarkThroneDaisRuntime>();
        interaction.Bind(encounter);
        Anchor(locationContainer.transform, KingAnchorName, new Vector3(0f, 1.2f, 13.5f));
        BuildCrystalEcology(locationContainer.transform);
    }

    private static void BuildDais(Transform parent, Material basalt, Material voidStone, Material metal, Material rune)
    {
        Block(parent, "Dais_Approach_1", new Vector3(0f, .05f, 8.4f), new Vector3(17f, .30f, 2.3f), basalt);
        Block(parent, "Dais_Approach_2", new Vector3(0f, .27f, 10.2f), new Vector3(19f, .36f, 2.4f), basalt);
        Block(parent, "Dais_Lower", new Vector3(0f, .52f, 17.7f), new Vector3(30f, .72f, 16f), basalt);
        Block(parent, "Dais_ShadowCourse", new Vector3(0f, .90f, 20.2f), new Vector3(24f, .30f, 11.5f), voidStone);
        Block(parent, "Dais_Upper", new Vector3(0f, 1.12f, 22f), new Vector3(20f, .34f, 8.8f), basalt);

        // Narrow iron bands visually terminate the courses so the dais reads as constructed ritual
        // architecture rather than stacked boxes.
        Block(parent, "Dais_FrontBand", new Vector3(0f, .89f, 14.5f), new Vector3(29f, .12f, .20f), metal);
        Block(parent, "Dais_UpperBand", new Vector3(0f, 1.31f, 18f), new Vector3(19.4f, .10f, .18f), metal);
        BuildRuneCircuit(parent, rune);
    }

    private static DarkThroneEncounterRuntime BuildEncounterAuthority(Transform parent)
    {
        var anchor = new GameObject(EncounterAnchorName);
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = Vector3.zero;
        anchor.AddComponent<ZNetView>();
        return anchor.AddComponent<DarkThroneEncounterRuntime>();
    }

    private static void BuildRuneCircuit(Transform parent, Material rune)
    {
        for (var i = 0; i < 12; i++)
        {
            var angle = i * Mathf.PI * 2f / 12f;
            var position = new Vector3(Mathf.Sin(angle) * 7.6f, 1.36f, 21.8f + Mathf.Cos(angle) * 3.35f);
            var inlay = Block(parent, $"Rune_{i + 1:00}", position, new Vector3(.22f, .055f, 1.08f), rune,
                Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
            var light = inlay.AddComponent<Light>();
            light.color = new Color(.34f, .04f, .46f);
            light.range = 2.1f;
            light.intensity = .36f;
        }
    }

    private static void BuildCrystalEcology(Transform parent)
    {
        var lesser = CrystalCreatureSpawnProfiles.DarkThroneLesser;
        var guardian = CrystalCreatureSpawnProfiles.DarkThroneGuardian;
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Lesser_West", new Vector3(-18f, .2f, -13f), lesser);
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Lesser_East", new Vector3(18f, .2f, -13f), lesser);
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Lesser_Approach", new Vector3(0f, .2f, -22f), lesser);
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Guardian_West", new Vector3(-13f, .8f, 12f), guardian);
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Guardian_East", new Vector3(13f, .8f, 12f), guardian);
    }

    private static void BuildThrone(Transform parent, Material basalt, Material voidStone, Material metal, Material rune)
    {
        Block(parent, "Throne_Platform_Lower", new Vector3(0f, 1.58f, 24f), new Vector3(10.5f, .62f, 7.3f), voidStone);
        Block(parent, "Throne_Platform_Upper", new Vector3(0f, 1.98f, 24.6f), new Vector3(8.4f, .34f, 5.5f), basalt);
        Block(parent, "Throne_Seat", new Vector3(0f, 2.55f, 24.65f), new Vector3(5.4f, .82f, 3.25f), basalt);
        Block(parent, "Throne_Seat_Inlay", new Vector3(0f, 3.00f, 24.55f), new Vector3(4.2f, .10f, 2.35f), voidStone);

        // Split the back into framed courses and a recessed void panel; this reads as monumental
        // carved architecture instead of one tall cuboid slab.
        Block(parent, "Throne_Back_Lower", new Vector3(0f, 5.05f, 26.0f), new Vector3(6.2f, 4.8f, .90f), basalt);
        Block(parent, "Throne_Back_Upper", new Vector3(0f, 8.15f, 26.0f), new Vector3(5.2f, 2.0f, .78f), basalt);
        Block(parent, "Throne_VoidPanel", new Vector3(0f, 5.90f, 25.50f), new Vector3(3.6f, 4.6f, .12f), voidStone);
        Block(parent, "Throne_RuneSpine", new Vector3(0f, 6.05f, 25.40f), new Vector3(.24f, 4.0f, .10f), rune);

        Block(parent, "Throne_Arm_Left", new Vector3(-3.2f, 3.45f, 24.6f), new Vector3(1.0f, 2.5f, 4.0f), basalt);
        Block(parent, "Throne_Arm_Right", new Vector3(3.2f, 3.45f, 24.6f), new Vector3(1.0f, 2.5f, 4.0f), basalt);
        Block(parent, "Throne_ArmCap_Left", new Vector3(-3.2f, 4.62f, 24.25f), new Vector3(1.28f, .22f, 3.4f), metal);
        Block(parent, "Throne_ArmCap_Right", new Vector3(3.2f, 4.62f, 24.25f), new Vector3(1.28f, .22f, 3.4f), metal);

        Prism(parent, "Throne_Crown_Left", new Vector3(-2.05f, 9.62f, 26f), new Vector3(1.55f, 3.25f, 1.55f), basalt,
            Quaternion.Euler(0f, 0f, -9f));
        Prism(parent, "Throne_Crown_Center", new Vector3(0f, 10.15f, 26f), new Vector3(1.25f, 3.75f, 1.25f), voidStone,
            Quaternion.identity);
        Prism(parent, "Throne_Crown_Right", new Vector3(2.05f, 9.62f, 26f), new Vector3(1.55f, 3.25f, 1.55f), basalt,
            Quaternion.Euler(0f, 0f, 9f));
    }

    private static void BuildBrokenColumn(Transform parent, string name, Vector3 groundPosition, float height, Material stone, Material metal)
    {
        Cylinder(parent, name + "_Base", groundPosition + Vector3.up * .24f, new Vector3(2.25f, .48f, 2.25f), 12, stone);
        Cylinder(parent, name + "_Plinth", groundPosition + Vector3.up * .56f, new Vector3(1.90f, .22f, 1.90f), 12, metal);

        var shaft = AddMeshPart(parent, name + "_Shaft", CylinderMesh(12), groundPosition + Vector3.up * (height * .5f + .58f),
            new Vector3(1.50f, height, 1.50f), Quaternion.Euler(0f, 0f, StableLean(name)), stone, CollisionShape.Mesh);
        shaft.transform.localRotation *= Quaternion.Euler(0f, StableYaw(name), 0f);

        Cylinder(parent, name + "_Collar", groundPosition + Vector3.up * (height + .62f), new Vector3(1.82f, .18f, 1.82f), 12, metal);
        Prism(parent, name + "_BrokenTop", groundPosition + Vector3.up * (height + .87f), new Vector3(1.58f, .55f, 1.58f), stone,
            Quaternion.Euler(0f, StableYaw(name) * .5f, StableLean(name) * 1.4f));
    }

    private static float StableLean(string name) => ((StableHash(name) % 9) - 4) * .65f;
    private static float StableYaw(string name) => (StableHash(name) % 31) - 15f;

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in value) hash = hash * 31 + c;
            return hash & int.MaxValue;
        }
    }

    private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null) =>
        AddMeshPart(parent, name, BoxMesh, position, scale, rotation ?? Quaternion.identity, material, CollisionShape.Box);

    private static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, int sides, Material material) =>
        AddMeshPart(parent, name, CylinderMesh(sides), position, scale, Quaternion.identity, material, CollisionShape.Mesh);

    private static GameObject Prism(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation) =>
        AddMeshPart(parent, name, PrismMesh(6), position, scale, rotation, material, CollisionShape.Mesh);

    private static GameObject AddMeshPart(
        Transform parent,
        string name,
        Mesh mesh,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material,
        CollisionShape collision)
    {
        var part = new GameObject(name) { layer = parent.gameObject.layer };
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = rotation;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;

        if (collision == CollisionShape.Box)
        {
            part.AddComponent<BoxCollider>();
        }
        else
        {
            var collider = part.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        return part;
    }

    private static Mesh CylinderMesh(int sides)
    {
        if (!CylinderMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = RuntimeMeshPrimitives.Cylinder(sides, $"magenheim.dark-throne.cylinder.{sides}");
            CylinderMeshes.Add(sides, mesh);
        }
        return mesh;
    }

    private static Mesh PrismMesh(int sides)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = RuntimeMeshPrimitives.Prism(sides, $"magenheim.dark-throne.prism.{sides}", .42f, -.50f, .50f, .18f, .66f, -.52f);
            PrismMeshes.Add(sides, mesh);
        }
        return mesh;
    }

    private static void Anchor(Transform parent, string name, Vector3 position)
    {
        var anchor = new GameObject(name) { layer = parent.gameObject.layer };
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = position;
    }

    private static Material Surface(
        Material source,
        string suffix,
        string semantic,
        Color color,
        float metallic,
        float gloss,
        float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.dark-throne." + suffix };
        GeneratedSurfaceTextures.Apply(material, semantic);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
        material.DisableKeyword("_NORMALMAP");
        if (material.HasProperty("_EmissionColor") && emission > 0f)
        {
            material.SetColor("_EmissionColor", color * emission);
            material.EnableKeyword("_EMISSION");
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }
        material.SetOverrideTag("RenderType", "Opaque");
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
        material.renderQueue = 2000;
        return material;
    }

    private enum CollisionShape
    {
        Box,
        Mesh,
    }
}
