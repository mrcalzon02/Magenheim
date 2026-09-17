using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Turns the Seeker-derived Burrower into an authored ambush chassis. The vanilla host still owns
/// terrestrial pursuit/combat after emergence; this component owns buried staging and presentation.
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
    private bool _burstPlayed;

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
                PlayEmergenceBurst();
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

    private void PlayEmergenceBurst()
    {
        if (_burstPlayed) return;
        _burstPlayed = true;

        var root = new GameObject("Magenheim_BurrowerEmergence");
        root.transform.position = _surfacePosition + Vector3.up * .12f;

        for (var i = 0; i < 9; i++)
        {
            var angle = i * (360f / 9f) * Mathf.Deg2Rad;
            var shard = EffectModelAssets.Create("effect-shard");
            shard.name = "GroundShard";
            shard.transform.SetParent(root.transform, false);
            shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.05f, .08f, Mathf.Sin(angle) * 1.05f);
            shard.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-22f, 22f), -angle * Mathf.Rad2Deg, UnityEngine.Random.Range(-18f, 18f));
            shard.transform.localScale = new Vector3(.18f, UnityEngine.Random.Range(.35f, .72f), .28f);
            var collider = shard.GetComponent<Collider>();
            if (collider is not null) Destroy(collider);
            var body = shard.AddComponent<Rigidbody>();
            body.mass = .12f;
            body.velocity = new Vector3(Mathf.Cos(angle) * 2.8f, UnityEngine.Random.Range(3.2f, 5.1f), Mathf.Sin(angle) * 2.8f);
            body.angularVelocity = UnityEngine.Random.insideUnitSphere * 7f;
            Destroy(shard, 1.25f);
        }

        var disturbance = EffectModelAssets.Create("effect-ring");
        disturbance.name = "GroundDisturbance";
        disturbance.transform.SetParent(root.transform, false);
        disturbance.transform.localPosition = new Vector3(0f, .025f, 0f);
        disturbance.transform.localScale = new Vector3(1.75f, .025f, 1.75f);
        var disturbanceCollider = disturbance.GetComponent<Collider>();
        if (disturbanceCollider is not null) Destroy(disturbanceCollider);
        Destroy(disturbance, .45f);
        Destroy(root, 1.4f);
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
