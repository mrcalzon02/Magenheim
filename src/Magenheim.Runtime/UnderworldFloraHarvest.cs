using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Rebinds native tree/destruction lifecycles to authored species and real resource yields.</summary>
internal static class UnderworldFloraHarvest
{
    internal sealed record Species(string Model, string Wood, string? Secondary, bool Collapses, int WoodAmount);
    internal static readonly IReadOnlyDictionary<string, Species> All = new Dictionary<string, Species>(StringComparer.Ordinal)
    {
        ["glowcap"] = new("underworld-flora-fungal-glowcap", "WorldrootTimber", "GlowcapFlesh", false, 4),
        ["spirestalk"] = new("underworld-flora-fungal-spirestalk", "WorldrootTimber", "SpireFibre", false, 8),
        ["cinderstalk"] = new("underworld-flora-sulfur-cinderstalk", "CharredTimber", null, false, 4),
        ["rimecap"] = new("underworld-flora-frozen-rimecap", "Rimewood", "ClearIce", true, 4),
        ["rotbloom"] = new("underworld-flora-decay-rotbloom", "Rotwood", "DecaySpore", true, 4),
    };

    internal static void Configure(GameObject prefab, string speciesName, Species species)
    {
        var tree = prefab.GetComponent<TreeBase>()
            ?? throw new InvalidOperationException("Harvestable flora donor has no native TreeBase.");
        var visual = ApplyVisual(prefab, species.Model, moving: false);
        tree.m_trunk = visual;
        tree.m_health = speciesName == "spirestalk" ? 160f : 100f;
        tree.m_minToolTier = 0;
        tree.m_spawnOnDamage = null;
        tree.m_dropWhenDestroyed = species.Secondary == null ? Empty() : Table(species.Secondary, 2);
        if (species.Collapses)
        {
            var destructible = prefab.AddComponent<Destructible>();
            destructible.m_health = tree.m_health;
            destructible.m_damages = tree.m_damageModifiers;
            destructible.m_destructibleType = DestructibleType.Tree;
            destructible.m_destroyedEffect = tree.m_destroyedEffect;
            destructible.m_hitEffect = tree.m_hitEffect;
            destructible.m_autoCreateFragments = true;
            var drop = prefab.AddComponent<DropOnDestroyed>();
            drop.m_dropWhenDestroyed = Table(species.Wood, species.WoodAmount);
            if (species.Secondary != null) drop.m_dropWhenDestroyed.m_drops.Add(Data(species.Secondary, 2));
            drop.m_dropWhenDestroyed.m_dropMin = drop.m_dropWhenDestroyed.m_dropMax = drop.m_dropWhenDestroyed.m_drops.Count;
            UnityEngine.Object.DestroyImmediate(tree);
            return;
        }
        tree.m_logPrefab = Fragment(tree.m_logPrefab, speciesName, "felled", species.Wood, species.WoodAmount);
        tree.m_stubPrefab = Fragment(tree.m_stubPrefab, speciesName, "stump", species.Wood, 1);
        var anchor = new GameObject("AuthoredFellingAnchor").transform;
        anchor.SetParent(prefab.transform, false);
        tree.m_logSpawnPoint = anchor;
    }

    private static GameObject Fragment(GameObject donor, string species, string stage, string resource, int amount)
    {
        if (!donor) throw new InvalidOperationException("Native flora donor has no " + stage + " prefab.");
        var name = "Magenheim_Underworld_Flora_" + species + "_" + stage;
        var prefab = PrefabManager.Instance.CreateClonedPrefab(name, donor);
        ApplyVisual(prefab, "underworld-flora-harvest-" + species + "-" + stage, moving: stage == "felled");
        var log = prefab.GetComponent<TreeLog>();
        var stub = prefab.GetComponent<DropOnDestroyed>();
        if (log)
        {
            log.m_dropWhenDestroyed = Table(resource, amount);
            log.m_subLogPrefab = null;
            log.m_subLogPoints = Array.Empty<Transform>();
        }
        else if (stub) stub.m_dropWhenDestroyed = Table(resource, amount);
        else throw new InvalidOperationException("Native flora fragment has no native resource-drop lifecycle: " + name);
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, fixReference: false));
        return prefab;
    }

    private static GameObject ApplyVisual(GameObject prefab, string model, bool moving)
    {
        // Harvestable canopies must leave walking space under the cap. The inventory-item
        // bounding box helper would enclose the entire tree, so use only authored collidable parts.
        foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        var root = ModelAssets.Load(prefab, model, item: false);
        if (moving)
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) collider.convex = true;
        if (root.GetComponentInChildren<Collider>(true) == null)
            throw new InvalidOperationException("Harvestable flora has no authored collider: " + model);
        return root;
    }

    private static DropTable Empty() => new() { m_dropChance = 0f, m_dropMin = 0, m_dropMax = 0 };
    private static DropTable Table(string resource, int amount) => new()
    {
        m_dropMin = 1, m_dropMax = 1, m_dropChance = 1f, m_oneOfEach = true,
        m_drops = new List<DropTable.DropData> { Data(resource, amount) },
    };
    private static DropTable.DropData Data(string resource, int amount) => new()
    {
        m_item = PrefabManager.Instance.GetPrefab("Magenheim_Underworld_Resource_" + resource)
            ?? throw new InvalidOperationException("Missing flora resource dependency: " + resource),
        m_stackMin = amount, m_stackMax = amount, m_weight = 1f, m_dontScale = true,
    };
}
