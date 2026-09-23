using System;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers the twelve elemental Surtlings as console-spawnable clones of humanoid donors, with the
/// authored body riding the donor's skeleton. Valheim keeps animation, attacks, AI, networking,
/// saving and loot; Magenheim supplies only the body and the name.
/// </summary>
internal sealed class UnderworldSurtlingRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    internal UnderworldSurtlingRegistrar(ManualLogSource log) => _log = log;
    internal void Register() => PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;

    private void RegisterContent()
    {
        Survey();
        var registered = 0;
        foreach (var entry in UnderworldSurtlings.All)
        {
            // One unavailable donor or model must not take the other elements down with it; the
            // failure is named in the log rather than swallowed.
            try
            {
                RegisterOne(entry);
                registered++;
            }
            catch (Exception exception)
            {
                _log.LogWarning($"Underworld Surtling {entry.Prefab} unavailable: {exception.Message}");
            }
        }
        _log.LogInfo($"Registered {registered}/{UnderworldSurtlings.All.Length} elemental Surtlings. Console spawn only; donor combat, AI and loot are unchanged.");
        Dispose();
    }

    private void RegisterOne(UnderworldSurtlings.Entry entry)
    {
        var donor = PrefabManager.Instance.GetPrefab(entry.Donor);
        if (!donor || !donor.GetComponent<Character>() || !donor.GetComponent<BaseAI>() || !donor.GetComponent<ZNetView>())
            throw new InvalidOperationException($"Donor {entry.Donor} is missing or lacks its native creature components.");
        if (PrefabManager.Instance.GetPrefab(entry.Prefab))
            throw new InvalidOperationException($"Prefab identity {entry.Prefab} is already occupied.");
        var rig = ModelAssets.Rig(entry.ModelId) ?? throw new InvalidOperationException($"{entry.ModelId} declares no canonical rig.");

        var clone = PrefabManager.Instance.CreateClonedPrefab(entry.Prefab, donor);
        try
        {
            var character = clone.GetComponent<Character>();
            character.m_name = entry.DisplayName;
            clone.transform.localScale *= entry.Scale;
            var stripped = StripArmour(clone.GetComponent<Humanoid>());
            var arrange = HumanoidSegmentBinder.Arranger(clone, rig, entry.Girth, out var report);
            ModelAssets.Load(clone, entry.ModelId, arrange: arrange);
            var ragdolls = BindRagdolls(entry, character, donor, rig);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
            _log.LogInfo($"Underworld Surtling {entry.Prefab} on {entry.Donor}: {report()}; {ragdolls} death ragdoll(s) re-bodied; {stripped} donor armour visual(s) removed. Home: {entry.Home}.");
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(clone);
            throw;
        }
    }

    /// <summary>
    /// The donor's death effect spawns its own ragdoll, which would drop the donor's corpse where the
    /// Surtling fell. Each ragdoll is cloned under a namespaced identity, given the same body, and
    /// substituted in this clone's death effects. The clone's EffectList is its own serialized copy,
    /// so the donor's is untouched.
    /// </summary>
    private int BindRagdolls(UnderworldSurtlings.Entry entry, Character character, GameObject donor, Newtonsoft.Json.Linq.JToken rig)
    {
        var effects = character.m_deathEffects?.m_effectPrefabs;
        if (effects is null) return 0;
        var bound = 0;
        foreach (var effect in effects)
        {
            // Some donors (Charred, Skeleton) carry the Ragdoll on a child of the death effect.
            if (effect?.m_prefab is null || !effect.m_prefab.GetComponentInChildren<Ragdoll>(true)) continue;
            var name = entry.Prefab + "_ragdoll";
            if (PrefabManager.Instance.GetPrefab(name)) throw new InvalidOperationException($"Prefab identity {name} is already occupied.");
            var ragdoll = PrefabManager.Instance.CreateClonedPrefab(name, effect.m_prefab);
            ragdoll.transform.localScale *= entry.Scale;
            var arrange = HumanoidSegmentBinder.Arranger(ragdoll, rig, entry.Girth, out _, donor);
            ModelAssets.Load(ragdoll, entry.ModelId, arrange: arrange);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(ragdoll, true));
            effect.m_prefab = ragdoll;
            bound++;
        }
        return bound;
    }

    /// <summary>
    /// Drops the donor's worn armour from this clone's loadout so a Charred hip cloth or a Dvergr suit
    /// does not render over the elemental body. Weapons and attack items are kept: they are the
    /// donor's combat. The arrays are the clone's own serialized copies; the donor is untouched.
    /// </summary>
    private static int StripArmour(Humanoid? humanoid)
    {
        if (humanoid is null) return 0;
        var removed = 0;
        GameObject[] Keep(GameObject[]? items)
        {
            if (items is null) return Array.Empty<GameObject>();
            var kept = items.Where(item => !IsArmour(item)).ToArray();
            removed += items.Length - kept.Length;
            return kept;
        }
        humanoid.m_defaultItems = Keep(humanoid.m_defaultItems);
        humanoid.m_randomArmor = Keep(humanoid.m_randomArmor);
        foreach (var set in humanoid.m_randomSets ?? Array.Empty<Humanoid.ItemSet>())
            set.m_items = Keep(set.m_items);
        return removed;
    }

    private static bool IsArmour(GameObject item)
    {
        var type = item ? item.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_itemType : null;
        return type is ItemDrop.ItemData.ItemType.Helmet or ItemDrop.ItemData.ItemType.Chest or
            ItemDrop.ItemData.ItemType.Legs or ItemDrop.ItemData.ItemType.Shoulder or ItemDrop.ItemData.ItemType.Utility;
    }

    /// <summary>Logs, per candidate donor, what the binder and the roster depend on.</summary>
    private void Survey()
    {
        foreach (var name in UnderworldSurtlings.DonorCandidates)
        {
            var prefab = PrefabManager.Instance.GetPrefab(name);
            if (!prefab) { _log.LogInfo($"Surtling donor survey: {name} absent."); continue; }
            var animator = prefab.GetComponentInChildren<Animator>(true);
            var avatar = animator ? animator.avatar : null;
            var human = avatar && avatar.isHuman;
            var character = prefab.GetComponent<Character>();
            var humanoid = prefab.GetComponent<Humanoid>();
            var weapons = humanoid is null ? "none" : string.Join(",",
                (humanoid.m_defaultItems ?? Array.Empty<GameObject>()).Concat(humanoid.m_randomWeapon ?? Array.Empty<GameObject>())
                .Where(item => item).Select(item => item.name).Distinct());
            var ragdoll = character?.m_deathEffects?.m_effectPrefabs?.Any(e => e?.m_prefab && e.m_prefab.GetComponentInChildren<Ragdoll>(true)) == true;
            var deaths = string.Join(",", (character?.m_deathEffects?.m_effectPrefabs ?? Array.Empty<EffectList.EffectData>())
                .Where(e => e?.m_prefab).Select(e => e.m_prefab.name));
            var detail = "";
            if (human)
            {
                try { detail = " " + HumanoidSegmentBinder.DonorSkeleton.Resolve(prefab).Count + " roles"; }
                catch (Exception exception) { detail = " roles unresolved: " + exception.Message; }
            }
            _log.LogInfo($"Surtling donor survey: {name} avatar={(avatar ? avatar.name : "none")} humanoid={human}{detail} " +
                         $"faction={character?.m_faction} ragdoll={ragdoll} death=[{deaths}] weapons=[{weapons}]");
        }
    }

    public void Dispose() => PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
}
