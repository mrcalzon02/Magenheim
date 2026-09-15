using System;
using System.Collections.Generic;
using System.Linq;
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
    private static readonly Mesh Box = MakeBox();
    private static readonly Dictionary<int, Mesh> Cylinders = new();
    private static readonly Dictionary<int, Mesh> Prisms = new();

    internal static void Apply(GameObject prefab, string assetName)
    {
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(x => x.sharedMaterial).FirstOrDefault(x => x)
            ?? throw new InvalidOperationException($"No material source exists on Radiance staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + assetName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var wood = Mat(source,"wood",new Color(.24f,.17f,.12f,1f),0f,.13f);
        var ivory = Mat(source,"ivory",new Color(.78f,.72f,.58f,1f),.03f,.23f);
        var bronze = Mat(source,"bronze",new Color(.62f,.43f,.19f,1f),.46f,.28f);
        var gold = Mat(source,"gold",new Color(.90f,.65f,.18f,1f),.68f,.43f);
        var light = Mat(source,"radiance-light-crystal",new Color(1f,.91f,.48f,1f),.02f,.78f,.42f);
        var white = Mat(source,"radiance-white-crystal",new Color(1f,.99f,.86f,1f),.01f,.88f,.60f);

        if (assetName == "staff-radiance-simple")
        {
            Shaft(root,wood,bronze,.034f); Part(root,"cross",Box,new Vector3(0,.69f,0),new Vector3(.26f,.036f,.038f),Quaternion.identity,bronze);
            Prism(root,"spark",new Vector3(0,.88f,0),.075f,.24f,6,light);
        }
        else if (assetName == "staff-radiance-crystal")
        {
            Shaft(root,ivory,gold,.038f); Cylinder(root,"ring",new Vector3(0,.64f,0),.12f,.04f,16,gold);
            for (var i=0;i<4;i++){ var a=i*Mathf.PI/2f; Part(root,"ray"+i,Box,new Vector3(.13f*Mathf.Cos(a),.78f,.13f*Mathf.Sin(a)),new Vector3(.03f,.30f,.03f),Quaternion.Euler(0,0,i%2==0?11f:-11f),gold); }
            Prism(root,"focus",new Vector3(0,.90f,0),.095f,.42f,6,light);
        }
        else if (assetName == "staff-radiance-advanced")
        {
            Shaft(root,wood,gold,.041f); Cylinder(root,"hub",new Vector3(0,.76f,0),.095f,.065f,14,gold);
            for (var i=0;i<8;i++){ var a=i*Mathf.PI/4f; var p=new Vector3(.19f*Mathf.Cos(a),.83f,.19f*Mathf.Sin(a)); Part(root,"corona"+i,Box,p,new Vector3(.028f,.34f,.028f),Quaternion.Euler(0,0,14f*Mathf.Cos(a)),gold); Prism(root,"tip"+i,p+Vector3.up*.17f,.025f,.11f,5,i%2==0?light:white); }
            Prism(root,"core",new Vector3(0,.91f,0),.105f,.36f,6,white);
        }
        else if (assetName == "staff-radiance-master")
        {
            Shaft(root,ivory,gold,.044f); foreach(var y in new[]{-.46f,-.10f,.26f,.53f}) Cylinder(root,"band"+y,new Vector3(0,y,0),.064f,.042f,14,gold);
            for (var i=0;i<12;i++){ var a=i*Mathf.PI/6f; Part(root,"daybreak"+i,Box,new Vector3(.23f*Mathf.Cos(a),.82f,.23f*Mathf.Sin(a)),new Vector3(.026f,.38f,.026f),Quaternion.Euler(0,0,18f*Mathf.Cos(a)),gold); }
            Cylinder(root,"halo",new Vector3(0,.78f,0),.24f,.045f,20,gold); Prism(root,"core",new Vector3(0,.95f,0),.135f,.52f,8,white); Prism(root,"heart",new Vector3(0,1.08f,0),.07f,.22f,6,light);
        }
        else throw new InvalidOperationException($"Unknown Radiance geometry '{assetName}'.");

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void Shaft(GameObject root, Material body, Material metal, float radius)
    {
        Cylinder(root,"shaft",new Vector3(0,-.08f,0),radius,1.48f,10,body);
        Cylinder(root,"pommel",new Vector3(0,-.77f,0),radius*1.38f,.10f,12,metal);
        Cylinder(root,"neck",new Vector3(0,.53f,0),radius*1.38f,.06f,12,metal);
    }
    private static void Cylinder(GameObject root,string name,Vector3 pos,float radius,float height,int sides,Material material)
    {
        if(!Cylinders.TryGetValue(sides,out var mesh)){mesh=MakeCylinder(sides);Cylinders.Add(sides,mesh);} Part(root,name,mesh,pos,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,material);
    }
    private static void Prism(GameObject root,string name,Vector3 pos,float radius,float height,int sides,Material material)
    {
        if(!Prisms.TryGetValue(sides,out var mesh)){mesh=MakePrism(sides);Prisms.Add(sides,mesh);} Part(root,name,mesh,pos,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,material);
    }
    private static void Part(GameObject root,string name,Mesh mesh,Vector3 pos,Vector3 scale,Quaternion rot,Material mat)
    {
        var go=new GameObject(name){layer=root.layer}; go.transform.SetParent(root.transform,false); go.transform.localPosition=pos; go.transform.localRotation=rot; go.transform.localScale=scale; go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }
    private static Material Mat(Material source,string name,Color color,float metallic,float gloss,float emission=0f)
    {
        var m=new Material(source){name="magenheim.radiance."+name}; GeneratedSurfaceTextures.Apply(m,name); if(m.HasProperty("_Color"))m.SetColor("_Color",color); if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic); if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",gloss); if(m.HasProperty("_BumpMap"))m.SetTexture("_BumpMap",null); m.DisableKeyword("_NORMALMAP"); if(m.HasProperty("_EmissionColor")&&emission>0f){m.SetColor("_EmissionColor",color*emission);m.EnableKeyword("_EMISSION");} else m.DisableKeyword("_EMISSION"); m.SetOverrideTag("RenderType","Opaque"); if(m.HasProperty("_ZWrite"))m.SetFloat("_ZWrite",1f); m.renderQueue=2000; return m;
    }
    private static Mesh MakeBox(){var m=new Mesh{name="magenheim.radiance.box"};m.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};m.triangles=new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2};m.RecalculateNormals();m.RecalculateBounds();return m;}
    private static Mesh MakeCylinder(int sides){var v=new List<Vector3>();var t=new List<int>();for(var r=0;r<2;r++){var y=r==0?-.5f:.5f;for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),y,.5f*Mathf.Sin(a)));}}var b=v.Count;v.Add(new Vector3(0,-.5f,0));var top=v.Count;v.Add(new Vector3(0,.5f,0));for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{i,sides+i,n,n,sides+i,sides+n,b,i,n,top,sides+n,sides+i});}var m=new Mesh{name="magenheim.radiance.cylinder."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
    private static Mesh MakePrism(int sides){var v=new List<Vector3>();var t=new List<int>();for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.34f*Mathf.Cos(a),-.45f,.34f*Mathf.Sin(a)));}for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),.18f,.5f*Mathf.Sin(a)));}var top=v.Count;v.Add(new Vector3(0,.68f,0));var bottom=v.Count;v.Add(new Vector3(0,-.5f,0));for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{bottom,i,n,i,sides+i,n,n,sides+i,sides+n,top,sides+n,sides+i});}var m=new Mesh{name="magenheim.radiance.prism."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
}
