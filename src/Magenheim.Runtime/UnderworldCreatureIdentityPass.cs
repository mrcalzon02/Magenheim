using System;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Turns reused vanilla creature chassis into biome-native Underworld species without replacing
/// their proven animation, attacks, networking or persistence.
/// </summary>
internal static class UnderworldCreatureIdentityPass
{
    internal static string Apply(GameObject prefab, UnderworldCreaturePrototypes.Entry entry)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        var character = prefab.GetComponent<Character>()
            ?? throw new InvalidOperationException($"{entry.Name} lost Character authority before identity tuning.");

        ApplyEnvironmentalPhysiology(character, entry);
        var combat = UnderworldCreatureCombatBalance.Apply(prefab, entry);
        var aggression = ApplyTemperament(prefab, entry);
        var elemental = AttachElementalAttack(prefab, entry);
        var effects = AddPresentation(prefab, entry);

        return $"{combat}, aggression x{aggression:0.00}, {elemental}, {effects}";
    }

    internal static bool HuntsPlayer(UnderworldCreaturePrototypes.Entry entry)
    {
        switch (entry.Name)
        {
            case "Crowncap Brute":
            case "Deep Hunter":
            case "Cinder Hound":
            case "Glacier Stalker":
            case "Chasm Stalker":
            case "Rift Colossus":
            case "Decay Hound":
            case "Graft Warden":
                return true;
            default:
                return false;
        }
    }

    private static void ApplyEnvironmentalPhysiology(Character character, UnderworldCreaturePrototypes.Entry entry)
    {
        // Preserve donor physical resistances and replace only the environmental/elemental channels
        // that define the new species' home adaptation.
        switch (entry.Biome)
        {
            case "Fungal Forest":
                character.m_damageModifiers.m_poison = HitData.DamageModifier.Resistant;
                character.m_damageModifiers.m_fire = HitData.DamageModifier.Weak;
                character.m_tolerateWater = true;
                character.m_tolerateTar = true;
                break;

            case "Blackwater Deep":
                character.m_damageModifiers.m_frost = HitData.DamageModifier.Resistant;
                character.m_damageModifiers.m_lightning = HitData.DamageModifier.Weak;
                character.m_damageModifiers.m_fire = HitData.DamageModifier.SlightlyWeak;
                character.m_tolerateWater = true;
                break;

            case "Sulfurous Wastes":
                character.m_damageModifiers.m_fire = HitData.DamageModifier.VeryResistant;
                character.m_damageModifiers.m_frost = HitData.DamageModifier.VeryWeak;
                character.m_damageModifiers.m_poison = HitData.DamageModifier.SlightlyResistant;
                character.m_tolerateFire = true;
                character.m_tolerateSmoke = true;
                break;

            case "Frozen Caverns":
                character.m_damageModifiers.m_frost = HitData.DamageModifier.VeryResistant;
                character.m_damageModifiers.m_fire = HitData.DamageModifier.VeryWeak;
                character.m_tolerateWater = true;
                break;

            case "Fracture Zones":
                character.m_damageModifiers.m_lightning = HitData.DamageModifier.Resistant;
                character.m_damageModifiers.m_spirit = HitData.DamageModifier.SlightlyResistant;
                character.m_tolerateSmoke = true;
                break;

            case "Great Decay":
                character.m_damageModifiers.m_poison = HitData.DamageModifier.VeryResistant;
                character.m_damageModifiers.m_spirit = HitData.DamageModifier.Weak;
                character.m_damageModifiers.m_fire = HitData.DamageModifier.Weak;
                character.m_tolerateWater = true;
                character.m_tolerateSmoke = true;
                character.m_tolerateTar = true;
                break;
        }

        switch (entry.Name)
        {
            case "Furnace Golem":
                character.m_damageModifiers.m_fire = HitData.DamageModifier.Immune;
                break;
            case "Fume Wraith":
                character.m_damageModifiers.m_poison = HitData.DamageModifier.Immune;
                character.m_tolerateSmoke = true;
                break;
            case "Cryolith Guardian":
                character.m_damageModifiers.m_frost = HitData.DamageModifier.Immune;
                break;
            case "Fracture Wisp":
                character.m_damageModifiers.m_lightning = HitData.DamageModifier.VeryResistant;
                break;
            case "Corpse Orchard":
            case "Spore Husk":
                character.m_damageModifiers.m_poison = HitData.DamageModifier.Immune;
                break;
        }

        if (entry.Donor == "Serpent" || entry.Donor == "Leech" || entry.Donor == "Neck")
            character.m_tolerateWater = true;
    }

    private static float ApplyTemperament(GameObject prefab, UnderworldCreaturePrototypes.Entry entry)
    {
        var aggression = AggressionFor(entry);
        var ai = prefab.GetComponent<MonsterAI>();
        if (ai)
        {
            ai.m_alertRange = Mathf.Clamp(Mathf.Max(ai.m_alertRange * aggression, 11f * aggression), 8f, 42f);
            ai.m_viewRange = Mathf.Clamp(Mathf.Max(ai.m_viewRange * aggression, 16f * aggression), 12f, 48f);
            ai.m_viewAngle = Mathf.Clamp(Mathf.Max(ai.m_viewAngle, 95f + (aggression - 1f) * 70f), 70f, 170f);
            ai.m_hearRange = Mathf.Clamp(Mathf.Max(ai.m_hearRange * aggression, 10f * aggression), 8f, 36f);

            if (ai.m_circleTargetInterval > 0f)
                ai.m_circleTargetInterval = Mathf.Clamp(ai.m_circleTargetInterval / aggression, .75f, 7f);
            if (ai.m_circleTargetDuration > 0f)
                ai.m_circleTargetDuration = Mathf.Clamp(ai.m_circleTargetDuration / Mathf.Sqrt(aggression), .75f, 6f);
            if (ai.m_randomMoveInterval > 0f && aggression > 1.05f)
                ai.m_randomMoveInterval = Mathf.Clamp(ai.m_randomMoveInterval / Mathf.Lerp(1f, aggression, .55f), 1.2f, 12f);
        }

        // Perception may become more aggressive, but movement speed is owned by the combat profile.
        // This prevents the old failure mode where harder-hitting creatures also became faster.
        return aggression;
    }

    private static float AggressionFor(UnderworldCreaturePrototypes.Entry entry)
    {
        switch (entry.Name)
        {
            case "Deep Hunter":
            case "Cinder Hound":
            case "Glacier Stalker":
            case "Chasm Stalker":
            case "Decay Hound":
            case "Crowncap Brute":
            case "Rift Colossus":
            case "Graft Warden":
                return 1.55f;

            case "Mycelial Stalker":
            case "Blackwater Lamprey":
            case "Lantern Angler":
            case "Basalt Crawler":
            case "Vent Spitter":
            case "Fume Wraith":
            case "Magma Leaper":
            case "Iceblind":
            case "Pale Burrower":
            case "Rimewing":
            case "Rift Skitter":
            case "Shardwing":
            case "Gravity Leech":
            case "Spore Husk":
            case "Marrow Creeper":
                return 1.35f;

            case "Lantern Moth":
            case "Puffback":
            case "Cave Ray":
            case "Rime Moth":
            case "Fracture Wisp":
                return 1.05f;

            default:
                return 1.20f;
        }
    }

    private static bool IsMovementConstrainedDonor(string donor) =>
        donor == "Serpent" || donor == "Leech" || donor == "Bat" || donor == "Hatchling" ||
        donor == "Wraith" || donor == "FrostWisp" || donor == "StoneGolem";

    private static string AttachElementalAttack(GameObject prefab, UnderworldCreaturePrototypes.Entry entry)
    {
        var component = prefab.GetComponent<UnderworldCreatureElementalAttack>();
        if (!component) component = prefab.AddComponent<UnderworldCreatureElementalAttack>();

        var element = UnderworldCreatureElementalAttack.Element.None;
        var fraction = .08f;
        switch (entry.Biome)
        {
            case "Fungal Forest":
                element = UnderworldCreatureElementalAttack.Element.Poison;
                fraction = .09f;
                break;
            case "Blackwater Deep":
                element = UnderworldCreatureElementalAttack.Element.Frost;
                fraction = .08f;
                break;
            case "Sulfurous Wastes":
                element = UnderworldCreatureElementalAttack.Element.Fire;
                fraction = .14f;
                break;
            case "Frozen Caverns":
                element = UnderworldCreatureElementalAttack.Element.Frost;
                fraction = .14f;
                break;
            case "Fracture Zones":
                element = UnderworldCreatureElementalAttack.Element.Lightning;
                fraction = .11f;
                break;
            case "Great Decay":
                element = UnderworldCreatureElementalAttack.Element.Poison;
                fraction = .13f;
                break;
        }

        if (AggressionFor(entry) >= 1.5f) fraction *= 1.20f;
        component.Configure(element, Mathf.Clamp(fraction, .05f, .18f));
        return $"{element} attack rider {fraction:P0}";
    }

    private static string AddPresentation(GameObject prefab, UnderworldCreaturePrototypes.Entry entry)
    {
        var profile = FxFor(entry);
        var height = VisualHeight(entry.Donor);

        var particlesRoot = new GameObject("Magenheim_Underworld_IdentityParticles");
        particlesRoot.transform.SetParent(prefab.transform, false);
        particlesRoot.transform.localPosition = new Vector3(0f, height * .55f, 0f);

        var particles = particlesRoot.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = profile.Lifetime;
        main.startSpeed = profile.Speed;
        main.startSize = profile.Size;
        main.startColor = profile.ParticleColor;
        main.maxParticles = profile.MaxParticles;

        var emission = particles.emission;
        emission.rateOverTime = profile.Rate;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = profile.Radius;

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = profile.VerticalDrift;

        var lightCount = 0;
        if (profile.GlowIntensity > 0f)
        {
            var glowRoot = new GameObject("Magenheim_Underworld_IdentityGlow");
            glowRoot.transform.SetParent(prefab.transform, false);
            glowRoot.transform.localPosition = new Vector3(0f, height, .08f);

            var light = glowRoot.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = profile.GlowColor;
            light.range = profile.GlowRange;
            light.intensity = profile.GlowIntensity;
            light.shadows = LightShadows.None;
            lightCount = 1;
        }

        return $"{profile.Style} VFX ({profile.MaxParticles} particle cap, {lightCount} local light)";
    }

    private static FxProfile FxFor(UnderworldCreaturePrototypes.Entry entry)
    {
        FxProfile profile;
        switch (entry.Biome)
        {
            case "Fungal Forest":
                profile = new FxProfile("spore drift", new Color(.46f, .92f, .48f, .42f),
                    new Color(.48f, 1f, .54f), 2.2f, .08f, .055f, 4f, 16, .55f, .08f, 0f, 0f);
                break;
            case "Blackwater Deep":
                profile = new FxProfile("bioluminescent motes", new Color(.20f, .78f, .92f, .34f),
                    new Color(.12f, .78f, 1f), 1.8f, .05f, .045f, 2.4f, 12, .45f, .035f, 0f, 0f);
                break;
            case "Sulfurous Wastes":
                profile = new FxProfile("sulfur smoke", new Color(.22f, .16f, .10f, .32f),
                    new Color(1f, .28f, .035f), 2.5f, .10f, .11f, 4.5f, 20, .52f, .11f, 0f, 0f);
                break;
            case "Frozen Caverns":
                profile = new FxProfile("rime mist", new Color(.72f, .92f, 1f, .28f),
                    new Color(.55f, .88f, 1f), 1.7f, .045f, .07f, 3f, 14, .48f, .025f, 0f, 0f);
                break;
            case "Fracture Zones":
                profile = new FxProfile("rift sparks", new Color(.72f, .38f, 1f, .46f),
                    new Color(.68f, .28f, 1f), 1.15f, .16f, .035f, 4f, 14, .48f, .06f, 0f, 0f);
                break;
            default:
                profile = new FxProfile("decay motes", new Color(.52f, .62f, .16f, .32f),
                    new Color(.55f, .70f, .12f), 2.4f, .055f, .07f, 3.2f, 16, .52f, .045f, 0f, 0f);
                break;
        }

        switch (entry.Name)
        {
            case "Lantern Moth":
            case "Lantern Angler":
            case "Fracture Wisp":
                return profile.WithGlow(1.15f, 3.2f).WithRate(profile.Rate * 1.25f);
            case "Mycelial Stalker":
            case "Deep Hunter":
            case "Cinder Hound":
            case "Iceblind":
            case "Glacier Stalker":
            case "Shardwing":
            case "Decay Hound":
                return profile.WithGlow(.62f, 2.2f);
            case "Fume Wraith":
                return profile.WithRate(8f).WithRadius(.82f).WithGlow(.48f, 2.5f);
            case "Furnace Golem":
                return profile.WithRate(7f).WithRadius(.78f).WithGlow(1.45f, 4.2f);
            case "Cryolith Guardian":
                return profile.WithRate(5f).WithRadius(.72f).WithGlow(1.0f, 3.7f);
            case "Rift Colossus":
                return profile.WithRate(6f).WithRadius(.88f).WithGlow(1.25f, 4.4f);
            case "Carrion Bloom":
            case "Corpse Orchard":
                return profile.WithRate(6f).WithRadius(.85f).WithGlow(.62f, 3.0f);
            case "Puffback":
                return profile.WithRate(7f).WithRadius(.78f);
            default:
                return profile;
        }
    }

    private static float VisualHeight(string donor)
    {
        switch (donor)
        {
            case "Tick": return .28f;
            case "Bat":
            case "FrostWisp": return .42f;
            case "Leech": return .22f;
            case "Neck": return .48f;
            case "Serpent": return .58f;
            case "Seeker": return .70f;
            case "SeekerBrute": return .95f;
            case "Wolf": return .72f;
            case "Lox": return 1.35f;
            case "Hatchling": return .72f;
            case "Wraith": return 1.25f;
            case "Fenring": return 1.05f;
            case "StoneGolem": return 1.55f;
            case "Troll": return 1.75f;
            case "Draugr":
            case "Greydwarf_Shaman": return .95f;
            default: return .75f;
        }
    }

    private readonly struct FxProfile
    {
        internal FxProfile(
            string style,
            Color particleColor,
            Color glowColor,
            float lifetime,
            float speed,
            float size,
            float rate,
            int maxParticles,
            float radius,
            float verticalDrift,
            float glowIntensity,
            float glowRange)
        {
            Style = style;
            ParticleColor = particleColor;
            GlowColor = glowColor;
            Lifetime = lifetime;
            Speed = speed;
            Size = size;
            Rate = rate;
            MaxParticles = maxParticles;
            Radius = radius;
            VerticalDrift = verticalDrift;
            GlowIntensity = glowIntensity;
            GlowRange = glowRange;
        }

        internal string Style { get; }
        internal Color ParticleColor { get; }
        internal Color GlowColor { get; }
        internal float Lifetime { get; }
        internal float Speed { get; }
        internal float Size { get; }
        internal float Rate { get; }
        internal int MaxParticles { get; }
        internal float Radius { get; }
        internal float VerticalDrift { get; }
        internal float GlowIntensity { get; }
        internal float GlowRange { get; }

        internal FxProfile WithGlow(float intensity, float range) =>
            new FxProfile(Style, ParticleColor, GlowColor, Lifetime, Speed, Size, Rate, MaxParticles,
                Radius, VerticalDrift, intensity, range);

        internal FxProfile WithRate(float rate) =>
            new FxProfile(Style, ParticleColor, GlowColor, Lifetime, Speed, Size, rate, MaxParticles,
                Radius, VerticalDrift, GlowIntensity, GlowRange);

        internal FxProfile WithRadius(float radius) =>
            new FxProfile(Style, ParticleColor, GlowColor, Lifetime, Speed, Size, Rate, MaxParticles,
                radius, VerticalDrift, GlowIntensity, GlowRange);
    }
}

internal sealed class UnderworldCreatureElementalAttack : MonoBehaviour
{
    internal enum Element
    {
        None,
        Fire,
        Frost,
        Lightning,
        Poison,
        Spirit,
    }

    [SerializeField] private Element _element;
    [SerializeField] private float _fraction;

    internal void Configure(Element element, float fraction)
    {
        _element = element;
        _fraction = Mathf.Clamp(fraction, 0f, .25f);
    }

    internal void Augment(HitData hit)
    {
        if (hit == null || _element == Element.None || _fraction <= 0f) return;
        // Ride only on a direct physical donor hit. This prevents poison/fire/frost status ticks
        // from recursively manufacturing another elemental rider on every damage pulse.
        var physical = hit.m_damage.m_blunt + hit.m_damage.m_slash + hit.m_damage.m_pierce +
                       hit.m_damage.m_chop + hit.m_damage.m_pickaxe;
        if (physical <= 0f || float.IsNaN(physical) || float.IsInfinity(physical)) return;

        var bonus = physical * _fraction;
        switch (_element)
        {
            case Element.Fire: hit.m_damage.m_fire += bonus; break;
            case Element.Frost: hit.m_damage.m_frost += bonus; break;
            case Element.Lightning: hit.m_damage.m_lightning += bonus; break;
            case Element.Poison: hit.m_damage.m_poison += bonus; break;
            case Element.Spirit: hit.m_damage.m_spirit += bonus; break;
        }
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class UnderworldCreatureElementalAttackPatch
{
    private static void Prefix(Character __instance, ref HitData hit)
    {
        if (__instance == null || hit == null) return;
        var attacker = hit.GetAttacker();
        if (!attacker || ReferenceEquals(attacker, __instance)) return;

        var scaling = attacker.GetComponent<MagenheimCreatureCombatScaling>();
        var elemental = attacker.GetComponent<UnderworldCreatureElementalAttack>();
        if (scaling == null && elemental == null) return;

        // Some area attacks may reuse their source HitData across targets. Clone before Magenheim
        // modifies it so damage scaling/riders are per victim and cannot compound target-to-target.
        hit = hit.Clone();
        scaling?.ScaleOutgoing(hit);
        elemental?.Augment(hit);
        scaling?.CapOutgoing(hit);
    }
}
