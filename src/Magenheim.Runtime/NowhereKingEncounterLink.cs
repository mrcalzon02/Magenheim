using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Links the reconstructable King instance back to its durable throne authority.</summary>
internal sealed class NowhereKingEncounterLink : MonoBehaviour
{
    private DarkThroneEncounterRuntime _encounter;
    private Character _character;
    private ZNetView _view;
    private bool _reported;

    internal void Bind(DarkThroneEncounterRuntime encounter)
    {
        _encounter = encounter;
        _character = GetComponent<Character>();
        _view = GetComponent<ZNetView>();
    }

    private void Update()
    {
        if (_reported || _encounter == null || _character == null || _view == null || !_view.IsValid() || !_view.IsOwner()) return;
        if (!_character.IsDead()) return;
        _reported = true;
        _encounter.MarkDefeated(_view.GetZDO());
    }
}
