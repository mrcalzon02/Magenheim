using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Restores only Magenheim dungeon derivatives accidentally saved on the Surface.</summary>
internal static class UnderworldSurfaceDungeonIsolation
{
    private static readonly System.Reflection.FieldInfo Instances =
        AccessTools.Field(typeof(ZoneSystem), "m_locationInstances")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "m_locationInstances");

    internal static bool IsSurface(ZoneSystem zone) => zone &&
        !zone.gameObject.scene.name.StartsWith("Magenheim_Underworld_", StringComparison.Ordinal);

    internal static ZoneSystem.ZoneLocation Restore(ZoneSystem zone, ZoneSystem.ZoneLocation location)
    {
        if (!IsSurface(zone)) return location;
        var donor = UnderworldDungeonBoundaryPolicy.SurfaceDonorFor(UnderworldZoneCatalogGuard.LocationName(location));
        if (donor is null) return location;
        return zone.m_locations.FirstOrDefault(row => row is not null &&
            UnderworldZoneCatalogGuard.LocationName(row) == donor)
            ?? throw new InvalidOperationException("Surface dungeon recovery cannot find native donor " + donor);
    }

    internal static int RestoreHash(ZoneSystem zone, int hash)
    {
        if (!IsSurface(zone)) return hash;
        foreach (var definition in UnderworldDungeonCatalog.All)
        {
            if (definition.PrefabName.GetStableHashCode() != hash) continue;
            var donor = UnderworldDungeonBoundaryPolicy.SurfaceDonorFor(definition.PrefabName);
            if (donor is not null) return donor.GetStableHashCode();
        }
        return hash;
    }

    internal static void RepairSavedInstances(ZoneSystem zone)
    {
        if (!IsSurface(zone) || ZNet.instance is null || !ZNet.instance.IsServer()) return;
        if (Instances.GetValue(zone) is not IDictionary instances)
            throw new InvalidOperationException("Native location instance index is unavailable.");
        var repaired = RestoreSavedEntries(instances, location => Restore(zone, location));
        if (repaired > 0)
            Debug.Log($"Magenheim restored {repaired} leaked Surface dungeon sites to their native donor identities; positions and placed-state retained.");
    }

    internal static int RestoreSavedEntries(IDictionary instances,
        Func<ZoneSystem.ZoneLocation, ZoneSystem.ZoneLocation> restore)
    {
        var updates = new System.Collections.Generic.List<DictionaryEntry>();
        foreach (DictionaryEntry entry in instances)
        {
            if (entry.Value is not ZoneSystem.LocationInstance instance || instance.m_location is null) continue;
            var restored = restore(instance.m_location);
            if (ReferenceEquals(restored, instance.m_location)) continue;
            instance.m_location = restored;
            // LocationInstance is a native value type. Write the changed copy back
            // after enumeration; mutating its boxed copy does not update the save index.
            updates.Add(new DictionaryEntry(entry.Key, instance));
        }
        foreach (var update in updates) instances[update.Key] = update.Value;
        return updates.Count;
    }
}

[HarmonyPatch(typeof(ZoneSystem), "SpawnLocation")]
internal static class UnderworldSurfaceDungeonSpawnPatch
{
    private static void Prefix(ZoneSystem __instance, ref ZoneSystem.ZoneLocation __0) =>
        __0 = UnderworldSurfaceDungeonIsolation.Restore(__instance, __0);
}

[HarmonyPatch(typeof(ZoneSystem), "SpawnProxyLocation")]
internal static class UnderworldSurfaceDungeonProxyPatch
{
    private static void Prefix(ZoneSystem __instance, ref int __0) =>
        __0 = UnderworldSurfaceDungeonIsolation.RestoreHash(__instance, __0);
}

[HarmonyPatch(typeof(LocationProxy), "SpawnLocation")]
internal static class UnderworldSurfaceDungeonProxySavePatch
{
    private const string RetainedLayout = "magenheim.recoveredDungeonEnvelope";

    private static void Prefix(LocationProxy __instance)
    {
        if (!__instance) return;
        var zone = ZoneSystem.instance;
        if (!zone || zone.gameObject.scene.handle != __instance.gameObject.scene.handle) return;
        var view = __instance.GetComponent<ZNetView>();
        if (!view || !view.IsValid() || !view.IsOwner()) return;
        var zdo = view.GetZDO();
        var hash = zdo.GetInt(ZDOVars.s_location);
        var restored = UnderworldSurfaceDungeonIsolation.RestoreHash(zone, hash);
        if (restored != hash)
        {
            // Retain only this site's original enclosure requirement, not another
            // dungeon save. Valheim continues to own its rooms, loot and picked-state.
            zdo.Set(RetainedLayout, hash);
            zdo.Set(ZDOVars.s_location, restored);
        }
    }

    private static void Postfix(LocationProxy __instance, bool __result)
    {
        if (!__result || !__instance || !UnderworldSurfaceDungeonIsolation.IsSurface(ZoneSystem.instance)) return;
        var view = __instance.GetComponent<ZNetView>();
        if (!view || !view.IsValid()) return;
        var original = view.GetZDO().GetInt(RetainedLayout);
        var definition = UnderworldDungeonCatalog.All.FirstOrDefault(value => value.PrefabName.GetStableHashCode() == original);
        if (definition is null) return;
        var profile = UnderworldVanillaDungeonReuseCatalog.All.FirstOrDefault(value => value.DungeonId == definition.Id);
        var location = __instance.GetComponentInChildren<Location>();
        if (profile is null || !location || !location.m_generator) return;
        var envelope = location.GetComponent<UnderworldDungeonInteriorEnvelope>()
            ?? location.gameObject.AddComponent<UnderworldDungeonInteriorEnvelope>();
        envelope.BindRetainedLayout(location.m_generator, profile);
        envelope.Apply(location);
    }
}
