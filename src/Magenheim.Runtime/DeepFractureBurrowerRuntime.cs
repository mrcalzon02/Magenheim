using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Turns the Seeker-derived Burrower into an authored ambush chassis. The vanilla host still owns
/// terrestrial pursuit/combat after emergence; this component owns only the buried staging state.
/// </summary>
internal sealed class DeepFractureBurrowerRuntime : MonoBehaviour
{
    private const float TriggerRadius = 11f;
    private const float BuriedDepth = 1.65f;
    private const float RiseDuration = .65f;

    private Character? _character;
    private BaseAI? _ai;
    private ZNetView? _view;
    private Vector3 _surfacePosition;
    private float _rise;
    private bool _emerging;
    private bool _emerged;

    private void Awake()
    {
        _character = GetComponent<Character>() ?? throw new InvalidOperationException("Burrower lost Character authority.");
        _ai = GetComponent<BaseAI>() ?? throw new InvalidOperationException("Burrower lost BaseAI authority.");
        _view = GetComponent<ZNetView>() ?? throw new InvalidOperationException("Burrower lost ZNetView authority.");
        _surfacePosition = transform.position;
        transform.position = _surfacePosition - Vector3.up * BuriedDepth;
        SetStaged(true);
    }

    private void Update()
    {
        if (_emerged || _character is null || _character.IsDead() || _view is null || !_view.IsValid()) return;
        if (!_view.IsOwner()) return;

        if (!_emerging)
        {
            foreach (var player in Player.GetAllPlayers())
            {
                if (player is null || player.IsDead()) continue;
                if ((player.transform.position - _surfacePosition).sqrMagnitude > TriggerRadius * TriggerRadius) continue;
                _emerging = true;
                SetStaged(false);
                break;
            }
            return;
        }

        _rise = Mathf.Min(1f, _rise + Time.deltaTime / RiseDuration);
        transform.position = Vector3.Lerp(_surfacePosition - Vector3.up * BuriedDepth, _surfacePosition, Mathf.SmoothStep(0f, 1f, _rise));
        if (_rise < 1f) return;
        transform.position = _surfacePosition;
        _emerged = true;
    }

    private void SetStaged(bool staged)
    {
        if (_ai is not null) _ai.enabled = !staged;
        if (_character is not null)
        {
            _character.m_runSpeed = staged ? 0f : Mathf.Max(_character.m_runSpeed, 5.4f);
            _character.m_walkSpeed = staged ? 0f : Mathf.Max(_character.m_walkSpeed, 2.2f);
        }
        var body = GetComponent<Rigidbody>();
        if (body is not null)
        {
            body.velocity = Vector3.zero;
            body.isKinematic = staged;
        }
    }
}
