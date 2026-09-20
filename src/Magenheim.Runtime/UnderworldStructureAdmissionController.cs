using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Thin composition boundary for deterministic native Underworld structures.
/// It assigns Magenheim instance identity to presentation objects but deliberately owns no
/// persistence, player lifecycle, network lifecycle, or replacement world-object database.
/// </summary>
internal sealed class UnderworldStructureAdmissionController
{
    private readonly ManualLogSource _log;

    internal UnderworldStructureAdmissionController(ManualLogSource log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal GameObject Admit(
        UnderworldWorldIdentity identity,
        string structureKind,
        UnderworldInstanceChunkKey chunk,
        int placementSlot,
        Func<GameObject> compose)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (string.IsNullOrWhiteSpace(structureKind)) throw new ArgumentException("Underworld structure kind is required.", nameof(structureKind));
        if (compose is null) throw new ArgumentNullException(nameof(compose));

        var placementKey = UnderworldStructurePlacementKey.For(chunk, placementSlot);
        GameObject? candidate = null;
        try
        {
            candidate = compose();
            if (!candidate) throw new InvalidOperationException("Underworld structure composer returned no root object.");

            var ownership = candidate.GetComponent<UnderworldGeneratedObjectIdentity>();
            if (ownership is null) ownership = candidate.AddComponent<UnderworldGeneratedObjectIdentity>();
            ownership.Bind(identity, structureKind, placementKey);

            _log.LogDebug($"Composed native Underworld structure '{structureKind}' at {placementKey} ({ownership.StableObjectId}).");
            var admitted = candidate;
            candidate = null;
            return admitted;
        }
        catch
        {
            if (candidate) UnityEngine.Object.Destroy(candidate);
            throw;
        }
    }
}
