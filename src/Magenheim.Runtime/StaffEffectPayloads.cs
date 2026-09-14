using System;
using System.Reflection;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Small content-facing factory for Magenheim-owned spell payloads. It clones proven vanilla
/// projectile/AOE carriers, strips inherited elemental behavior, applies the requested Magenheim
/// damage/field behavior, and registers only new Magenheim prefab identities.
/// </summary>
internal static class StaffEffectPayloads
{
    private const string BaseStaffPrefab = "StaffFireball";
    private const string AreaCarrierPrefab = "BombOoze";

    internal static GameObject CreateProjectile(
        string prefabName,
        Color tint,
        float emission,
        GameObject? spawnOnHit = null)
    {
        EnsureIdentityFree(prefabName);

        var baseStaff = PrefabManager.Instance.GetPrefab(BaseStaffPrefab)
            ?? throw new InvalidOperationException($"Required spell carrier '{BaseStaffPrefab}' is unavailable.");
        var itemDrop = baseStaff.GetComponent<ItemDrop>()
            ?? throw new InvalidOperationException($"Spell carrier '{BaseStaffPrefab}' has no ItemDrop component.");
        var sourceProjectile = itemDrop.m_itemData.m_shared.m_attack.m_attackProjectile
            ?? throw new InvalidOperationException($"Spell carrier '{BaseStaffPrefab}' has no attack projectile.");

        var clone = PrefabManager.Instance.CreateClonedPrefab(prefabName, sourceProjectile)
            ?? throw new InvalidOperationException($"Unable to clone spell projectile '{sourceProjectile.name}' as '{prefabName}'.");
        var projectile = clone.GetComponent<Projectile>()
            ?? throw new InvalidOperationException($"Cloned spell payload '{prefabName}' has no Projectile component.");

        // Remove Staff of Embers' baked combat identity. Attack/item damage is supplied by the
        // owning Magenheim staff; optional spawn-on-hit content is explicitly Magenheim-owned.
        SetOptionalField(projectile, "m_damage", new HitData.DamageTypes());
        SetOptionalField(projectile, "m_aoe", 0f);
        SetOptionalField(projectile, "m_attackForce", 0f);
        SetOptionalField(projectile, "m_statusEffect", null);
        SetRequiredField(projectile, "m_spawnOnHit", spawnOnHit, prefabName);
        SetOptionalField(projectile, "m_spawnOnHitChance", 1f);
        SetOptionalField(projectile, "m_spawnCount", 1);
        SetOptionalField(projectile, "m_onlySpawnedProjectilesDealDamage", false);
        SetOptionalField(projectile, "m_divideDamageBetweenProjectiles", false);

        TintOwnedPrefab(clone, tint, emission);
        PrefabManager.Instance.AddPrefab(clone);
        EnsureRegistered(prefabName);
        return clone;
    }

    internal static GameObject CreateField(
        string prefabName,
        HitData.DamageTypes damage,
        float radius,
        float ttl,
        float hitInterval,
        float attackForce,
        Color tint,
        float emission)
    {
        EnsureIdentityFree(prefabName);

        var bomb = PrefabManager.Instance.GetPrefab(AreaCarrierPrefab)
            ?? throw new InvalidOperationException($"Required field carrier '{AreaCarrierPrefab}' is unavailable.");
        var bombDrop = bomb.GetComponent<ItemDrop>()
            ?? throw new InvalidOperationException($"Field carrier '{AreaCarrierPrefab}' has no ItemDrop component.");
        var bombProjectile = bombDrop.m_itemData.m_shared.m_attack.m_attackProjectile
            ?? throw new InvalidOperationException($"Field carrier '{AreaCarrierPrefab}' has no attack projectile.");
        var projectile = bombProjectile.GetComponent<Projectile>()
            ?? throw new InvalidOperationException($"Field carrier projectile '{bombProjectile.name}' has no Projectile component.");
        var sourceArea = ReadRequiredField<GameObject>(projectile, "m_spawnOnHit", AreaCarrierPrefab)
            ?? throw new InvalidOperationException($"Field carrier '{AreaCarrierPrefab}' has no spawn-on-hit area.");

        var clone = PrefabManager.Instance.CreateClonedPrefab(prefabName, sourceArea)
            ?? throw new InvalidOperationException($"Unable to clone field '{sourceArea.name}' as '{prefabName}'.");
        var aoe = clone.GetComponent<Aoe>()
            ?? throw new InvalidOperationException($"Cloned spell field '{prefabName}' has no Aoe component.");

        SetRequiredField(aoe, "m_damage", damage, prefabName);
        SetOptionalField(aoe, "m_damagePerLevel", new HitData.DamageTypes());
        SetOptionalField(aoe, "m_useAttackSettings", false);
        SetOptionalField(aoe, "m_scaleDamageByDistance", false);
        SetRequiredField(aoe, "m_radius", radius, prefabName);
        SetRequiredField(aoe, "m_ttl", ttl, prefabName);
        SetOptionalField(aoe, "m_ttlMax", ttl);
        SetRequiredField(aoe, "m_hitInterval", hitInterval, prefabName);
        SetOptionalField(aoe, "m_activationDelay", 0f);
        SetOptionalField(aoe, "m_attackForce", attackForce);
        SetOptionalField(aoe, "m_dodgeable", false);
        SetOptionalField(aoe, "m_blockable", false);
        SetOptionalField(aoe, "m_hitOwner", false);
        SetOptionalField(aoe, "m_hitParent", false);
        SetOptionalField(aoe, "m_hitSame", false);
        SetOptionalField(aoe, "m_hitFriendly", false);
        SetRequiredField(aoe, "m_hitEnemy", true, prefabName);
        SetRequiredField(aoe, "m_hitCharacters", true, prefabName);
        SetOptionalField(aoe, "m_hitProps", false);
        SetOptionalField(aoe, "m_hitTerrain", false);
        SetOptionalField(aoe, "m_ignorePVP", true);
        SetOptionalField(aoe, "m_skill", Skills.SkillType.ElementalMagic);
        SetOptionalField(aoe, "m_canRaiseSkill", false);
        SetOptionalField(aoe, "m_hitOnEnable", true);
        SetOptionalField(aoe, "m_hitAfterTtl", false);
        SetOptionalField(aoe, "m_attachToCaster", false);
        ClearOptionalReferenceField(aoe, "m_statusEffect");
        ClearOptionalReferenceField(aoe, "m_statusEffectIfBoss");
        ClearOptionalReferenceField(aoe, "m_statusEffectIfPlayer");

        TintOwnedPrefab(clone, tint, emission);
        PrefabManager.Instance.AddPrefab(clone);
        EnsureRegistered(prefabName);
        return clone;
    }

    private static T? ReadRequiredField<T>(object target, string fieldName, string ownerName) where T : class
    {
        var field = FindField(target.GetType(), fieldName)
            ?? throw new InvalidOperationException($"Prefab '{ownerName}' requires runtime field '{target.GetType().FullName}.{fieldName}'.");
        return field.GetValue(target) as T;
    }

    private static void SetRequiredField(object target, string fieldName, object? value, string ownerName)
    {
        var field = FindField(target.GetType(), fieldName)
            ?? throw new InvalidOperationException($"Prefab '{ownerName}' requires runtime field '{target.GetType().FullName}.{fieldName}'.");
        field.SetValue(target, value);
    }

    private static void SetOptionalField(object target, string fieldName, object? value)
    {
        var field = FindField(target.GetType(), fieldName);
        if (field is not null)
            field.SetValue(target, value);
    }

    private static void ClearOptionalReferenceField(object target, string fieldName)
    {
        var field = FindField(target.GetType(), fieldName);
        if (field is not null && !field.FieldType.IsValueType)
            field.SetValue(target, null);
    }

    private static FieldInfo? FindField(Type type, string fieldName) =>
        type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private static void EnsureIdentityFree(string prefabName)
    {
        if (PrefabManager.Instance.GetPrefab(prefabName) is not null)
            throw new InvalidOperationException($"Cannot replace occupied Magenheim spell payload identity '{prefabName}'.");
    }

    private static void EnsureRegistered(string prefabName)
    {
        if (PrefabManager.Instance.GetPrefab(prefabName) is null)
            throw new InvalidOperationException($"Jotunn did not retain Magenheim spell payload prefab '{prefabName}'.");
    }

    private static void TintOwnedPrefab(GameObject prefab, Color tint, float emissionMultiplier)
    {
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var sourceMaterials = renderer.sharedMaterials;
            var clonedMaterials = new Material[sourceMaterials.Length];
            for (var i = 0; i < sourceMaterials.Length; i++)
            {
                var source = sourceMaterials[i];
                if (!source) continue;
                var material = new Material(source) { name = $"magenheim.spell.{prefab.name}.{i}" };
                if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", tint * emissionMultiplier);
                    material.EnableKeyword("_EMISSION");
                }
                clonedMaterials[i] = material;
            }
            renderer.sharedMaterials = clonedMaterials;
        }

        foreach (var particles in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(tint);
        }

        foreach (var light in prefab.GetComponentsInChildren<Light>(true))
        {
            light.color = tint;
            light.intensity = Mathf.Max(light.intensity, emissionMultiplier);
        }
    }
}
