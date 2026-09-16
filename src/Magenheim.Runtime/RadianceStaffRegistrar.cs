using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class RadianceStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal RadianceStaffRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

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
            if (PrefabManager.Instance.GetPrefab(BaseStaffPrefab) is null)
                throw new InvalidOperationException($"Required hidden staff carrier '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Radiance staff recipes are registered.");

            var flash = StaffEffectPayloads.CreateField(
                "Magenheim_Radiance_Flash", new HitData.DamageTypes { m_spirit = 2f },
                2.2f, .20f, .20f, 28f, new Color(1f, .93f, .52f, 1f), 1.65f);
            var sanctuary = StaffEffectPayloads.CreateField(
                "Magenheim_Radiance_Sanctuary", new HitData.DamageTypes { m_spirit = 6f },
                5.5f, 10f, 1f, 0f, new Color(1f, .98f, .78f, 1f), 1.85f);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Radiance_Projectile", new Color(1f, .90f, .38f, 1f), 1.45f, sourceStaffPrefab: BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Radiance_FlashProjectile", new Color(1f, .96f, .64f, 1f), 1.70f, flash, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Radiance_DaybreakProjectile", new Color(1f, .99f, .84f, 1f), 1.95f, sanctuary, BaseStaffPrefab));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Radiance abilities with owned Radiance staff bodies and stamina-only casting.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Radiance staff content registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static void RegisterStaff(Definition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Radiance staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_pierce = definition.PierceDamage, m_spirit = definition.SpiritDamage };
        shared.m_damagesPerLevel = new HitData.DamageTypes();
        shared.m_icons = new[] { EarthAssets.Icon("crystal", definition.AssetName, definition.Tint) };

        var attack = shared.m_attack;
        attack.m_attackProjectile = payloads.Resolve(definition.Payload);
        attack.m_attackStamina = definition.StaminaCost;
        attack.m_attackEitr = 0f;
        attack.m_damageMultiplier = definition.DamageMultiplier;
        attack.m_forceMultiplier = definition.ForceMultiplier;
        attack.m_staggerMultiplier = definition.StaggerMultiplier;
        attack.m_projectileVel = definition.ProjectileVelocity;
        attack.m_projectileVelMin = definition.ProjectileVelocity;
        attack.m_projectileAccuracy = definition.ProjectileAccuracy;
        attack.m_projectileAccuracyMin = definition.ProjectileAccuracy;
        attack.m_projectiles = definition.Projectiles;
        attack.m_projectileBursts = definition.Bursts;
        attack.m_burstInterval = definition.BurstInterval;
        attack.m_perBurstResourceUsage = false;

        RadianceVisuals.Apply(item.ItemPrefab, definition.AssetName);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Radiance staff item '{definition.PrefabName}'.");
    }

    private static void RegisterRecipe(Definition definition)
    {
        var config = new RecipeConfig
        {
            Name = "Magenheim_Recipe_" + definition.PrefabName,
            Item = definition.PrefabName,
            Amount = 1,
            CraftingStation = WorkshopRegistrar.StationPrefab,
            MinStationLevel = definition.MinimumStationLevel,
            Enabled = true,
        };
        foreach (var requirement in definition.Requirements) config.AddRequirement(requirement.PrefabName, requirement.Amount);
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException($"Jotunn refused Radiance staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<Definition> Definitions() => new[]
    {
        new Definition("Magenheim_Staff_Radiance_Simple", "staff-radiance-simple", "Simple Staff of Radiance",
            "Prism Spark: fires a needle of white-gold hard light. Pierce harms ordinary flesh while Spirit damage naturally punishes undead and other spirit-vulnerable corruption.",
            1, 20f, 8f, 10f, 1f, .35f, .85f, 45f, .35f, 1, 1, 0f, PayloadKind.Direct, new Color(.96f,.82f,.32f,1f),
            new Requirement("FineWood",8), new Requirement("Bronze",2), new Requirement("Magenheim_Crystal_Radiance_Simple",1)),
        new Definition("Magenheim_Staff_Radiance_Crystal", "staff-radiance-crystal", "Crystal Staff of Radiance",
            "Sun Lance: compresses Radiance into a nearly dispersionless line. High Pierce, heavy Spirit pressure, extreme velocity, and focused stagger reward deliberate aim.",
            2, 30f, 18f, 30f, 1f, 1.10f, 1.55f, 72f, .10f, 1, 1, 0f, PayloadKind.Direct, new Color(1f,.90f,.48f,1f),
            new Requirement("ElderBark",10), new Requirement("Silver",3), new Requirement("Magenheim_Crystal_Radiance_Crystal",1)),
        new Definition("Magenheim_Staff_Radiance_Advanced", "staff-radiance-advanced", "Advanced Staff of Radiance",
            "Corona Flash: fractures one cast into seven rays. Each impact blooms into a brief radiant flash, adding local Spirit pressure and violent interruption around clustered targets.",
            3, 30f, 4f, 8f, .55f, .55f, 1.35f, 48f, 9f, 7, 1, 0f, PayloadKind.Flash, new Color(1f,.95f,.68f,1f),
            new Requirement("YggdrasilWood",10), new Requirement("Silver",4), new Requirement("BlackMetal",2), new Requirement("Magenheim_Crystal_Radiance_Advanced",1)),
        new Definition("Magenheim_Staff_Radiance_Master", "staff-radiance-master", "Master Staff of Radiance",
            "Daybreak Sanctuary: drives one sun-bright lance into the target point and leaves a wide sanctified field for ten seconds. The field deals pure Spirit damage, making corrupted ground lethal to spirit-vulnerable enemies rather than becoming another artillery barrage.",
            4, 50f, 18f, 38f, 1f, 1.0f, 1.80f, 66f, .12f, 1, 1, 0f, PayloadKind.Sanctuary, new Color(1f,.99f,.84f,1f),
            new Requirement("YggdrasilWood",15), new Requirement("BlackMetal",4), new Requirement("Magenheim_Crystal_Radiance_Master",1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Direct, Flash, Sanctuary }

    private sealed class PayloadSet
    {
        private readonly GameObject _direct, _flash, _sanctuary;
        internal PayloadSet(GameObject direct, GameObject flash, GameObject sanctuary) { _direct=direct; _flash=flash; _sanctuary=sanctuary; }
        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Direct => _direct, PayloadKind.Flash => _flash, PayloadKind.Sanctuary => _sanctuary,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private readonly struct Requirement
    {
        internal Requirement(string prefabName, int amount) { PrefabName=prefabName; Amount=amount; }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class Definition
    {
        internal Definition(string prefabName, string assetName, string displayName, string description, int minimumStationLevel,
            float staminaCost, float pierceDamage, float spiritDamage, float damageMultiplier, float forceMultiplier,
            float staggerMultiplier, float projectileVelocity, float projectileAccuracy, int projectiles, int bursts, float burstInterval,
            PayloadKind payload, Color tint, params Requirement[] requirements)
        {
            PrefabName=prefabName; AssetName=assetName; DisplayName=displayName; Description=description; MinimumStationLevel=minimumStationLevel;
            StaminaCost=staminaCost; PierceDamage=pierceDamage; SpiritDamage=spiritDamage; DamageMultiplier=damageMultiplier;
            ForceMultiplier=forceMultiplier; StaggerMultiplier=staggerMultiplier; ProjectileVelocity=projectileVelocity; ProjectileAccuracy=projectileAccuracy;
            Projectiles=projectiles; Bursts=bursts; BurstInterval=burstInterval; Payload=payload; Tint=tint; Requirements=requirements;
        }
        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float PierceDamage { get; }
        internal float SpiritDamage { get; }
        internal float DamageMultiplier { get; }
        internal float ForceMultiplier { get; }
        internal float StaggerMultiplier { get; }
        internal float ProjectileVelocity { get; }
        internal float ProjectileAccuracy { get; }
        internal int Projectiles { get; }
        internal int Bursts { get; }
        internal float BurstInterval { get; }
        internal PayloadKind Payload { get; }
        internal Color Tint { get; }
        internal IReadOnlyList<Requirement> Requirements { get; }
    }
}

internal static class RadianceVisuals
{
    internal static void Apply(GameObject prefab,string assetName)
    {
        var context=ValheimStaffVisualBuilder.Begin(prefab,assetName);
        var source=context.SourceMaterial;
        var wood=ValheimStaffVisualBuilder.Surface(source,"radiance-staff","wood",new Color(.24f,.17f,.12f,1f),0f,.13f);
        var ivory=ValheimStaffVisualBuilder.Surface(source,"radiance-staff","ivory",new Color(.78f,.72f,.58f,1f),.03f,.23f);
        var leather=ValheimStaffVisualBuilder.Surface(source,"radiance-staff","leather-wrap",new Color(.19f,.13f,.08f,1f),.01f,.10f);
        var bronze=ValheimStaffVisualBuilder.Surface(source,"radiance-staff","bronze",new Color(.62f,.43f,.19f,1f),.46f,.28f);
        var gold=ValheimStaffVisualBuilder.Surface(source,"radiance-staff","gold",new Color(.90f,.65f,.18f,1f),.68f,.43f);
        var light=ValheimStaffVisualBuilder.Surface(source,"radiance-staff","radiance-light-crystal",new Color(1f,.91f,.48f,1f),.02f,.78f,.42f);
        var white=ValheimStaffVisualBuilder.Surface(source,"radiance-staff","radiance-white-crystal",new Color(1f,.99f,.86f,1f),.01f,.88f,.60f);

        switch(assetName)
        {
            case "staff-radiance-simple": BuildSimple(context.Root,wood,leather,bronze,light); break;
            case "staff-radiance-crystal": BuildCrystal(context.Root,ivory,leather,gold,light,white); break;
            case "staff-radiance-advanced": BuildAdvanced(context.Root,wood,leather,gold,light,white); break;
            case "staff-radiance-master": BuildMaster(context.Root,ivory,leather,gold,light,white); break;
            default: throw new InvalidOperationException($"Unknown Radiance geometry '{assetName}'.");
        }
        ValheimStaffVisualBuilder.Finish(context);
    }

    private static void BuildSimple(GameObject root,Material body,Material leather,Material bronze,Material light)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"radiance",body,leather,bronze,.039f,.70f,false);
        var hub=new Vector3(.015f,.53f,.005f);
        var left=new Vector3(-.16f,.82f,.025f);
        var right=new Vector3(.17f,.86f,-.02f);
        ValheimStaffVisualBuilder.Segment(root,"sun-fork-left",hub,left,.025f,bronze,8);
        ValheimStaffVisualBuilder.Segment(root,"sun-fork-right",hub,right,.024f,bronze,8);
        ValheimStaffVisualBuilder.Segment(root,"left-ray",left,new Vector3(-.22f,1.00f,.035f),.014f,bronze,6);
        ValheimStaffVisualBuilder.Segment(root,"right-ray",right,new Vector3(.26f,1.03f,-.03f),.013f,bronze,6);
        ValheimStaffVisualBuilder.Shard(root,"prism-spark",new Vector3(.020f,.89f,.004f),new Vector3(.16f,.36f,.16f),Quaternion.Euler(-3f,15f,3f),light,6);
        ValheimStaffVisualBuilder.Segment(root,"side-ray",new Vector3(-.08f,.66f,.04f),new Vector3(-.22f,.73f,.06f),.011f,bronze,6);
        ValheimStaffVisualBuilder.FocusLight(root,"radiance-light",new Vector3(.02f,.91f,.004f),new Color(1f,.86f,.38f),1.55f,.36f);
    }

    private static void BuildCrystal(GameObject root,Material body,Material leather,Material gold,Material light,Material white)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"radiance",body,leather,gold,.043f,.76f,true);
        var centre=new Vector3(.018f,.91f,.005f);
        for(var i=0;i<4;i++)
        {
            var a=i*Mathf.PI*.5f+.17f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.09f+Vector3.down*.26f;
            var upper=centre+radial*(i==1?.29f:.25f)+Vector3.up*(i%2==0?.18f:.12f);
            ValheimStaffVisualBuilder.Segment(root,"sun-lens-arm-a-"+i,lower,Vector3.Lerp(lower,upper,.56f),.017f,gold,8);
            ValheimStaffVisualBuilder.Segment(root,"sun-lens-arm-b-"+i,Vector3.Lerp(lower,upper,.56f),upper,.012f,gold,6);
            ValheimStaffVisualBuilder.Shard(root,"sun-ray-node-"+i,upper,new Vector3(.036f,.12f,.036f),Quaternion.Euler(11f*Mathf.Sin(a),a*Mathf.Rad2Deg,15f*Mathf.Cos(a)),light,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"sun-lance-focus",centre,new Vector3(.20f,.48f,.20f),Quaternion.Euler(-4f,20f,2f),white,7);
        ValheimStaffVisualBuilder.Shard(root,"golden-heart",centre+new Vector3(.03f,.11f,-.015f),new Vector3(.074f,.19f,.074f),Quaternion.Euler(8f,-12f,-4f),light,6);
        ValheimStaffVisualBuilder.FocusLight(root,"sun-lance-light",centre+Vector3.up*.05f,new Color(1f,.94f,.63f),1.85f,.46f);
    }

    private static void BuildAdvanced(GameObject root,Material body,Material leather,Material gold,Material light,Material white)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"radiance",body,leather,gold,.046f,.84f,true);
        var centre=new Vector3(.018f,.92f,.005f);
        for(var i=0;i<7;i++)
        {
            var a=i*Mathf.PI*2f/7f+.20f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.11f+Vector3.down*.24f;
            var upper=centre+radial*(i==5?.34f:.28f)+Vector3.up*(i%2==0?.22f:.14f);
            ValheimStaffVisualBuilder.Segment(root,"corona-ray-"+i,lower,upper,.013f,gold,6);
            ValheimStaffVisualBuilder.Shard(root,"corona-tip-"+i,upper,new Vector3(.040f,.13f,.040f),Quaternion.Euler(13f*Mathf.Sin(a),a*Mathf.Rad2Deg,18f*Mathf.Cos(a)),i%2==0?white:light,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"corona-core",centre,new Vector3(.24f,.51f,.24f),Quaternion.Euler(-3f,24f,2f),white,7);
        ValheimStaffVisualBuilder.Segment(root,"long-processional-ray",new Vector3(-.12f,.72f,.05f),new Vector3(-.34f,1.02f,.08f),.012f,gold,6);
        ValheimStaffVisualBuilder.Shard(root,"processional-tip",new Vector3(-.345f,1.06f,.08f),new Vector3(.038f,.13f,.038f),Quaternion.Euler(9f,0f,-14f),light,5);
        ValheimStaffVisualBuilder.FocusLight(root,"corona-light",centre+Vector3.up*.06f,new Color(1f,.97f,.74f),2.05f,.56f);
    }

    private static void BuildMaster(GameObject root,Material body,Material leather,Material gold,Material light,Material white)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"radiance",body,leather,gold,.050f,.90f,true);
        foreach(var y in new[]{-.34f,-.08f,.20f,.46f})
            ValheimStaffVisualBuilder.Band(root,"daybreak-band-"+y.ToString("0.00"),new Vector3(.005f*y,y,.003f),.075f,.045f,gold,10);
        var basePoint=new Vector3(.018f,.52f,.005f);
        var left=new Vector3(-.18f,.73f,.04f);
        var right=new Vector3(.19f,.70f,-.03f);
        ValheimStaffVisualBuilder.Segment(root,"sanctuary-left",basePoint,left,.027f,gold,8);
        ValheimStaffVisualBuilder.Segment(root,"sanctuary-right",basePoint,right,.026f,gold,8);
        ValheimStaffVisualBuilder.Segment(root,"sanctuary-left-spire",left,new Vector3(-.30f,1.15f,.06f),.016f,gold,7);
        ValheimStaffVisualBuilder.Segment(root,"sanctuary-right-spire",right,new Vector3(.31f,1.11f,-.04f),.015f,gold,7);
        var centre=new Vector3(.018f,.96f,.005f);
        for(var i=0;i<9;i++)
        {
            var a=i*Mathf.PI*2f/9f+.18f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.14f+Vector3.down*.21f;
            var upper=centre+radial*(i%4==0?.34f:.28f)+Vector3.up*(i%2==0?.19f:.13f);
            ValheimStaffVisualBuilder.Segment(root,"daybreak-ray-"+i,lower,upper,.011f,gold,6);
            if(i%2==0) ValheimStaffVisualBuilder.Shard(root,"daybreak-node-"+i,upper,new Vector3(.040f,.14f,.040f),Quaternion.Euler(12f*Mathf.Sin(a),a*Mathf.Rad2Deg,15f*Mathf.Cos(a)),light,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"daybreak-core",centre,new Vector3(.30f,.64f,.30f),Quaternion.Euler(-4f,27f,3f),white,8);
        ValheimStaffVisualBuilder.Shard(root,"daybreak-heart",centre+new Vector3(.032f,.13f,-.018f),new Vector3(.10f,.26f,.10f),Quaternion.Euler(8f,-14f,-5f),light,6);
        ValheimStaffVisualBuilder.Segment(root,"hanging-votive",new Vector3(.23f,.89f,-.06f),new Vector3(.31f,.72f,-.09f),.009f,gold,6);
        ValheimStaffVisualBuilder.Shard(root,"votive-light",new Vector3(.315f,.68f,-.09f),new Vector3(.036f,.12f,.036f),Quaternion.Euler(170f,0f,-8f),white,5);
        ValheimStaffVisualBuilder.FocusLight(root,"daybreak-light",centre+Vector3.up*.08f,new Color(1f,.99f,.86f),2.50f,.72f);
    }
}
