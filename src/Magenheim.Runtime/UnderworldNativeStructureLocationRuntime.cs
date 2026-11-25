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

    internal string FamilyKind => _familyKind;
    internal bool IsComposed => _composed;

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
            UnderworldBossLocationRegistrar.BossLocation? bossLocation = null;
            foreach (var boss in UnderworldBossLocationRegistrar.Locations)
                if (string.Equals(boss.Family.Kind, _familyKind, StringComparison.Ordinal)) bossLocation = boss;
            if (bossLocation is not null && UnderworldArrivalBossMigration.IsRetired(
                    context.ZoneSystem, bossLocation.PrefabName, transform.position))
            {
                _log?.LogInfo("Retired obsolete boss location proxy: " + bossLocation.PrefabName);
                _composed = true;
                return;
            }
            var position = transform.position;
            var terrain = UnderworldTerrainRuntime.SampleForDiagnostics(position.x, position.z);
            if (!terrain.Admitted) return;
            var groundMask = LayerMask.GetMask("terrain");
            var domain = _services.TerrainDomain;
            var rayStart = (float)domain.MaximumY + 1f;
            var rayLength = (float)(domain.MaximumY - domain.MinimumY) + 2f;
            double GroundHeight(double x, double z)
            {
                var probe = new Vector3((float)x, rayStart, (float)z);
                if (context.PhysicsScene.Raycast(probe, Vector3.down, out var hit, rayLength, groundMask,
                        QueryTriggerInteraction.Ignore))
                    return hit.point.y;

                return UnderworldBossSite.GeneratedHeight(x, z);
            }

            var centerHeight = GroundHeight(position.x, position.z);
            position.y = (float)centerHeight;
            using (ValheimWorldInstanceExecution.Enter(context))
            {
                if (bossLocation is null)
                {
                    content = family.Compose(position, identity, key);
                }
                else
                {
                    var drownedRing = family.Biome == UnderworldTerrainBiome.BlackwaterDeep;
                    var waterLevel = context.ZoneSystem.m_waterLevel;
                    if (!UnderworldBossSite.TryGetGrade(family.Biome, position, waterLevel, GroundHeight,
                            out var origin, out var footing))
                    {
                        _log?.LogWarning($"Skipped arena '{_familyKind}' at ({position.x:F1}, {position.z:F1}) because its loaded ground is unsuitable.");
                        return;
                    }
                    position.y = (float)origin;
                    content = UnderworldBiomeBossRegistrar.ComposeArena(
                        bossLocation.CanonicalLocationId, position, transform.rotation);
                    UnderworldBiomeBossRegistrar.ExtendFoundation(content, (float)footing);
                    var spawnHeight = drownedRing ? waterLevel - UnderworldArenaGrounding.BossSwimmingDepth : position.y + 1.5f;
                    content.AddComponent<UnderworldBiomeBossArenaRuntime>()
                        .Bind(bossLocation.CanonicalLocationId, spawnHeight);
                }
            }
            content.transform.SetParent(transform, worldPositionStays: true);
            _composed = true;
            _log?.LogInfo(
                $"Composed native Underworld location '{_familyKind}' at chunk {key.X},{key.Z}, " +
                $"origin Y {position.y:F2}, center terrain Y {centerHeight:F2}.");
        }
        catch
        {
            if (content) Destroy(content);
            throw;
        }
    }
}
