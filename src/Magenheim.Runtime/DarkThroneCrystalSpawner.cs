using System;
using Jotunn.Managers;
using Magenheim.Core;
using Magenheim.Core.DarkThrone;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Server-owned crystal ecology node for the Dark Throne. Spawn timing and population identity live
/// in ZDO state so unloading/reloading the location cannot silently reset the node into duplicate waves.
/// Creature-family and population policy comes from the shared core profile authority.
/// </summary>
internal sealed class DarkThroneCrystalSpawner : MonoBehaviour
{
    private const string SpawnerIdentityKey = "magenheim.darkthrone.spawner.id";
    private const string SpawnOwnerKey = "magenheim.darkthrone.spawn.owner";
    private const string NextSpawnTicksKey = "magenheim.darkthrone.spawner.next";
    private const string SuspendedKey = "magenheim.darkthrone.spawner.suspended";

    private ZNetView _view;
    private CrystalCreatureSpawnProfile _profile;

    internal void Configure(CrystalCreatureSpawnProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _profile.Validate();
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
    }

    private void Start()
    {
        if (_profile == null)
        {
            Debug.LogError($"Dark Throne crystal spawner '{name}' has no spawn profile and will remain inert.");
            enabled = false;
            return;
        }
        if (!HasAuthority()) return;
        EnsurePersistentIdentityAndTimer();
    }

    private void Update()
    {
        if (_profile == null || !HasAuthority()) return;
        var zdo = _view.GetZDO();
        if (zdo.GetBool(SuspendedKey, false)) return;

        EnsurePersistentIdentityAndTimer();
        var now = ZNet.instance.GetTime();
        var nextTicks = zdo.GetLong(NextSpawnTicksKey, 0L);
        if (nextTicks > now.Ticks) return;

        var identity = zdo.GetString(SpawnerIdentityKey, string.Empty);
        if (CountLivingOwnedCreatures(identity) >= _profile.MaximumAlive)
        {
            zdo.Set(NextSpawnTicksKey, now.AddSeconds(Math.Max(10d, _profile.RespawnSeconds * 0.25d)).Ticks);
            return;
        }

        if (SpawnOne(identity))
            zdo.Set(NextSpawnTicksKey, now.AddSeconds(_profile.RespawnSeconds).Ticks);
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
        var alignments = (ElementalAlignment[])Enum.GetValues(typeof(ElementalAlignment));
        var families = _profile.PrefabPrefixes;
        if (families.Count == 0 || alignments.Length == 0 || string.IsNullOrWhiteSpace(identity)) return false;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var family = families[UnityEngine.Random.Range(0, families.Count)];
            var alignment = alignments[UnityEngine.Random.Range(0, alignments.Length)];
            var prefab = PrefabManager.Instance.GetPrefab(family + alignment);
            if (prefab == null) continue;

            var offset = UnityEngine.Random.insideUnitCircle * (float)_profile.SpawnRadius;
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
    internal static GameObject Create(Transform parent, string name, Vector3 localPosition, CrystalCreatureSpawnProfile profile)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        profile.Validate();
        var node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;
        node.AddComponent<ZNetView>();
        var spawner = node.AddComponent<DarkThroneCrystalSpawner>();
        spawner.Configure(profile);
        return node;
    }
}
