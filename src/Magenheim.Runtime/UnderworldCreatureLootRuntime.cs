using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Canonical runtime bridge from Magenheim creatures into the Underworld material economy.
/// Custom drop tables replace donor loot; harvesting remains the primary source and creature loot
/// is a supplemental route. Trophies are one per species, not per star level/body sex/alignment.
/// </summary>
internal static class UnderworldCreatureLootRuntime
{
    private readonly record struct Drop(string Item, int Min, int Max, float Chance);
    private static readonly HashSet<string> RegisteredTrophies = new(StringComparer.Ordinal);

    internal static IEnumerable<DropConfig> OrdinaryDrops(UnderworldCreaturePrototypes.Entry entry)
    {
        var drops = Ordinary(entry.Name);
        foreach (var drop in drops) yield return Config(drop);
        var trophy = EnsureTrophy("Magenheim_Trophy_Underworld_" + Id(entry.Name), entry.Name, entry.Donor, entry.Color);
        yield return Config(new Drop(trophy, 1, 1, TrophyChance(entry.Name)));
    }

    internal static IEnumerable<DropConfig> SurtlingDrops(UnderworldSurtlings.Entry entry)
    {
        foreach (var drop in Surtling(entry.Element)) yield return Config(drop);
        var color = entry.Element switch
        {
            "Fire" => new Color(.95f,.28f,.06f),
            "Water" => new Color(.12f,.62f,.88f),
            "Earth" => new Color(.52f,.42f,.26f),
            "Wind" => new Color(.70f,.85f,.90f),
            "Radiance" => new Color(1f,.82f,.26f),
            "Umbral" => new Color(.28f,.12f,.42f),
            _ => Color.white,
        };
        var trophy = EnsureTrophy("Magenheim_Trophy_Surtling_" + Id(entry.Element), entry.Element + " Surtling", entry.Donor, color);
        yield return Config(new Drop(trophy, 1, 1, 10f));
    }

    internal static void ApplyDeepFracture(GameObject prefab, string species, string donor)
    {
        var color = new Color(.58f,.38f,.78f);
        var trophy = EnsureTrophy("Magenheim_Trophy_DeepFracture_" + Id(species), species, donor, color);
        var component = prefab.GetComponent<CharacterDrop>() ?? prefab.AddComponent<CharacterDrop>();
        component.m_drops = new List<CharacterDrop.Drop>();
        foreach (var drop in DeepFracture(species))
            component.m_drops.Add(Native(drop));
        component.m_drops.Add(Native(new Drop(trophy, 1, 1, 10f)));
    }

    private static DropConfig Config(Drop drop) => new()
    {
        Item = drop.Item,
        MinAmount = drop.Min,
        MaxAmount = drop.Max,
        Chance = drop.Chance,
        OnePerPlayer = false,
        LevelMultiplier = false,
    };

    private static CharacterDrop.Drop Native(Drop drop)
    {
        var prefab = PrefabManager.Instance.GetPrefab(drop.Item)
            ?? throw new InvalidOperationException("Creature loot item '" + drop.Item + "' is not registered.");
        return new CharacterDrop.Drop
        {
            m_prefab = prefab,
            m_amountMin = drop.Min,
            m_amountMax = drop.Max,
            m_chance = drop.Chance / 100f,
            m_onePerPlayer = false,
            m_levelMultiplier = false,
            m_dontScale = false,
        };
    }

    private static string EnsureTrophy(string prefabName, string displayName, string donorCreature, Color tint)
    {
        if (RegisteredTrophies.Contains(prefabName) || PrefabManager.Instance.GetPrefab(prefabName))
        {
            RegisteredTrophies.Add(prefabName);
            return prefabName;
        }

        var donor = TrophyDonor(donorCreature);
        if (!PrefabManager.Instance.GetPrefab(donor))
            donor = "TrophyDeer";
        if (!PrefabManager.Instance.GetPrefab(donor))
            throw new InvalidOperationException("No trophy donor is available for '" + displayName + "'.");

        var item = new CustomItem(prefabName, donor);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Trophy: " + displayName;
        shared.m_description = "A trophy taken from " + displayName + ".";
        shared.m_maxStackSize = 20;
        shared.m_weight = 1.5f;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        TintTrophy(item.ItemPrefab, tint, prefabName);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException("Jotunn refused trophy item '" + prefabName + "'.");
        RegisteredTrophies.Add(prefabName);
        return prefabName;
    }

    private static string TrophyDonor(string donor) => donor switch
    {
        "Bat" => "TrophyBat",
        "Tick" => "TrophyTick",
        "Seeker" => "TrophySeeker",
        "SeekerBrute" => "TrophySeekerBrute",
        "Wolf" => "TrophyWolf",
        "Lox" => "TrophyLox",
        "Troll" => "TrophyTroll",
        "Serpent" => "TrophySerpent",
        "Leech" => "TrophyLeech",
        "Neck" => "TrophyNeck",
        "Wraith" => "TrophyWraith",
        "Fenring" => "TrophyFenring",
        "StoneGolem" => "TrophySGolem",
        "Hatchling" => "TrophyHatchling",
        "FrostWisp" => "TrophyHatchling",
        "Greydwarf_Shaman" => "TrophyGreydwarfShaman",
        "Greydwarf" => "TrophyGreydwarf",
        "Draugr" => "TrophyDraugr",
        "Draugr_Elite" => "TrophyDraugrElite",
        "DvergerMage" => "TrophyDvergr",
        "Skeleton" => "TrophySkeleton",
        "Charred_Melee" => "TrophyCharredMelee",
        _ => "TrophyDeer",
    };

    private static void TintTrophy(GameObject prefab, Color tint, string prefix)
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
                    material.SetColor("_EmissionColor", tint * .22f);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static float TrophyChance(string name) => name switch
    {
        "Crowncap Brute" or "Deep Hunter" or "Furnace Golem" or "Cryolith Guardian" or "Rift Colossus" => 20f,
        "Puffback" or "Abyss Shellback" or "Stonebound" or "Graft Warden" or "Corpse Orchard" => 15f,
        _ => 10f,
    };

    private static Drop[] Ordinary(string name) => name switch
    {
        "Lantern Moth" => D(R("SpireFibre"),1,1,65),
        "Sporeling" => D(R("GlowcapFlesh"),1,2,75),
        "Capcrawler" => D(R("SpireFibre"),1,2,85, R("Understone"),1,1,20),
        "Mycelial Stalker" => D(R("WorldrootTimber"),1,2,70, R("SpireFibre"),1,1,35),
        "Puffback" => D(R("GlowcapFlesh"),2,4,100, R("WorldrootTimber"),1,1,35),
        "Shelf Lurker" => D(R("Understone"),1,2,85, R("GlowcapFlesh"),1,1,25),
        "Crowncap Brute" => D(R("WorldrootTimber"),3,5,100, R("Understone"),2,3,75, R("GlowcapFlesh"),2,3,50),

        "Cave Ray" => D(R("PaleFibre"),1,1,70),
        "Gloomfin" => D(R("DeepSalt"),1,1,70),
        "Blackwater Lamprey" => D(R("DeepSalt"),1,2,75, R("BlackwaterPearl"),1,1,35),
        "Shoreclaw" => D(R("BlackwaterFlowstone"),1,2,80, R("BlackwaterPearl"),1,1,20),
        "Lantern Angler" => D(R("DeepSalt"),1,2,90, R("BlackwaterPearl"),1,1,30),
        "Abyss Shellback" => D(R("BlackwaterFlowstone"),2,4,100, R("BlackwaterPearl"),1,2,55),
        "Deep Hunter" => D(R("BlackwaterPearl"),2,3,100, R("DeepSalt"),2,4,80, R("BlackwaterFlowstone"),1,2,50),

        "Ashmite" => D(R("Sulfur"),1,1,70),
        "Cinder Hound" => D(R("CharredTimber"),1,2,65, R("Sulfur"),1,1,35),
        "Basalt Crawler" => D(R("Slagstone"),2,3,100, R("Sulfur"),1,1,40),
        "Vent Spitter" => D(R("Sulfur"),1,2,90),
        "Fume Wraith" => D(R("Sulfur"),1,2,80, R("Emberiron"),1,1,15),
        "Magma Leaper" => D(R("CharredTimber"),1,2,70, R("Emberiron"),1,1,35),
        "Furnace Golem" => D(R("Emberiron"),2,4,100, R("Slagstone"),3,5,100, R("Sulfur"),1,2,65),

        "Rime Moth" => D(R("ClearIce"),1,1,65),
        "Frost Tick" => D(R("ClearIce"),1,1,75),
        "Iceblind" => D(R("Rimewood"),1,2,70, R("ClearIce"),1,1,30),
        "Pale Burrower" => D(R("ClearIce"),1,2,65, R("Rimesilver"),1,1,25),
        "Rimewing" => D(R("ClearIce"),1,2,80, R("Rimesilver"),1,1,25),
        "Glacier Stalker" => D(R("Rimewood"),1,2,65, R("Rimesilver"),1,1,45),
        "Cryolith Guardian" => D(R("Rimesilver"),2,3,100, R("ClearIce"),2,4,100, R("Rimewood"),1,2,40),

        "Fracture Wisp" => D(R("FractureCrystal"),1,1,55),
        "Rift Skitter" => D(R("Shardstone"),1,2,80),
        "Shardwing" => D(R("FractureCrystal"),1,2,75, R("Shardstone"),1,1,35),
        "Gravity Leech" => D(R("FractureCrystal"),1,1,55, R("Shardstone"),1,1,35),
        "Chasm Stalker" => D(R("Shardstone"),1,2,80, R("Titanbone"),1,1,35),
        "Stonebound" => D(R("Shardstone"),2,4,100, R("Titanbone"),1,2,45),
        "Rift Colossus" => D(R("Titanbone"),3,5,100, R("FractureCrystal"),2,4,100, R("Shardstone"),3,5,100),

        "Rotling" => D(R("DecaySpore"),1,1,75),
        "Carrion Bloom" => D(R("DecaySpore"),1,2,90),
        "Spore Husk" => D(R("BoneGravel"),1,2,80, R("DecaySpore"),1,1,40),
        "Marrow Creeper" => D(R("BoneGravel"),1,2,75),
        "Decay Hound" => D(R("Rotwood"),1,2,65, R("BoneGravel"),1,1,35),
        "Graft Warden" => D(R("Rotwood"),2,3,100, R("CarrionAmber"),1,1,45),
        "Corpse Orchard" => D(R("DecaySpore"),2,4,100, R("CarrionAmber"),1,2,75, R("BoneGravel"),2,3,70),
        _ => throw new InvalidOperationException("No progression loot table exists for Underworld creature '" + name + "'."),
    };

    private static Drop[] Surtling(string element) => element switch
    {
        "Fire" => D(R("Sulfur"),1,2,75, R("Emberiron"),1,1,20),
        "Water" => D(R("DeepSalt"),1,2,75, R("BlackwaterPearl"),1,1,15),
        "Earth" => D(R("Shardstone"),1,2,80, R("Titanbone"),1,1,20),
        "Wind" => D(R("FractureCrystal"),1,1,50, R("Shardstone"),1,1,35),
        "Radiance" => D(R("FractureCrystal"),1,2,75),
        "Umbral" => D(R("DecaySpore"),1,2,55, R("CarrionAmber"),1,1,15),
        _ => throw new InvalidOperationException("No progression loot table exists for Surtling element '" + element + "'."),
    };

    private static Drop[] DeepFracture(string species) => species switch
    {
        "Annoyance Wisp" => D(R("FractureCrystal"),1,1,30),
        "Geode Crawler" => D(R("FractureCrystal"),1,1,70, R("Shardstone"),1,1,30),
        "Shardling" => D(R("Shardstone"),1,1,70),
        "Crystal Parasite" => D(R("FractureCrystal"),1,1,60),
        "Crystal Revenant" => D(R("FractureCrystal"),1,2,75, R("Titanbone"),1,1,15),
        "Facet Sentry" => D(R("FractureCrystal"),1,2,100),
        "Stone Sentinel" => D(R("Shardstone"),2,3,100, R("FractureCrystal"),1,1,30),
        "Crystal Hound" => D(R("FractureCrystal"),1,1,50, R("Titanbone"),1,1,20),
        "Burrower" => D(R("Shardstone"),1,2,80, R("Titanbone"),1,1,20),
        "Stone Guardian" => D(R("Shardstone"),2,4,100, R("Titanbone"),1,1,40),
        "Crystal Golem" => D(R("FractureCrystal"),2,3,100, R("Shardstone"),2,4,100),
        "Obelisk Warden" => D(R("Titanbone"),2,3,100, R("FractureCrystal"),2,3,100),
        "Deep Colossus" => D(R("Titanbone"),3,5,100, R("FractureCrystal"),3,5,100, R("Shardstone"),3,5,100),
        _ => throw new InvalidOperationException("No progression loot table exists for Deep Fracture species '" + species + "'."),
    };

    private static string R(string suffix) => "Magenheim_Underworld_Resource_" + suffix;
    private static string Id(string name) => name.Replace(" ", string.Empty).Replace("-", string.Empty);

    private static Drop[] D(params object[] values)
    {
        if (values.Length % 4 != 0) throw new InvalidOperationException("Invalid creature loot tuple.");
        var result = new Drop[values.Length / 4];
        for (var i = 0; i < result.Length; i++)
            result[i] = new Drop((string)values[i*4], (int)values[i*4+1], (int)values[i*4+2], Convert.ToSingle(values[i*4+3]));
        return result;
    }
}
