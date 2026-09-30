using System;
using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Species-defining mechanics layered onto selected donor-backed Underworld creatures. These
/// mechanics deliberately reuse the proven donor rig/animation/network chassis while making the
/// creature play differently from the donor.
/// </summary>
internal sealed class UnderworldCreatureSpecialBehavior : MonoBehaviour
{
    private enum Mode
    {
        None,
        PuffbackSporeBurst,
        CaveRayFlee,
        LampreyLatch,
        LanternAnglerPressure,
        AbyssShellArmor,
        DeepHunterRam,
        VentSpitterRetaliation,
        RootedCaster,
        RootedSpawner,
    }

    [SerializeField] private Mode _mode;
    [SerializeField] private float _nextAbilityTime;
    private Player? _latchedTarget;
    private BaseAI? _latchedAi;
    private float _latchUntil;
    private float _nextLatchTick;

    internal static string Attach(GameObject prefab, UnderworldCreaturePrototypes.Entry entry)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        if (entry is null) throw new ArgumentNullException(nameof(entry));

        var mode = ModeFor(entry.Name);
        if (mode == Mode.None)
            return "no species-specific ability";

        var component = prefab.GetComponent<UnderworldCreatureSpecialBehavior>();
        if (!component)
            component = prefab.AddComponent<UnderworldCreatureSpecialBehavior>();
        component._mode = mode;

        if (mode == Mode.RootedCaster || mode == Mode.RootedSpawner)
            Root(prefab);

        return mode switch
        {
            Mode.PuffbackSporeBurst => "reactive spore burst",
            Mode.CaveRayFlee => "hit-triggered aquatic flee impulse",
            Mode.LampreyLatch => "timed attach-and-feed latch",
            Mode.LanternAnglerPressure => "radial pressure-release attack",
            Mode.AbyssShellArmor => "shell armor; pickaxe bypass",
            Mode.DeepHunterRam => "apex ram charge",
            Mode.VentSpitterRetaliation => "line-of-sight thermal spit retaliation",
            Mode.RootedCaster => "rooted caster behavior",
            Mode.RootedSpawner => "rooted colony with Rotling propagation",
            _ => "no species-specific ability",
        };
    }

    internal void ModifyIncoming(HitData hit)
    {
        if (hit is null || _mode != Mode.AbyssShellArmor)
            return;

        // Shell armor strongly resists ordinary weapon impact but deliberately does not reduce
        // pickaxe damage, giving the player a readable tool-counter instead of flat durability.
        hit.m_damage.m_damage *= .62f;
        hit.m_damage.m_blunt *= .58f;
        hit.m_damage.m_slash *= .52f;
        hit.m_damage.m_pierce *= .55f;
        hit.m_damage.m_chop *= .70f;
        hit.m_staggerMultiplier *= .68f;
    }

    internal void AfterDamage(Character owner, HitData hit)
    {
        if (!owner || hit is null || owner.IsDead() || !HasAuthority(owner))
            return;

        switch (_mode)
        {
            case Mode.PuffbackSporeBurst:
                TrySporeBurst(owner);
                break;
            case Mode.CaveRayFlee:
                TryFlee(owner, hit.GetAttacker());
                break;
            case Mode.VentSpitterRetaliation:
                TryThermalSpit(owner, hit.GetAttacker());
                break;
        }
    }

    private void Update()
    {
        var owner = GetComponent<Character>();
        if (!owner || owner.IsDead() || !HasAuthority(owner))
            return;

        switch (_mode)
        {
            case Mode.RootedSpawner:
                UpdateRootedSpawner(owner);
                break;
            case Mode.LampreyLatch:
                UpdateLampreyLatch(owner);
                break;
            case Mode.LanternAnglerPressure:
                UpdateLanternAngler(owner);
                break;
            case Mode.DeepHunterRam:
                UpdateDeepHunter(owner);
                break;
        }
    }

    private void UpdateRootedSpawner(Character owner)
    {
        if (Time.time < _nextAbilityTime ||
            ZNet.instance is null ||
            !ZNet.instance.IsServer())
            return;

        if (!HasLivingPlayerNearby(owner.transform.position, owner.gameObject.scene.handle, 15f))
        {
            _nextAbilityTime = Time.time + 2f;
            return;
        }

        if (CountNearbyRotlings(owner.transform.position, owner.gameObject.scene.handle, 13f) >= 3)
        {
            _nextAbilityTime = Time.time + 5f;
            return;
        }

        var prefab = PrefabManager.Instance.GetPrefab("Magenheim_Underworld_Prototype_Rotling");
        if (!prefab)
        {
            _nextAbilityTime = Time.time + 8f;
            return;
        }

        var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        var radius = UnityEngine.Random.Range(1.8f, 3.2f);
        var position = owner.transform.position +
                       new Vector3(Mathf.Cos(angle) * radius, .35f, Mathf.Sin(angle) * radius);

        var instance = Instantiate(
            prefab,
            position,
            Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
        var view = instance.GetComponent<ZNetView>();
        if (!view || !view.IsValid())
        {
            UnityEngine.Object.Destroy(instance);
            _nextAbilityTime = Time.time + 6f;
            return;
        }

        instance.SetActive(true);
        _nextAbilityTime = Time.time + 18f;
    }

    private void UpdateLampreyLatch(Character owner)
    {
        if (_latchedTarget)
        {
            if (_latchedTarget.IsDead() ||
                _latchedTarget.gameObject.scene.handle != owner.gameObject.scene.handle ||
                Time.time >= _latchUntil)
            {
                ReleaseLamprey(owner);
                return;
            }

            var target = _latchedTarget;
            var body = owner.GetComponent<Rigidbody>();
            if (body)
                body.linearVelocity = Vector3.zero;

            owner.transform.position =
                target.GetCenterPoint() -
                target.transform.forward * .38f +
                target.transform.right * .22f;
            owner.transform.rotation = Quaternion.LookRotation(
                target.transform.forward,
                Vector3.up);

            if (Time.time >= _nextLatchTick)
            {
                _nextLatchTick = Time.time + .72f;
                var hit = new HitData
                {
                    m_point = target.GetCenterPoint(),
                    m_dir = target.transform.forward,
                    m_damage = new HitData.DamageTypes
                    {
                        m_pierce = 4f,
                        m_poison = 3f,
                    },
                    m_staggerMultiplier = .15f,
                };
                hit.SetAttacker(owner);
                target.Damage(hit);
            }
            return;
        }

        if (Time.time < _nextAbilityTime)
            return;

        var candidate = NearestLivingPlayer(
            owner.transform.position,
            owner.gameObject.scene.handle,
            1.9f);
        if (!candidate)
            return;

        _latchedTarget = candidate;
        _latchUntil = Time.time + 3.2f;
        _nextLatchTick = Time.time;
        _latchedAi = owner.GetComponent<BaseAI>();
        if (_latchedAi)
            _latchedAi.enabled = false;

        owner.GetComponentInChildren<RigidCreaturePresentationDriver>(true)?.PlayOneShot("latch");
    }

    private void ReleaseLamprey(Character owner)
    {
        var previous = _latchedTarget;
        _latchedTarget = null;
        if (_latchedAi)
            _latchedAi.enabled = true;
        _latchedAi = null;
        _nextAbilityTime = Time.time + 6.5f;

        var body = owner.GetComponent<Rigidbody>();
        if (body)
        {
            var away = previous
                ? owner.transform.position - previous.transform.position
                : -owner.transform.forward;
            if (away.sqrMagnitude < .001f)
                away = -owner.transform.forward;
            body.AddForce(away.normalized * 4.5f, ForceMode.VelocityChange);
        }

        owner.GetComponentInChildren<RigidCreaturePresentationDriver>(true)?.PlayOneShot("detach");
    }

    private void UpdateLanternAngler(Character owner)
    {
        if (Time.time < _nextAbilityTime)
            return;

        var target = NearestLivingPlayer(
            owner.transform.position,
            owner.gameObject.scene.handle,
            12f);
        if (!target)
            return;

        var distance = Vector3.Distance(owner.transform.position, target.transform.position);
        if (distance < 4.5f || distance > 12f)
            return;

        _nextAbilityTime = Time.time + 8.5f;
        owner.GetComponentInChildren<RigidCreaturePresentationDriver>(true)?.PlayOneShot("pressurerelease");

        foreach (var player in Player.GetAllPlayers())
        {
            if (!player ||
                player.IsDead() ||
                player.gameObject.scene.handle != owner.gameObject.scene.handle)
                continue;

            var offset = player.transform.position - owner.transform.position;
            var distanceToPlayer = offset.magnitude;
            if (distanceToPlayer > 8f || distanceToPlayer < .01f)
                continue;

            var direction = offset / distanceToPlayer;
            var hit = new HitData
            {
                m_point = player.GetCenterPoint(),
                m_dir = direction,
                m_damage = new HitData.DamageTypes
                {
                    m_blunt = 12f,
                },
                m_pushForce = 14f,
                m_staggerMultiplier = .85f,
            };
            hit.SetAttacker(owner);
            player.Damage(hit);

            var body = player.GetComponent<Rigidbody>();
            if (body)
                body.AddForce(direction * 6.5f, ForceMode.VelocityChange);
        }

        EmitBurst(owner.transform, new Color(.22f, .72f, .86f, .52f), 30, 6.2f);
    }

    private void UpdateDeepHunter(Character owner)
    {
        if (Time.time < _nextAbilityTime)
            return;

        var target = NearestLivingPlayer(
            owner.transform.position,
            owner.gameObject.scene.handle,
            20f);
        if (!target)
            return;

        var delta = target.GetCenterPoint() - owner.GetCenterPoint();
        var distance = delta.magnitude;
        if (distance < 7f || distance > 20f)
            return;

        if (Physics.Linecast(
                owner.GetCenterPoint(),
                target.GetCenterPoint(),
                out var obstruction,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            var hitCharacter = obstruction.collider
                ? obstruction.collider.GetComponentInParent<Character>()
                : null;
            if (!ReferenceEquals(hitCharacter, target))
                return;
        }

        var body = owner.GetComponent<Rigidbody>();
        if (!body)
            return;

        _nextAbilityTime = Time.time + 7.8f;
        var direction = delta.normalized;
        body.AddForce(direction * 10.5f, ForceMode.VelocityChange);
        owner.GetComponentInChildren<RigidCreaturePresentationDriver>(true)?.PlayOneShot("ram");
    }

    private void TryFlee(Character owner, Character? attacker)
    {
        if (Time.time < _nextAbilityTime || attacker is not Player || attacker.IsDead())
            return;

        var body = owner.GetComponent<Rigidbody>();
        if (!body)
            return;

        var away = owner.transform.position - attacker.transform.position;
        if (away.sqrMagnitude < .001f)
            away = -owner.transform.forward;
        away.Normalize();

        body.AddForce((away * 7.5f) + Vector3.up * 1.2f, ForceMode.VelocityChange);
        _nextAbilityTime = Time.time + 3.5f;
        owner.GetComponentInChildren<RigidCreaturePresentationDriver>(true)?.PlayOneShot("flee");
    }

    private static Player? NearestLivingPlayer(
        Vector3 origin,
        int sceneHandle,
        float maximumDistance)
    {
        Player? best = null;
        var bestSquared = maximumDistance * maximumDistance;

        foreach (var player in Player.GetAllPlayers())
        {
            if (!player ||
                player.IsDead() ||
                player.gameObject.scene.handle != sceneHandle)
                continue;

            var squared = (player.transform.position - origin).sqrMagnitude;
            if (squared > bestSquared)
                continue;

            best = player;
            bestSquared = squared;
        }

        return best;
    }

    private void TrySporeBurst(Character owner)
    {
        if (Time.time < _nextAbilityTime)
            return;

        _nextAbilityTime = Time.time + 7.5f;
        var origin = owner.transform.position;
        var colliders = Physics.OverlapSphere(
            origin,
            5.2f,
            LayerMask.GetMask("character"),
            QueryTriggerInteraction.Collide);
        var struck = new HashSet<Character>();

        foreach (var collider in colliders)
        {
            var target = collider.GetComponentInParent<Character>();
            if (!target || target.IsDead() || ReferenceEquals(target, owner) || !struck.Add(target))
                continue;
            if (target is not Player)
                continue;

            var direction = target.transform.position - origin;
            direction.y = Mathf.Max(.12f, direction.y);
            if (direction.sqrMagnitude < .001f)
                direction = Vector3.up;
            direction.Normalize();

            var burst = new HitData
            {
                m_damage = new HitData.DamageTypes
                {
                    m_blunt = 3f,
                    m_poison = 14f,
                },
                m_point = target.transform.position,
                m_dir = direction,
                m_pushForce = 7f,
                m_staggerMultiplier = 1.15f,
            };
            burst.SetAttacker(owner);
            target.Damage(burst);
        }

        EmitBurst(owner.transform, new Color(.48f, .92f, .48f, .62f), 26, 4.4f);
        owner.GetComponentInChildren<RigidCreaturePresentationDriver>(true)?.PlayOneShot("sporepuff");
    }

    private void TryThermalSpit(Character owner, Character? attacker)
    {
        if (Time.time < _nextAbilityTime || attacker is not Player || attacker.IsDead())
            return;

        var start = owner.transform.position + Vector3.up * .75f;
        var end = attacker.transform.position + Vector3.up * .85f;
        var delta = end - start;
        var distance = delta.magnitude;
        if (distance < 4f || distance > 22f)
            return;

        if (Physics.Linecast(
                start,
                end,
                out var obstruction,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            var hitCharacter = obstruction.collider
                ? obstruction.collider.GetComponentInParent<Character>()
                : null;
            if (!ReferenceEquals(hitCharacter, attacker))
                return;
        }

        _nextAbilityTime = Time.time + 5.5f;
        var retaliation = new HitData
        {
            m_damage = new HitData.DamageTypes
            {
                m_fire = 12f,
                m_poison = 4f,
            },
            m_point = attacker.transform.position,
            m_dir = delta.normalized,
            m_pushForce = 4f,
        };
        retaliation.SetAttacker(owner);
        attacker.Damage(retaliation);
        EmitBurst(owner.transform, new Color(1f, .42f, .08f, .68f), 18, 2.8f);
    }

    private static void Root(GameObject prefab)
    {
        var character = prefab.GetComponent<Character>()
            ?? throw new InvalidOperationException(
                "Rooted Underworld species lost Character authority.");

        character.m_walkSpeed = 0f;
        character.m_runSpeed = 0f;
        character.m_swimSpeed = 0f;
        character.m_flySlowSpeed = 0f;
        character.m_flyFastSpeed = 0f;
        character.m_acceleration = Mathf.Min(character.m_acceleration, .1f);
        character.m_turnSpeed *= .65f;

        var ai = prefab.GetComponent<MonsterAI>();
        if (ai)
        {
            ai.m_randomMoveInterval = 9999f;
            ai.m_randomMoveRange = 0f;
            ai.m_circleTargetInterval = 9999f;
            ai.m_circleTargetDuration = 0f;
        }
    }

    private static bool HasAuthority(Character owner)
    {
        var view = owner.GetComponent<ZNetView>();
        return view && view.IsValid() && view.IsOwner() && ZNet.instance is not null;
    }

    private static bool HasLivingPlayerNearby(Vector3 origin, int sceneHandle, float radius)
    {
        var radiusSquared = radius * radius;
        foreach (var player in Player.GetAllPlayers())
        {
            if (!player || player.IsDead() || player.gameObject.scene.handle != sceneHandle)
                continue;
            if ((player.transform.position - origin).sqrMagnitude <= radiusSquared)
                return true;
        }
        return false;
    }

    private static int CountNearbyRotlings(Vector3 origin, int sceneHandle, float radius)
    {
        var radiusSquared = radius * radius;
        var count = 0;
        foreach (var character in Character.GetAllCharacters())
        {
            if (!character ||
                character.IsDead() ||
                character.gameObject.scene.handle != sceneHandle ||
                !character.gameObject.name.StartsWith(
                    "Magenheim_Underworld_Prototype_Rotling",
                    StringComparison.Ordinal))
                continue;

            if ((character.transform.position - origin).sqrMagnitude <= radiusSquared)
                count++;
        }
        return count;
    }

    private static void EmitBurst(
        Transform owner,
        Color color,
        int count,
        float radius)
    {
        var root = new GameObject("Magenheim_Underworld_SpeciesBurst");
        root.transform.position = owner.position + Vector3.up * .7f;

        var particles = root.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 1.2f;
        main.startSpeed = 2.4f;
        main.startSize = .12f;
        main.startColor = color;
        main.maxParticles = count;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(.2f, radius * .22f);

        particles.Emit(count);
        UnityEngine.Object.Destroy(root, 1.8f);
    }

    private static Mode ModeFor(string name)
    {
        switch (name)
        {
            case "Puffback":
                return Mode.PuffbackSporeBurst;
            case "Cave Ray":
                return Mode.CaveRayFlee;
            case "Blackwater Lamprey":
                return Mode.LampreyLatch;
            case "Lantern Angler":
                return Mode.LanternAnglerPressure;
            case "Abyss Shellback":
                return Mode.AbyssShellArmor;
            case "Deep Hunter":
                return Mode.DeepHunterRam;
            case "Vent Spitter":
                return Mode.VentSpitterRetaliation;
            case "Carrion Bloom":
                return Mode.RootedCaster;
            case "Corpse Orchard":
                return Mode.RootedSpawner;
            default:
                return Mode.None;
        }
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class UnderworldCreatureSpecialBehaviorPatch
{
    private static void Prefix(Character __instance, HitData hit) =>
        __instance?.GetComponent<UnderworldCreatureSpecialBehavior>()?.ModifyIncoming(hit);

    private static void Postfix(Character __instance, HitData hit) =>
        __instance?.GetComponent<UnderworldCreatureSpecialBehavior>()?.AfterDamage(__instance, hit);
}
