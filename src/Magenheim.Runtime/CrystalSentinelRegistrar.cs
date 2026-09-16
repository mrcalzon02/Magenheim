using System;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the floating Crystal Sentinel and its single shard-refined Crystal Munition.</summary>
internal sealed class CrystalSentinelRegistrar : IDisposable
{
    internal const string SentinelPrefab = "Magenheim_CrystalSentinel";
    internal const string MunitionPrefab = "Magenheim_CrystalMunition";
    internal const string AmmoType = "magenheim.crystal_sentinel";
    internal const int DefaultMunitionsPerShard = 10;

    private static readonly Color MunitionTint = new(.72f, .91f, 1f, 1f);
    private readonly ManualLogSource _log;
    private readonly int _munitionsPerShard;
    private bool _subscribed;
    private bool _registered;

    internal CrystalSentinelRegistrar(ManualLogSource log, ConfigFile config)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        if (config is null) throw new ArgumentNullException(nameof(config));

        _munitionsPerShard = config.Bind(
            "Balance.CrystalSentinel",
            "MunitionsPerShard",
            DefaultMunitionsPerShard,
            new ConfigDescription(
                "Number of Crystal Munitions produced from one elemental shard. Requires restart because recipes are registered during bootstrap.",
                new AcceptableValueRange<int>(1, 100))).Value;
    }

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _subscribed = true;
    }

    private void RegisterContent()
    {
        if (_registered) return;
        try
        {
            if (PrefabManager.Instance.GetPrefab("piece_turret") is null)
                throw new InvalidOperationException("Vanilla ballista source 'piece_turret' is unavailable.");
            if (PrefabManager.Instance.GetPrefab("TurretBolt") is null)
                throw new InvalidOperationException("Vanilla ballista munition source 'TurretBolt' is unavailable.");

            var ammo = RegisterMunition();
            RegisterSentinel(ammo);
            _registered = true;
            _log.LogInfo(
                $"Registered Crystal Sentinel plus one Crystal Munition item; every elemental shard refines into {_munitionsPerShard} shots.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Crystal Sentinel registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private ItemDrop RegisterMunition()
    {
        if (PrefabManager.Instance.GetPrefab(MunitionPrefab) || CustomItem.IsCustomItem(MunitionPrefab))
            throw new InvalidOperationException($"Cannot replace occupied Crystal Munition identity '{MunitionPrefab}'.");

        var item = new CustomItem(MunitionPrefab, "TurretBolt");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Crystal Munition";
        shared.m_description =
            "A dense shaped crystal dart cut, sleeved, and balanced for use in a Crystal Sentinel. Any elemental shard can be refined into this standardized munition.";
        shared.m_ammoType = AmmoType;
        shared.m_maxStackSize = 100;
        shared.m_weight = .25f;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;

        var sourceProjectile = shared.m_attack.m_attackProjectile
            ?? throw new InvalidOperationException("TurretBolt no longer exposes an attack projectile.");
        const string projectileName = "Magenheim_CrystalMunitionProjectile";
        if (PrefabManager.Instance.GetPrefab(projectileName))
            throw new InvalidOperationException($"Cannot replace occupied projectile identity '{projectileName}'.");
        var projectile = PrefabManager.Instance.CreateClonedPrefab(projectileName, sourceProjectile)
            ?? throw new InvalidOperationException("Unable to clone Crystal Munition projectile.");
        CrystalSentinelVisuals.TintProjectile(projectile);
        PrefabManager.Instance.AddPrefab(projectile);
        shared.m_attack.m_attackProjectile = projectile;

        TintItem(item.ItemPrefab);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Crystal Munition item '{MunitionPrefab}'.");

        foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
            RegisterRefiningRecipe(element);

        return item.ItemDrop;
    }

    private void RegisterRefiningRecipe(ElementalAlignment element)
    {
        var recipe = new RecipeConfig
        {
            Name = $"Magenheim_Recipe_CrystalMunition_From_{element}",
            Item = MunitionPrefab,
            Amount = _munitionsPerShard,
            CraftingStation = WorkshopRegistrar.StationPrefab,
            MinStationLevel = 2,
            Enabled = true,
        };
        recipe.AddRequirement($"Magenheim_Shard_{element}", 1);
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(recipe)))
            throw new InvalidOperationException($"Jotunn refused Crystal Munition recipe '{recipe.Name}'.");
    }

    private static void RegisterSentinel(ItemDrop ammo)
    {
        if (PrefabManager.Instance.GetPrefab(SentinelPrefab))
            throw new InvalidOperationException($"Cannot replace occupied Crystal Sentinel identity '{SentinelPrefab}'.");

        var config = new PieceConfig
        {
            Name = "Crystal Sentinel",
            Description = "A large floating crystal bound in iron rings. It tracks hostile targets like a ballista and fires standardized Crystal Munitions refined from any elemental shard.",
            PieceTable = "Hammer",
            Category = "Defence",
            CraftingStation = "piece_artisanstation",
            Icon = CrystalSentinelIcons.Icon(),
            Requirements = new[]
            {
                Cost("BlackMetal", 20),
                Cost("YggdrasilWood", 12),
                Cost("MechanicalSpring", 4),
                Cost("RefinedEitr", 8),
                Cost("Crystal", 12),
            }
        };

        var custom = new CustomPiece(SentinelPrefab, "piece_turret", config);
        custom.Piece.m_dlc = string.Empty;
        var prefab = custom.PiecePrefab;
        var turret = prefab.GetComponent<Turret>()
            ?? throw new InvalidOperationException("Cloned Crystal Sentinel source has no Turret component.");

        CrystalSentinelVisuals.Apply(prefab);
        turret.m_name = "Crystal Sentinel";
        turret.m_ammoType = AmmoType;
        turret.m_defaultAmmo = null;
        turret.m_allowedAmmo.Clear();
        turret.m_maxAmmo = 20;
        turret.m_returnAmmoOnDestroy = true;

        var sourceMaterial = prefab.GetComponentsInChildren<Renderer>(true)
            .Select(renderer => renderer.sharedMaterial)
            .FirstOrDefault(material => material)
            ?? throw new InvalidOperationException("Crystal Sentinel has no material source for loaded-ammo indicator.");

        var indicator = CrystalSentinelVisuals.CreateAmmoVisual(turret, sourceMaterial);
        turret.m_allowedAmmo.Add(new Turret.AmmoType
        {
            m_ammo = ammo,
            m_visual = indicator,
        });

        var wear = prefab.GetComponent<WearNTear>();
        if (wear) wear.m_health = Mathf.Max(wear.m_health, 1800f);

        if (!PieceManager.Instance.AddPiece(custom))
            throw new InvalidOperationException("Jotunn refused Crystal Sentinel piece registration.");
    }

    private static void TintItem(GameObject prefab)
    {
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var sources = renderer.sharedMaterials;
            var materials = new Material[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (!source) continue;
                var material = new Material(source) { name = $"magenheim.munition.crystal.{i}" };
                GeneratedSurfaceTextures.Apply(material, "crystal-munition");
                if (material.HasProperty("_Color")) material.SetColor("_Color", MunitionTint);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", MunitionTint * .65f);
                    material.EnableKeyword("_EMISSION");
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static RequirementConfig Cost(string item, int amount) =>
        new RequirementConfig(item, amount, 0, true);

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }
}
