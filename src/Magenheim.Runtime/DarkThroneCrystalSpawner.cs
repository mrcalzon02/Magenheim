using System;
using System.Collections.Generic;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Server-owned crystal ecology node for the Dark Throne. Spawn timing and population identity live
/// in ZDO state so unloading/reloading the location cannot silently reset the node into duplicate waves.
/// Encounter runtime may suspend a node while the King is engaged.
/// </summary>
internal sealed class DarkThroneCrystalSpawner : MonoBehaviour
{
    private const string SpawnerIdentityKey = "magenheim.darkthrone.spawner.id";
    private const string SpawnOwnerKey = "magenheim.darkthrone.spawn.owner";
    private const string NextSpawnTicksKey = "magenheim.darkthrone.spawner.next";
    private const string SuspendedKey = "magenheim.darkthrone.spawner.suspended";

    private static readonly string[] LesserFamilies =
    {
        DeepFractureCreatureRegistrar.AnnoyanceWispPrefix,
        DeepFractureCreatureRegistrar.GeodeCrawlerPrefix,
        DeepFractureCreatureRegistrar.ShardlingPrefix,
        DeepFractureCreatureRegistrar.CrystalParasitePrefix,
        DeepFractureCreatureRegistrar.CrystalHoundPrefix,
    };

    private static readonly string[] GuardianFamilies =
    {
        DeepFractureCreatureRegistrar.CrystalRevenantPrefix,
        DeepFractureCreatureRegistrar.FacetSentryPrefix,
        DeepFractureCreatureRegistrar.StoneSentinelPrefix,
        DeepFractureCreatureRegistrar.StoneGuardianPrefix,
        DeepFractureCreatureRegistrar.CrystalGolemPrefix,
    };

    private ZNetView _view;
    private bool _guardian;
    private int _maxAlive;
    private float _radius;
    private float _respawnSeconds;

    internal void Configure(bool guardian, int maxAlive, float radius, float respawnSeconds)
    {
        if (maxAlive < 1) throw new ArgumentOutOfRangeException(nameof(maxAlive));
        if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius)) throw new ArgumentOutOfRangeException(nameof(radius));
        if (respawnSeconds < 1f || float.IsNaN(respawnSeconds) || float.IsInfinity(respawnSeconds)) throw new ArgumentOutOfRangeException(nameof(respawnSeconds));
        _guardian = guardian;
        _maxAlive = maxAlive;
        _radius = radius;
        _respawnSeconds = respawnSeconds;
    }

    internal void SetEncounterSuspended(bool suspended)
    {
        if (!HasAuthority()) return;
        var zdo = _view.GetZDO();
        zdo.Set(SuspendedKey, suspended);
        if (!suspended)
            zdo.Set(NextSpawnTicksKey, ZNet.instance.GetTime().AddSeconds(3d).Ticks);
    }

    private void Awake()
    {
        _view = GetComponent<ZNetView>();
        if (_maxAlive == 0) Configure(false, 2, 8f, 75f);
    }

    private void Start()
    {
        if (!HasAuthority()) return;
        EnsurePersistentIdentityAndTimer();
    }

    private void Update()
    {
        if (!HasAuthority()) return;
        var zdo = _view.GetZDO();
        if (zdo.GetBool(SuspendedKey, false)) return;

        EnsurePersistentIdentityAndTimer();
        var now = ZNet.instance.GetTime();
        var nextTicks = zdo.GetLong(NextSpawnTicksKey, 0L);
        if (nextTicks > now.Ticks) return;

        var identity = zdo.GetString(SpawnerIdentityKey, string.Empty);
        if (CountLivingOwnedCreatures(identity) >= _maxAlive)
        {
            // Do not poll every frame while the node is already at capacity. This timer is persisted,
            // so a zone reload cannot turn the capacity check into an immediate extra wave.
            zdo.Set(NextSpawnTicksKey, now.AddSeconds(Math.Max(10d, _respawnSeconds * 0.25d)).Ticks);
            return;
        }

        if (SpawnOne(identity))
            zdo.Set(NextSpawnTicksKey, now.AddSeconds(_respawnSeconds).Ticks);
        else
            zdo.Set(NextSpawnTicksKey, now.AddSeconds(15d).Ticks);
    }

    private bool HasAuthority()
    {
        return _view != null && _view.IsValid() && _view.IsOwner() && ZNet.instance != null;
    }

    private void EnsurePersistentIdentityAndTimer()
    {
        var zdo = _view.GetZDO();
        if (string.IsNullOrWhiteSpace(zdo.GetString(SpawnerIdentityKey, string.Empty)))
        {
            // World-space quantization is stable for a generated location and avoids coupling persistence
            // to transient Unity instance IDs. Dark Throne itself is unique per world by location policy.
            var p = transform.position;
            var identity = string.Concat(
                gameObject.name, ":",
                Mathf.RoundToInt(p.x * 10f), ":",
                Mathf.RoundToInt(p.y * 10f), ":",
                Mathf.RoundToInt(p.z * 10f));
            zdo.Set(SpawnerIdentityKey, identity);
        }

        if (zdo.GetLong(NextSpawnTicksKey, 0L) == 0L)
            zdo.Set(NextSpawnTicksKey, ZNet.instance.GetTime().AddSeconds(UnityEngine.Random.Range(4f, 10f)).Ticks);
    }

    private int CountLivingOwnedCreatures(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) return 0;
        var count = 0;
        foreach (var character in Character.GetAllCharacters())
        {
            if (character == null || character.IsDead()) continue;
            var nview = character.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) continue;
            if (string.Equals(nview.GetZDO().GetString(SpawnOwnerKey, string.Empty), identity, StringComparison.Ordinal))
                count++;
        }
        return count;
    }

    private bool SpawnOne(string identity)
    {
        var families = _guardian ? GuardianFamilies : LesserFamilies;
        var alignments = (ElementalAlignment[])Enum.GetValues(typeof(ElementalAlignment));
        if (families.Length == 0 || alignments.Length == 0 || string.IsNullOrWhiteSpace(identity)) return false;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var family = families[UnityEngine.Random.Range(0, families.Length)];
            var alignment = alignments[UnityEngine.Random.Range(0, alignments.Length)];
            var prefab = PrefabManager.Instance.GetPrefab(family + alignment);
            if (prefab == null) continue;

            var offset = UnityEngine.Random.insideUnitCircle * _radius;
            var point = transform.position + new Vector3(offset.x, 1.5f, offset.y);
            if (Physics.Raycast(point, Vector3.down, out var hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                point = hit.point + Vector3.up * 0.15f;

            var spawned = Instantiate(prefab, point, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            var spawnedView = spawned.GetComponent<ZNetView>();
            if (spawnedView == null || !spawnedView.IsValid())
            {
                Destroy(spawned);
                continue;
            }
            spawnedView.GetZDO().Set(SpawnOwnerKey, identity);
            return true;
        }
        return false;
    }
}

internal static class DarkThroneCrystalSpawnerFactory
{
    internal static GameObject Create(Transform parent, string name, Vector3 localPosition, bool guardian, int maxAlive, float radius, float respawnSeconds)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        var node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;
        node.AddComponent<ZNetView>();
        var spawner = node.AddComponent<DarkThroneCrystalSpawner>();
        spawner.Configure(guardian, maxAlive, radius, respawnSeconds);
        return node;
    }
}
