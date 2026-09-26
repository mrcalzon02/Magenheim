using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Shared runtime composition boundary for every native Underworld CustomLocation. Valheim owns
/// placement and generated-zone persistence; this component only asks the existing family composer
/// to build the location's contents after the location exists in the Underworld scene.
/// </summary>
internal sealed class UnderworldNativeStructureLocationRuntime : MonoBehaviour
{
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static readonly Dictionary<string, IUnderworldBiomeStructureFamily> Families =
        new(StringComparer.Ordinal);

    [SerializeField] private string _familyKind = string.Empty;
    private bool _composed;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (_services is not null && !ReferenceEquals(_services, services)) Families.Clear();
        _services = services;
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal static void RegisterFamily(IUnderworldBiomeStructureFamily family)
    {
        if (family is null) throw new ArgumentNullException(nameof(family));
        if (Families.TryGetValue(family.Kind, out var existing) && !ReferenceEquals(existing, family))
            throw new InvalidOperationException($"Native Underworld location family '{family.Kind}' is already registered.");
        Families[family.Kind] = family;
    }

    internal void Bind(string familyKind)
    {
        if (string.IsNullOrWhiteSpace(familyKind))
            throw new ArgumentException("Underworld location family kind is required.", nameof(familyKind));
        _familyKind = familyKind;
    }

    private void Start()
    {
        if (_composed || _services is null || string.IsNullOrWhiteSpace(_familyKind)) return;
        if (!_services.WorldInstances.TryGetContextForScene(gameObject.scene.handle, out var context) ||
            context is null || !context.InstanceId.IsUnderworld)
            return;
        if (_services.InstanceLifecycle.Identity is not { } identity)
            return;
        if (!Families.TryGetValue(_familyKind, out var family))
            throw new InvalidOperationException($"Unknown native Underworld location family '{_familyKind}'.");

        var grid = new UnderworldInstanceChunkGrid(_services.TerrainDomain);
        var key = grid.KeyAt(transform.position.x, transform.position.z);
        if (!grid.IntersectsPlayableDomain(key))
        {
            _log?.LogWarning(
                $"Native location '{name}' landed outside the playable Underworld terrain domain; content was not composed.");
            return;
        }

        GameObject? content = null;
        try
        {
            using (ValheimWorldInstanceExecution.Enter(context))
                content = family.Compose(transform.position, identity, key);
            content.transform.SetParent(transform, worldPositionStays: true);
            _composed = true;
            _log?.LogInfo(
                $"Composed native Underworld location '{_familyKind}' at chunk {key.X},{key.Z}.");
        }
        catch
        {
            if (content) Destroy(content);
            throw;
        }
    }
}
