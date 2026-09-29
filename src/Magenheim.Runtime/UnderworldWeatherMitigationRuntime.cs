using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Resolves already-owned progression into the two atmosphere mitigation verbs. Deep Boons and
/// admitted biome armour contribute resistance; the runtime-admitted Defiant Censer contributes
/// Great Decay suppression while held. No progression unlock logic lives here.
/// </summary>
internal static class UnderworldWeatherMitigationRuntime
{
    private const double ArmourResistancePerPiece = 0.09d;
    private const double MatchingDeepBoonResistance = 0.36d;
    private const double DefiantCenserSuppression = 0.62d;
    private const float DefiantCenserRadiusMeters = 16f;
    private const string DefiantCenserPrefab = "Magenheim_Underworld_Tool_DefiantCenser";

    internal readonly record struct Result(double Resistance01, double Suppression01);

    internal static Result Resolve(Player player, UnderworldTerrainBiome biome)
    {
        if (player is null) return default;

        var resistance = MatchingDeepBoon(player, biome) ? MatchingDeepBoonResistance : 0d;
        var armourPieces = 0;
        foreach (var item in player.GetInventory().GetEquippedItems())
        {
            if (item is null || item.m_dropPrefab is null) continue;
            var prefab = item.m_dropPrefab.name;
            foreach (var definition in UnderworldEquipmentCatalog.Armour)
            {
                if (definition.Biome != biome ||
                    !string.Equals(definition.Prefab, prefab, StringComparison.Ordinal))
                    continue;
                armourPieces++;
                break;
            }
        }

        resistance += armourPieces * ArmourResistancePerPiece;

        // The Censer is a carried local-clearing source, not a passive inventory bonus. Any
        // player standing inside an active Censer's radius receives the Great Decay suppression;
        // this makes it useful for a small worksite/base rather than only the carrier.
        var suppression = biome == UnderworldTerrainBiome.GreatDecay && HasActiveCenserNearby(player)
            ? DefiantCenserSuppression
            : 0d;

        return new Result(
            Math.Min(.78d, Math.Max(0d, resistance)),
            Math.Min(.80d, Math.Max(0d, suppression)));
    }

    private static bool HasActiveCenserNearby(Player player)
    {
        var radiusSq = DefiantCenserRadiusMeters * DefiantCenserRadiusMeters;
        foreach (var candidate in Player.GetAllPlayers())
        {
            if (candidate is null || candidate.IsDead() ||
                candidate.gameObject.scene.handle != player.gameObject.scene.handle ||
                (candidate.transform.position - player.transform.position).sqrMagnitude > radiusSq)
                continue;

            var held = candidate.GetCurrentWeapon();
            if (held?.m_dropPrefab is not null &&
                string.Equals(held.m_dropPrefab.name, DefiantCenserPrefab, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static bool MatchingDeepBoon(Player player, UnderworldTerrainBiome biome)
    {
        var id = biome switch
        {
            UnderworldTerrainBiome.FungalForest => "spore_communion",
            UnderworldTerrainBiome.BlackwaterDeep => "deep_current",
            UnderworldTerrainBiome.SulfurousWastes => "furnace_blood",
            UnderworldTerrainBiome.FrozenCaverns => "rimebound",
            UnderworldTerrainBiome.FractureZones => "stone_anchor",
            UnderworldTerrainBiome.GreatDecay => "defiant_flesh",
            _ => string.Empty,
        };
        return id.Length > 0 && DeepBoonRuntime.IsActive(player, id);
    }
}
