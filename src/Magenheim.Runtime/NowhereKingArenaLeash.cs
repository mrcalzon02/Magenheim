using Magenheim.Core.DarkThrone;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Server-authoritative hard leash. The encounter controller supplies the arena center after spawning;
/// every physics/pathfinding escape is corrected from the same core arena geometry.
/// </summary>
internal sealed class NowhereKingArenaLeash : MonoBehaviour
{
    private ZNetView _view = null!;
    private Character _character = null!;
    private DarkThroneArena _arena = null!;
    private bool _configured;

    internal void Configure(Vector3 center)
    {
        _arena = DarkThroneArena.CreateDefault(new EncounterPosition(center.x, center.y, center.z));
        _arena.Validate();
        _configured = true;
    }

    private void Awake()
    {
        _view = GetComponent<ZNetView>();
        _character = GetComponent<Character>();
    }

    private void FixedUpdate()
    {
        if (!_configured || _view == null || !_view.IsValid() || !_view.IsOwner()) return;
        var p = transform.position;
        var core = new EncounterPosition(p.x, p.y, p.z);
        if (_arena.Contains(core)) return;
        var recovery = _arena.RecoveryPoint(core);
        var target = new Vector3(recovery.X, recovery.Y, recovery.Z);
        if (_character != null) _character.transform.position=target;
        else transform.position = target;
        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
