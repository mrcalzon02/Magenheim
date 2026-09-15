using System;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DarkThroneVisuals
{
    internal const string EncounterAnchorName = "Magenheim_DarkThrone_EncounterAnchor";
    internal const string KingAnchorName = "Magenheim_DarkThrone_KingAnchor";
    private const string MaterialSourcePrefab = "Rock_4";

    internal static void Build(GameObject locationContainer)
    {
        if (locationContainer is null) throw new ArgumentNullException(nameof(locationContainer));
        var source = PrefabManager.Instance.GetPrefab(MaterialSourcePrefab)
            ?? throw new InvalidOperationException("Dark Throne requires vanilla stone material source 'Rock_4'.");
        var sourceRenderer = source.GetComponentsInChildren<Renderer>(true).FirstOrDefault()
            ?? throw new InvalidOperationException("Dark Throne stone material source has no renderer.");
        var sourceMaterial = sourceRenderer.sharedMaterial
            ?? throw new InvalidOperationException("Dark Throne stone material source has no material.");

        var basalt = new Material(sourceMaterial) { name = "Magenheim_DarkThrone_Basalt" };
        var voidStone = new Material(sourceMaterial) { name = "Magenheim_DarkThrone_VoidStone" };
        Tint(basalt, new Color(0.105f, 0.115f, 0.13f, 1f));
        Tint(voidStone, new Color(0.025f, 0.027f, 0.035f, 1f));

        Block(locationContainer.transform, "Arena_Foundation", new Vector3(0f, -0.65f, 0f), new Vector3(52f, 1.3f, 60f), basalt);
        Block(locationContainer.transform, "Dais_Lower", new Vector3(0f, 0.35f, 18f), new Vector3(30f, 0.7f, 16f), basalt);
        Block(locationContainer.transform, "Dais_Upper", new Vector3(0f, 0.9f, 22f), new Vector3(20f, 0.55f, 9f), basalt);
        Block(locationContainer.transform, "Dais_Step_1", new Vector3(0f, 0.05f, 8.8f), new Vector3(18f, 0.3f, 2.2f), basalt);
        Block(locationContainer.transform, "Dais_Step_2", new Vector3(0f, 0.28f, 10.4f), new Vector3(20f, 0.35f, 2.2f), basalt);

        for (var side = -1; side <= 1; side += 2)
        {
            for (var row = 0; row < 4; row++)
            {
                var z = -18f + row * 11.5f;
                Column(locationContainer.transform, $"BrokenColumn_{side}_{row}", new Vector3(side * 20.5f, 2.2f, z), row % 2 == 0 ? 5.8f : 4.4f, basalt);
            }
        }

        BuildThrone(locationContainer.transform, basalt, voidStone);
        Anchor(locationContainer.transform, EncounterAnchorName, new Vector3(0f, 0f, 0f));
        Anchor(locationContainer.transform, KingAnchorName, new Vector3(0f, 1.2f, 13.5f));
        BuildCrystalEcology(locationContainer.transform);
    }

    private static void BuildCrystalEcology(Transform parent)
    {
        // Lesser nodes make the court visibly infested before the player commits to the dais.
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Lesser_West", new Vector3(-18f, 0.2f, -13f), false, 2, 7f, 90f);
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Lesser_East", new Vector3(18f, 0.2f, -13f), false, 2, 7f, 90f);
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Lesser_Approach", new Vector3(0f, 0.2f, -22f), false, 2, 8f, 105f);

        // Guardian nodes defend the upper court. Encounter authority can suspend every node through
        // DarkThroneCrystalSpawner.SetEncounterSuspended when the King takes control of the arena.
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Guardian_West", new Vector3(-13f, 0.8f, 12f), true, 1, 5f, 150f);
        DarkThroneCrystalSpawnerFactory.Create(parent, "CrystalSpawner_Guardian_East", new Vector3(13f, 0.8f, 12f), true, 1, 5f, 150f);
    }

    private static void BuildThrone(Transform parent, Material basalt, Material voidStone)
    {
        Block(parent, "Throne_Platform", new Vector3(0f, 1.45f, 24f), new Vector3(10f, 1.1f, 7f), voidStone);
        Block(parent, "Throne_Seat", new Vector3(0f, 2.5f, 25f), new Vector3(5.8f, 1f, 3.4f), basalt);
        Block(parent, "Throne_Back", new Vector3(0f, 6.2f, 26.1f), new Vector3(6.4f, 8.2f, 1.2f), basalt);
        Block(parent, "Throne_Arm_Left", new Vector3(-3.2f, 3.4f, 24.8f), new Vector3(1.1f, 2.7f, 4.1f), basalt);
        Block(parent, "Throne_Arm_Right", new Vector3(3.2f, 3.4f, 24.8f), new Vector3(1.1f, 2.7f, 4.1f), basalt);
        Block(parent, "Throne_Crown_Left", new Vector3(-2.1f, 10.3f, 26.1f), new Vector3(1.3f, 3f, 1.2f), basalt, Quaternion.Euler(0f, 0f, -13f));
        Block(parent, "Throne_Crown_Right", new Vector3(2.1f, 10.3f, 26.1f), new Vector3(1.3f, 3f, 1.2f), basalt, Quaternion.Euler(0f, 0f, 13f));
    }

    private static void Column(Transform parent, string name, Vector3 position, float height, Material material)
    {
        var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        column.name = name;
        column.transform.SetParent(parent, false);
        column.transform.localPosition = position;
        column.transform.localScale = new Vector3(1.7f, height * 0.5f, 1.7f);
        column.transform.localRotation = Quaternion.Euler(0f, 0f, name.GetHashCode() % 7 - 3);
        column.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = scale;
        block.transform.localRotation = rotation ?? Quaternion.identity;
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }

    private static void Anchor(Transform parent, string name, Vector3 position)
    {
        var anchor = new GameObject(name);
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = position;
    }

    private static void Tint(Material material, Color color)
    {
        if (material.HasProperty("_Color")) material.color = color;
    }
}
