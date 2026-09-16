using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Four-tier Seidr staff family built around binding sorcery and owned Magenheim staff geometry.</summary>
internal sealed class SeidrStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal SeidrStaffRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

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
                throw new InvalidOperationException("Geologist's Workstation must exist before Seidr staff recipes are registered.");

            var snare = RegisterBindingEffect("Magenheim_SE_SeidrSnare", "Seidr Snare", "Binding runes drag against every step.", 2.2f, -.20f, new Color(.63f, .27f, .94f, 1f));
            var fateBind = RegisterBindingEffect("Magenheim_SE_SeidrFateBind", "Fate Bind", "A woven fate-line constrains movement until the omen releases.", 4f, -.35f, new Color(.84f, .53f, 1f, 1f));

            var hexMark = StaffEffectPayloads.CreateField("Magenheim_Seidr_HexMark", new HitData.DamageTypes { m_spirit = .5f }, .70f, .15f, .15f, 0f, new Color(.57f, .19f, .88f, 1f), 1.35f, snare);
            var runeSeal = StaffEffectPayloads.CreateField("Magenheim_Seidr_RuneSeal", new HitData.DamageTypes { m_spirit = 3f }, 2.4f, .22f, .22f, 8f, new Color(.67f, .28f, .96f, 1f), 1.50f, snare);
            var witchweave = StaffEffectPayloads.CreateField("Magenheim_Seidr_WitchweaveSeal", new HitData.DamageTypes { m_spirit = 1.5f }, 1.35f, .26f, .26f, 3f, new Color(.75f, .39f, 1f, 1f), 1.60f, snare);
            var fateKnot = StaffEffectPayloads.CreateField("Magenheim_Seidr_FateKnot", new HitData.DamageTypes { m_spirit = 2f }, 1.65f, .36f, .36f, 4f, new Color(.88f, .64f, 1f, 1f), 1.80f, fateBind);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Seidr_HexProjectile", new Color(.52f, .16f, .85f, 1f), 1.35f, hexMark, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Seidr_RuneProjectile", new Color(.64f, .25f, .95f, 1f), 1.50f, runeSeal, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Seidr_WeaveProjectile", new Color(.74f, .38f, 1f, 1f), 1.62f, witchweave, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Seidr_FateProjectile", new Color(.88f, .62f, 1f, 1f), 1.85f, fateKnot, BaseStaffPrefab));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Seidr abilities with owned Seidr staff bodies and stamina-only casting.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Seidr staff content registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static StatusEffect RegisterBindingEffect(string identity, string displayName, string tooltip, float ttl, float speedModifier, Color tint)
    {
        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = identity;
        effect.m_name = displayName;
        effect.m_tooltip = tooltip;
        effect.m_ttl = ttl;
        effect.m_icon = EarthAssets.Icon("crystal", identity, tint);
        var speedField = typeof(SE_Stats).GetField("m_speedModifier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Current Valheim SE_Stats no longer exposes the m_speedModifier field required by Seidr binding.");
        speedField.SetValue(effect, speedModifier);
        var custom = new CustomStatusEffect(effect, fixReference: false);
        if (!ItemManager.Instance.AddStatusEffect(custom)) throw new InvalidOperationException($"Jotunn refused Seidr status effect '{identity}'.");
        return custom.StatusEffect;
    }

    private static void RegisterStaff(Definition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Seidr staff identity '{definition.PrefabName}'.");
        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_spirit = definition.SpiritDamage };
        shared.m_damagesPerLevel = new HitData.DamageTypes();
        shared.m_icons = new[] { EarthAssets.Icon("crystal", definition.AssetName, new Color(.67f, .30f, .96f, 1f)) };

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

        SeidrVisuals.Apply(item.ItemPrefab, definition.AssetName);
        if (!ItemManager.Instance.AddItem(item)) throw new InvalidOperationException($"Jotunn refused Seidr staff item '{definition.PrefabName}'.");
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
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config))) throw new InvalidOperationException($"Jotunn refused Seidr staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<Definition> Definitions() => new[]
    {
        new Definition("Magenheim_Staff_Seidr_Simple", "staff-seidr-simple", "Simple Staff of Seidr",
            "Hex Needle: drives one omen-laced bolt into the chosen target. The impact inscribes a brief Seidr Snare, dragging twenty percent from movement instead of pretending the hex is only damage.",
            1, 18f, 15f, 1f, .30f, .40f, 44f, .60f, 1, 1, 0f, PayloadKind.Hex,
            new Requirement("FineWood", 8), new Requirement("GreydwarfEye", 6), new Requirement("Magenheim_Crystal_Seidr_Simple", 1)),
        new Definition("Magenheim_Staff_Seidr_Crystal", "staff-seidr-crystal", "Crystal Staff of Seidr",
            "Rune Spear: hurls one heavy sorcerous lance. Its impact opens a binding seal around the struck point, damaging nearby spirits and catching movement in the same rune.",
            2, 28f, 34f, 1f, .70f, .90f, 54f, .25f, 1, 1, 0f, PayloadKind.Rune,
            new Requirement("ElderBark", 10), new Requirement("AncientSeed", 3), new Requirement("Silver", 2), new Requirement("Magenheim_Crystal_Seidr_Crystal", 1)),
        new Definition("Magenheim_Staff_Seidr_Advanced", "staff-seidr-advanced", "Advanced Staff of Seidr",
            "Witchweave: casts five crossing omen-lines. Every line leaves a small binding seal, trading raw impact for a fan of overlapping snares that catches evasive or clustered enemies.",
            3, 28f, 9f, .50f, .35f, .55f, 42f, 9f, 5, 1, 0f, PayloadKind.Weave,
            new Requirement("YggdrasilWood", 10), new Requirement("BlackCore", 2), new Requirement("Magenheim_Crystal_Seidr_Advanced", 1)),
        new Definition("Magenheim_Staff_Seidr_Master", "staff-seidr-master", "Master Staff of Seidr",
            "Fate Loom: releases three omen-lines through four successive responses. Every impact knots a stronger Fate Bind into the ground, reducing movement by thirty-five percent for four seconds and turning the target lane into controlled sorcery.",
            4, 48f, 8f, .40f, .40f, .65f, 45f, 7f, 3, 4, .13f, PayloadKind.Fate,
            new Requirement("YggdrasilWood", 15), new Requirement("BlackCore", 4), new Requirement("Magenheim_Crystal_Seidr_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Hex, Rune, Weave, Fate }

    private sealed class PayloadSet
    {
        private readonly GameObject _hex, _rune, _weave, _fate;
        internal PayloadSet(GameObject hex, GameObject rune, GameObject weave, GameObject fate) { _hex=hex; _rune=rune; _weave=weave; _fate=fate; }
        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Hex => _hex, PayloadKind.Rune => _rune, PayloadKind.Weave => _weave, PayloadKind.Fate => _fate,
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
            float staminaCost, float spiritDamage, float damageMultiplier, float forceMultiplier, float staggerMultiplier,
            float projectileVelocity, float projectileAccuracy, int projectiles, int bursts, float burstInterval, PayloadKind payload,
            params Requirement[] requirements)
        {
            PrefabName=prefabName; AssetName=assetName; DisplayName=displayName; Description=description; MinimumStationLevel=minimumStationLevel;
            StaminaCost=staminaCost; SpiritDamage=spiritDamage; DamageMultiplier=damageMultiplier; ForceMultiplier=forceMultiplier;
            StaggerMultiplier=staggerMultiplier; ProjectileVelocity=projectileVelocity; ProjectileAccuracy=projectileAccuracy; Projectiles=projectiles;
            Bursts=bursts; BurstInterval=burstInterval; Payload=payload; Requirements=requirements;
        }
        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
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
        internal IReadOnlyList<Requirement> Requirements { get; }
    }
}

internal static class SeidrVisuals
{
    internal static void Apply(GameObject prefab,string assetName)
    {
        var context=ValheimStaffVisualBuilder.Begin(prefab,assetName);
        var source=context.SourceMaterial;
        var blackWood=ValheimStaffVisualBuilder.Surface(source,"seidr-staff","black-wood",new Color(.10f,.07f,.12f,1f),0f,.12f);
        var leather=ValheimStaffVisualBuilder.Surface(source,"seidr-staff","leather-wrap",new Color(.11f,.07f,.09f,1f),.01f,.09f);
        var iron=ValheimStaffVisualBuilder.Surface(source,"seidr-staff","black-iron",new Color(.16f,.13f,.20f,1f),.62f,.30f);
        var silver=ValheimStaffVisualBuilder.Surface(source,"seidr-staff","rune-silver",new Color(.54f,.48f,.62f,1f),.55f,.38f);
        var violet=ValheimStaffVisualBuilder.Surface(source,"seidr-staff","seidr-violet-crystal",new Color(.62f,.20f,.95f,1f),.02f,.74f,.42f);
        var pale=ValheimStaffVisualBuilder.Surface(source,"seidr-staff","seidr-pale-eitr-crystal",new Color(.83f,.62f,1f,1f),.01f,.86f,.70f);

        switch(assetName)
        {
            case "staff-seidr-simple": BuildSimple(context.Root,blackWood,leather,iron,silver,violet); break;
            case "staff-seidr-crystal": BuildCrystal(context.Root,blackWood,leather,silver,violet,pale); break;
            case "staff-seidr-advanced": BuildAdvanced(context.Root,blackWood,leather,silver,iron,violet,pale); break;
            case "staff-seidr-master": BuildMaster(context.Root,blackWood,leather,iron,silver,violet,pale); break;
            default: throw new InvalidOperationException($"Unknown Seidr geometry '{assetName}'.");
        }
        ValheimStaffVisualBuilder.Finish(context);
    }

    private static void BuildSimple(GameObject root,Material wood,Material leather,Material iron,Material silver,Material violet)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"seidr",wood,leather,iron,.039f,1.35f,false);
        var hub=new Vector3(.018f,.52f,.006f);
        var left=new Vector3(-.18f,.85f,.05f);
        var right=new Vector3(.13f,.90f,-.03f);
        ValheimStaffVisualBuilder.Segment(root,"hex-crook-left",hub,left,.025f,iron,8);
        ValheimStaffVisualBuilder.Segment(root,"hex-crook-right",hub,right,.022f,silver,8);
        ValheimStaffVisualBuilder.Segment(root,"left-omen-hook",left,new Vector3(-.10f,1.05f,.07f),.015f,iron,6);
        ValheimStaffVisualBuilder.Segment(root,"right-omen-hook",right,new Vector3(.23f,1.00f,-.045f),.013f,silver,6);
        ValheimStaffVisualBuilder.Shard(root,"hex-focus",new Vector3(.018f,.91f,.005f),new Vector3(.16f,.34f,.16f),Quaternion.Euler(7f,19f,-8f),violet,6);
        ValheimStaffVisualBuilder.Shard(root,"omen-splinter",new Vector3(-.16f,.73f,.075f),new Vector3(.040f,.13f,.040f),Quaternion.Euler(16f,6f,22f),violet,5);
        ValheimStaffVisualBuilder.FocusLight(root,"hex-light",new Vector3(.018f,.93f,.005f),new Color(.62f,.25f,.95f),1.45f,.30f);
    }

    private static void BuildCrystal(GameObject root,Material wood,Material leather,Material silver,Material violet,Material pale)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"seidr",wood,leather,silver,.043f,1.45f,true);
        var centre=new Vector3(.018f,.94f,.005f);
        var tips=new[]
        {
            new Vector3(-.21f,1.09f,.04f),new Vector3(.20f,1.13f,-.03f),
            new Vector3(-.05f,.99f,-.20f),new Vector3(.08f,.93f,.19f)
        };
        for(var i=0;i<tips.Length;i++)
        {
            var lower=new Vector3(.018f,.57f,.005f)+new Vector3((i-1.5f)*.008f,0f,0f);
            var elbow=Vector3.Lerp(lower,tips[i],.58f)+Vector3.up*.015f;
            ValheimStaffVisualBuilder.Segment(root,"rune-spear-a-"+i,lower,elbow,.018f,silver,8);
            ValheimStaffVisualBuilder.Segment(root,"rune-spear-b-"+i,elbow,tips[i],.012f,silver,6);
            ValheimStaffVisualBuilder.Shard(root,"rune-node-"+i,tips[i],new Vector3(.038f,.12f,.038f),Quaternion.Euler(13f*i,21f*i,-7f*i),violet,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"rune-spear-core",centre,new Vector3(.19f,.50f,.19f),Quaternion.Euler(-5f,22f,3f),violet,7);
        ValheimStaffVisualBuilder.Shard(root,"rune-spear-tip",centre+Vector3.up*.26f,new Vector3(.075f,.20f,.075f),Quaternion.Euler(4f,-13f,-3f),pale,6);
        ValheimStaffVisualBuilder.FocusLight(root,"rune-spear-light",centre+Vector3.up*.08f,new Color(.74f,.43f,1f),1.70f,.40f);
    }

    private static void BuildAdvanced(GameObject root,Material wood,Material leather,Material silver,Material iron,Material violet,Material pale)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"seidr",wood,leather,iron,.046f,1.55f,true);
        var centre=new Vector3(.018f,.93f,.005f);
        for(var i=0;i<5;i++)
        {
            var a=i*Mathf.PI*2f/5f+.28f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.10f+Vector3.down*.25f;
            var elbow=centre+radial*.20f+Vector3.down*.01f;
            var upper=centre+radial*(i==3?.33f:.27f)+Vector3.up*(i%2==0?.21f:.14f);
            ValheimStaffVisualBuilder.Segment(root,"witchweave-a-"+i,lower,elbow,.017f,i%2==0?silver:iron,7);
            ValheimStaffVisualBuilder.Segment(root,"witchweave-b-"+i,elbow,upper,.011f,silver,6);
            ValheimStaffVisualBuilder.Shard(root,"witch-node-"+i,upper,new Vector3(.039f,.13f,.039f),Quaternion.Euler(14f*Mathf.Sin(a),a*Mathf.Rad2Deg,18f*Mathf.Cos(a)),i%2==0?pale:violet,5);
        }
        for(var i=0;i<5;i++)
        {
            var a=i*Mathf.PI*2f/5f+.28f;
            var b=(i+2)%5*Mathf.PI*2f/5f+.28f;
            var p1=centre+new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a))*.19f;
            var p2=centre+new Vector3(Mathf.Cos(b),0f,Mathf.Sin(b))*.19f+Vector3.up*.025f;
            ValheimStaffVisualBuilder.Segment(root,"woven-cross-"+i,p1,p2,.008f,iron,6);
        }
        ValheimStaffVisualBuilder.Shard(root,"witchweave-core",centre,new Vector3(.23f,.48f,.23f),Quaternion.Euler(-4f,25f,2f),pale,7);
        ValheimStaffVisualBuilder.Segment(root,"asymmetric-omen-hook",new Vector3(-.14f,.73f,.05f),new Vector3(-.34f,.99f,.09f),.013f,iron,6);
        ValheimStaffVisualBuilder.Shard(root,"hook-omen",new Vector3(-.345f,1.03f,.09f),new Vector3(.038f,.13f,.038f),Quaternion.Euler(10f,0f,-15f),violet,5);
        ValheimStaffVisualBuilder.FocusLight(root,"witchweave-light",centre+Vector3.up*.05f,new Color(.79f,.52f,1f),1.95f,.50f);
    }

    private static void BuildMaster(GameObject root,Material wood,Material leather,Material iron,Material silver,Material violet,Material pale)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"seidr",wood,leather,iron,.050f,1.65f,true);
        foreach(var y in new[]{-.34f,-.08f,.20f,.46f})
            ValheimStaffVisualBuilder.Band(root,"fate-band-"+y.ToString("0.00"),new Vector3(.007f*y,y,.004f),.076f,.045f,silver,10);
        var basePoint=new Vector3(.018f,.52f,.005f);
        var left=new Vector3(-.19f,.72f,.05f);
        var right=new Vector3(.19f,.70f,-.035f);
        ValheimStaffVisualBuilder.Segment(root,"loom-left",basePoint,left,.028f,iron,8);
        ValheimStaffVisualBuilder.Segment(root,"loom-right",basePoint,right,.027f,silver,8);
        ValheimStaffVisualBuilder.Segment(root,"loom-left-spire",left,new Vector3(-.33f,1.16f,.075f),.016f,iron,7);
        ValheimStaffVisualBuilder.Segment(root,"loom-right-spire",right,new Vector3(.31f,1.11f,-.05f),.015f,silver,7);
        var centre=new Vector3(.018f,.96f,.005f);
        var nodes=new Vector3[8];
        for(var i=0;i<nodes.Length;i++)
        {
            var a=i*Mathf.PI*2f/nodes.Length+.21f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            nodes[i]=centre+radial*(i%3==0?.32f:.27f)+Vector3.up*(i%2==0?.16f:.10f);
            var lower=centre+radial*.14f+Vector3.down*.22f;
            ValheimStaffVisualBuilder.Segment(root,"fate-loom-arm-"+i,lower,nodes[i],.011f,i%2==0?silver:iron,6);
            if(i%2==0) ValheimStaffVisualBuilder.Shard(root,"fate-node-"+i,nodes[i],new Vector3(.040f,.14f,.040f),Quaternion.Euler(12f*Mathf.Sin(a),a*Mathf.Rad2Deg,16f*Mathf.Cos(a)),violet,5);
        }
        for(var i=0;i<4;i++)
            ValheimStaffVisualBuilder.Segment(root,"fate-thread-"+i,nodes[i],nodes[(i+3)%nodes.Length],.007f,silver,6);
        ValheimStaffVisualBuilder.Shard(root,"fate-core",centre,new Vector3(.29f,.62f,.29f),Quaternion.Euler(-4f,28f,3f),pale,8);
        ValheimStaffVisualBuilder.Shard(root,"omen-heart",centre+new Vector3(.032f,.13f,-.018f),new Vector3(.10f,.26f,.10f),Quaternion.Euler(8f,-15f,-5f),violet,6);
        ValheimStaffVisualBuilder.Segment(root,"hanging-fate-thread",new Vector3(.23f,.88f,-.07f),new Vector3(.31f,.69f,-.10f),.007f,silver,6);
        ValheimStaffVisualBuilder.Shard(root,"hanging-omen",new Vector3(.315f,.65f,-.10f),new Vector3(.036f,.12f,.036f),Quaternion.Euler(170f,0f,-8f),violet,5);
        ValheimStaffVisualBuilder.FocusLight(root,"fate-light",centre+Vector3.up*.07f,new Color(.86f,.64f,1f),2.30f,.62f);
    }
}
