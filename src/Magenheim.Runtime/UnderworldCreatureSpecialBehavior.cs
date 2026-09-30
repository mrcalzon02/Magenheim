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
        AbyssShellArmor,
        VentSpitterRetaliation,
        RootedCaster,
        RootedSpawner,
    }

    [SerializeField] private Mode _mode;
    [SerializeField] private float _nextAbilityTime;

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
            Mode.AbyssShellArmor => "shell armor; pickaxe bypass",
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
            case Mode.VentSpitterRetaliation:
                TryThermalSpit(owner, hit.GetAttacker());
                break;
        }
    }

    private void Update()
    {
        if (_mode != Mode.RootedSpawner || Time.time < _nextAbilityTime)
            return;

        var owner = GetComponent<Character>();
        if (!owner || owner.IsDead() || !HasAuthority(owner) ||
            ZNet.instance is null || !ZNet.instance.IsServer())
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
            case "Abyss Shellback":
                return Mode.AbyssShellArmor;
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
