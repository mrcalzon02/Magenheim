using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Shared admission boundary for repeatable native Underworld structures.
/// Placement identity is derived exclusively from the native chunk grid plus deterministic slot;
/// Valheim ZoneSystem/ZDO identity and Surface coordinates never participate in ownership.
/// </summary>
internal sealed class UnderworldStructureAdmissionController
{
    private readonly UnderworldGeneratedObjectStateStore _store;
    private readonly ManualLogSource _log;

    internal UnderworldStructureAdmissionController(UnderworldGeneratedObjectStateStore store, ManualLogSource log)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal GameObject Admit(
        UnderworldWorldIdentity identity,
        string structureKind,
        UnderworldInstanceChunkKey chunk,
        int placementSlot,
        bool authoritativeServer,
        Func<GameObject> compose,
        out bool restored)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (string.IsNullOrWhiteSpace(structureKind)) throw new ArgumentException("Underworld structure kind is required.", nameof(structureKind));
        if (compose is null) throw new ArgumentNullException(nameof(compose));

        var placementKey = UnderworldStructurePlacementKey.For(chunk, placementSlot);
        restored = _store.IsRecorded(identity, structureKind, placementKey);
        GameObject? candidate = null;
        try
        {
            candidate = compose();
            if (!candidate) throw new InvalidOperationException("Underworld structure composer returned no root object.");

            var ownership = candidate.GetComponent<UnderworldGeneratedObjectIdentity>();
            if (ownership is null) ownership = candidate.AddComponent<UnderworldGeneratedObjectIdentity>();
            ownership.Bind(identity, structureKind, placementKey);

            if (authoritativeServer && !restored)
                _store.RecordGenerated(identity, structureKind, placementKey);

            _log.LogDebug($"{(restored ? "Restored" : "Generated")} native Underworld structure '{structureKind}' at {placementKey} ({ownership.StableObjectId}).");
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
