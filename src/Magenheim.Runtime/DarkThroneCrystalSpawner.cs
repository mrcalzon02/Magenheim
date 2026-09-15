using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Server-owned crystal ecology node for the Dark Throne. The node chooses from existing
/// Magenheim creature prefabs and never creates a parallel creature definition authority.
/// Encounter runtime may suspend a node while the King is engaged.
/// </summary>
internal sealed class DarkThroneCrystalSpawner : MonoBehaviour
{
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

    private readonly List<GameObject> _living = new List<GameObject>();
    private ZNetView _view;
    private bool _guardian;
    private bool _suspended;
    private int _maxAlive;
    private float _radius;
    private float _respawnSeconds;
    private float _nextSpawn;

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
        if (_suspended == suspended) return;
        _suspended = suspended;
        if (!suspended) _nextSpawn = Time.time + 3f;
    }

    private void Awake()
    {
        _view = GetComponent<ZNetView>();
        if (_maxAlive == 0) Configure(false, 2, 8f, 75f);
        _nextSpawn = Time.time + UnityEngine.Random.Range(4f, 10f);
    }

    private void Update()
    {
        if (_suspended || Time.time < _nextSpawn || !HasAuthority()) return;
        _living.RemoveAll(go => go == null);
        if (_living.Count >= _maxAlive) return;
        SpawnOne();
        _nextSpawn = Time.time + _respawnSeconds;
    }

    private bool HasAuthority()
    {
        if (_view == null || !_view.IsValid()) return false;
        return _view.IsOwner();
    }

    private void SpawnOne()
    {
        var families = _guardian ? GuardianFamilies : LesserFamilies;
        var alignments = (ElementalAlignment[])Enum.GetValues(typeof(ElementalAlignment));
        if (families.Length == 0 || alignments.Length == 0) return;

        // Retry several combinations because a compatibility profile may deliberately omit a family.
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
            _living.Add(spawned);
            return;
        }
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
