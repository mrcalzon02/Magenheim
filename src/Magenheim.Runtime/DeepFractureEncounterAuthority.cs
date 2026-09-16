using System;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class DeepFractureEncounterAuthority : MonoBehaviour
{
    private const string ClearedKeyPrefix = "magenheim.fracture.cleared.";
    private ZNetView _view = null!;

    private void Awake() => _view = GetComponent<ZNetView>();

    internal bool HasAuthority =>
        _view is not null && _view.IsValid() && _view.IsOwner() &&
        ZNet.instance is not null && ZNet.instance.IsServer();

    internal bool IsCleared(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentException("Encounter unit identity is required.", nameof(identity));
        return _view is not null && _view.IsValid() && _view.GetZDO().GetBool(ClearedKeyPrefix + identity, false);
    }

    internal void MarkCleared(string identity)
    {
        if (!HasAuthority) return;
        if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentException("Encounter unit identity is required.", nameof(identity));
        _view.GetZDO().Set(ClearedKeyPrefix + identity, true);
    }
}

internal sealed class DeepFractureEncounterDeathTracker : MonoBehaviour
{
    private Character _character = null!;
    private DeepFractureEncounterAuthority _authority = null!;
    private string _identity = string.Empty;
    private bool _recorded;

    internal void Bind(Character character, DeepFractureEncounterAuthority authority, string identity)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _identity = string.IsNullOrWhiteSpace(identity) ? throw new ArgumentException("Encounter unit identity is required.", nameof(identity)) : identity;
    }

    private void Update()
    {
        if (_recorded || _character is null || !_character.IsDead()) return;
        _authority.MarkCleared(_identity);
        if (_authority.HasAuthority) _recorded = true;
    }
}
