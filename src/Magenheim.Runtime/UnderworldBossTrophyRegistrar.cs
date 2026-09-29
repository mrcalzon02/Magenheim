using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class UnderworldBossTrophyRegistrar
{
    private static bool _subscribed;
    private static bool _registered;
    private static ManualLogSource? _log;

    internal static void Register(ManualLogSource log)
    {
        if (_registered || _subscribed) return;
        _log = log ?? throw new ArgumentNullException(nameof(log));
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _subscribed = true;
    }

    private static void RegisterContent()
    {
        if (_registered) return;
        try
        {
            var bosses = UnderworldDeepstoneRuntimeAuthority.Bosses;
            foreach (var boss in bosses)
                RegisterOne(boss);
            _registered = true;
            _log?.LogInfo($"Registered {bosses.Count} canonical Underworld biome-boss trophy items for Deepstone progression.");
        }
        finally
        {
            if (_subscribed)
            {
                PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
                _subscribed = false;
            }
        }
    }

    private static void RegisterOne(UnderworldBossDefinition boss)
    {
        if (PrefabManager.Instance.GetPrefab(boss.TrophyPrefabName))
            throw new InvalidOperationException($"Canonical boss trophy identity '{boss.TrophyPrefabName}' is already occupied.");
        var spec = SpecFor(boss.TrophyPrefabName);
        var donor = PrefabManager.Instance.GetPrefab(spec.Donor) ? spec.Donor : "TrophyDeer";
        if (!PrefabManager.Instance.GetPrefab(donor))
            throw new InvalidOperationException($"No trophy donor exists for canonical boss trophy '{boss.TrophyPrefabName}'.");

        var item = new CustomItem(boss.TrophyPrefabName, donor);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Trophy: " + spec.Display;
        shared.m_description = "The progression trophy of " + spec.Display + ", required by the Deepstone Conclave.";
        shared.m_maxStackSize = 20;
        shared.m_weight = 2f;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        Tint(item.ItemPrefab, spec.Tint, boss.TrophyPrefabName);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused canonical boss trophy '{boss.TrophyPrefabName}'.");
    }

    private static TrophySpec SpecFor(string prefab) => prefab switch
    {
        "Magenheim_Underworld_Trophy_FirstBloom" =>
            new TrophySpec("The First Bloom","TrophyGreydwarfShaman",new Color(.38f,.72f,.36f)),
        "Magenheim_Underworld_Trophy_BlackwaterMaw" =>
            new TrophySpec("The Blackwater Maw","TrophySerpent",new Color(.10f,.50f,.66f)),
        "Magenheim_Underworld_Trophy_FurnaceHeart" =>
            new TrophySpec("The Furnace Heart","TrophySGolem",new Color(.84f,.28f,.06f)),
        "Magenheim_Underworld_Trophy_WhiteSilence" =>
            new TrophySpec("The White Silence","TrophyHatchling",new Color(.72f,.90f,1f)),
        "Magenheim_Underworld_Trophy_RiftTitan" =>
            new TrophySpec("The Rift Titan","TrophySGolem",new Color(.56f,.34f,.78f)),
        "Magenheim_Underworld_Trophy_CarrionCrown" =>
            new TrophySpec("The Carrion Crown","TrophyDraugrElite",new Color(.52f,.56f,.18f)),
        _ => throw new InvalidOperationException($"Unknown canonical boss trophy '{prefab}'."),
    };

    private static void Tint(GameObject prefab, Color tint, string prefix)
    {
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var source = renderer.sharedMaterials;
            if (source == null || source.Length == 0) continue;
            var materials = new Material[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                if (!source[i]) continue;
                var material = new Material(source[i]) { name = prefix + "." + renderer.name + "." + i };
                if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", tint * .28f);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private readonly record struct TrophySpec(string Display,string Donor,Color Tint);
}
