using System;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Persistent server-owned controller for the unique Dark Throne encounter. The controller's ZDO is
/// the durable authority; the King is a reconstructable runtime entity and therefore cannot be lost
/// permanently to distance cleanup, zone unload, or restart while the encounter remains undefeated.
/// </summary>
internal sealed class DarkThroneEncounterRuntime : MonoBehaviour
{
    private const string DefeatedKey = "magenheim.darkthrone.defeated";
    private const string EngagedKey = "magenheim.darkthrone.engaged";
    private const string KingIdentityKey = "magenheim.darkthrone.king.id";
    private const string KingEncounterKey = "magenheim.darkthrone.king.encounter";
    private const float DisengageGraceSeconds = 12f;

    private ZNetView _view;
    private Transform _kingAnchor;
    private float _noParticipantsSince = -1f;

    private void Awake() => _view = GetComponent<ZNetView>();

    private void Start()
    {
        _kingAnchor = transform.parent != null
            ? transform.parent.Find(DarkThroneVisuals.KingAnchorName)
            : null;
        if (_kingAnchor == null)
        {
            Debug.LogError("Dark Throne encounter has no Nowhere King anchor and will remain inert.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!HasAuthority()) return;
        var zdo = _view.GetZDO();
        if (zdo.GetBool(DefeatedKey, false))
        {
            SuspendEcology(false);
            return;
        }

        var king = FindOwnedKing();
        if (king == null)
        {
            king = SpawnKing();
            if (king == null) return;
        }

        var participants = CountLivingParticipants();
        var engaged = zdo.GetBool(EngagedKey, false);
        if (participants > 0)
        {
            _noParticipantsSince = -1f;
            if (!engaged)
            {
                zdo.Set(EngagedKey, true);
                SuspendEcology(true);
            }
            return;
        }

        if (!engaged) return;
        if (_noParticipantsSince < 0f) _noParticipantsSince = Time.time;
        if (Time.time - _noParticipantsSince < DisengageGraceSeconds) return;
        ResetEncounter(king);
    }

    private bool HasAuthority() => _view != null && _view.IsValid() && _view.IsOwner() && ZNet.instance != null;

    private int CountLivingParticipants()
    {
        var center = transform.position;
        var count = 0;
        foreach (var player in Player.GetAllPlayers())
        {
            if (player == null || player.IsDead()) continue;
            var delta = player.transform.position - center;
            if (Mathf.Abs(delta.x) <= 34f && Mathf.Abs(delta.z) <= 38f) count++;
        }
        return count;
    }

    private Character FindOwnedKing()
    {
        var encounterId = EnsureEncounterIdentity();
        foreach (var character in Character.GetAllCharacters())
        {
            if (character == null || character.IsDead()) continue;
            var nview = character.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) continue;
            if (string.Equals(nview.GetZDO().GetString(KingEncounterKey, string.Empty), encounterId, StringComparison.Ordinal))
                return character;
        }
        return null;
    }

    private Character SpawnKing()
    {
        var prefab = PrefabManager.Instance.GetPrefab(NowhereKingRegistrar.PrefabName);
        if (prefab == null) return null;
        var instance = Instantiate(prefab, _kingAnchor.position, _kingAnchor.rotation);
        var character = instance.GetComponent<Character>();
        var nview = instance.GetComponent<ZNetView>();
        if (character == null || nview == null || !nview.IsValid())
        {
            Destroy(instance);
            return null;
        }

        var encounterId = EnsureEncounterIdentity();
        nview.GetZDO().Set(KingEncounterKey, encounterId);
        _view.GetZDO().Set(KingIdentityKey, nview.GetZDO().m_uid.ToString());
        var leash = instance.GetComponent<NowhereKingArenaLeash>();
        if (leash != null) leash.Configure(transform.position);
        return character;
    }

    private string EnsureEncounterIdentity()
    {
        var zdo = _view.GetZDO();
        var id = zdo.GetString("magenheim.darkthrone.id", string.Empty);
        if (!string.IsNullOrWhiteSpace(id)) return id;
        var p = transform.position;
        id = string.Concat("darkthrone:", Mathf.RoundToInt(p.x), ":", Mathf.RoundToInt(p.y), ":", Mathf.RoundToInt(p.z));
        zdo.Set("magenheim.darkthrone.id", id);
        return id;
    }

    private void ResetEncounter(Character king)
    {
        var zdo = _view.GetZDO();
        zdo.Set(EngagedKey, false);
        _noParticipantsSince = -1f;
        SuspendEcology(false);
        if (king == null) return;
        king.Heal(king.GetMaxHealth(), true);
        king.SetPos(_kingAnchor.position);
        var body = king.GetComponent<Rigidbody>();
        if (body != null) { body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
    }

    private void SuspendEcology(bool suspended)
    {
        var root = transform.parent != null ? transform.parent : transform;
        foreach (var spawner in root.GetComponentsInChildren<DarkThroneCrystalSpawner>(true))
            spawner.SetEncounterSuspended(suspended);
    }
}
