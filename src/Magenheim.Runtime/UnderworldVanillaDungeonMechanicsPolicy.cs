using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// DDE-08 donor-mechanics boundary. Structural mechanics survive by default because the point of
/// these dungeons is to remain recognizably vanilla-derived. Only known Surface lore/progression
/// components are stripped here; population and reward ownership are handled by their own policies.
/// </summary>
internal static class UnderworldVanillaDungeonMechanicsPolicy
{
    internal readonly record struct Stats(
        int DoorsPreserved,
        int TeleportsPreserved,
        int RandomSpawnsPreserved,
        int DestructiblesPreserved,
        int LoreMarkersRemoved,
        int OtherComponentsPreserved)
    {
        internal static Stats Empty => new(0,0,0,0,0,0);

        public static Stats operator +(Stats left, Stats right) =>
            new(
                left.DoorsPreserved + right.DoorsPreserved,
                left.TeleportsPreserved + right.TeleportsPreserved,
                left.RandomSpawnsPreserved + right.RandomSpawnsPreserved,
                left.DestructiblesPreserved + right.DestructiblesPreserved,
                left.LoreMarkersRemoved + right.LoreMarkersRemoved,
                left.OtherComponentsPreserved + right.OtherComponentsPreserved);
    }

    internal static Stats Apply(
        GameObject roomObject,
        UnderworldVanillaDungeonReuseDefinition profile,
        string donorRoomName)
    {
        if (!roomObject) throw new ArgumentNullException(nameof(roomObject));
        if (string.IsNullOrWhiteSpace(donorRoomName))
            throw new ArgumentException("Donor room identity is required.", nameof(donorRoomName));

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var component in roomObject.GetComponentsInChildren<Component>(true))
        {
            if (!component || component is Transform) continue;
            var name = component.GetType().Name;
            counts[name] = counts.TryGetValue(name, out var current) ? current + 1 : 1;
        }

        // Explicitly remove only Surface-facing discovery/lore systems we know are semantically
        // wrong in an Underworld derivative. Doors, barriers, hidden-route RandomSpawn objects,
        // Teleports, Destructibles, traps and unknown donor mechanics are deliberately preserved.
        var vegvisirs = roomObject.GetComponentsInChildren<Vegvisir>(true);
        foreach (var vegvisir in vegvisirs)
            UnityEngine.Object.DestroyImmediate(vegvisir);

        var runestones = roomObject.GetComponentsInChildren<RuneStone>(true);
        foreach (var runestone in runestones)
            UnityEngine.Object.DestroyImmediate(runestone);

        var knownPreserved =
            Count(counts, "Door") +
            Count(counts, "Teleport") +
            Count(counts, "RandomSpawn") +
            Count(counts, "Destructible");
        var totalComponents = 0;
        foreach (var pair in counts) totalComponents += pair.Value;

        return new Stats(
            Count(counts, "Door"),
            Count(counts, "Teleport"),
            Count(counts, "RandomSpawn"),
            Count(counts, "Destructible"),
            vegvisirs.Length + runestones.Length,
            Math.Max(0, totalComponents - knownPreserved - vegvisirs.Length - runestones.Length));
    }

    private static int Count(IReadOnlyDictionary<string, int> counts, string name) =>
        counts.TryGetValue(name, out var value) ? value : 0;
}
