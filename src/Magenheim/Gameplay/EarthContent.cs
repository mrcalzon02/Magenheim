using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim
{
    internal static class EarthContent
    {
        internal const string Station = "Magenheim_GeologistWorkstation";
        internal const string Geode = "Magenheim_Geode_Meadows";
        internal const string Shard = "Magenheim_CrystalShard_Earth";
        internal static readonly string[] Tiers = { "Rough", "Simple", "Refined", "Advanced", "Master" };
        internal static string Crystal(int tier) { return "Magenheim_Crystal_Earth_" + Tiers[tier]; }
        internal static string Staff(int tier) { return "Magenheim_Staff_Earth_" + Tiers[tier]; }
        internal static ManualLogSource Log;

        internal static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
            try
            {
                AddItem(Geode, "Stone", "Meadows Geode", "An unbroken mineral nodule. Crack it at the Geologist's Workstation.", new Color(.35f,.43f,.29f), 20);
                AddItem(Shard, "Crystal", "Earth Crystal Shard", "Five matching shards can be recombined into a Simple Earth Crystal.", new Color(.3f,.6f,.18f), 100);
                for (int i = 0; i < Tiers.Length; i++)
                    AddItem(Crystal(i), "Crystal", Tiers[i] + " Earth Crystal", "Earth held in stone. Shape and bind it at the Geologist's Workstation.", new Color(.18f + i*.06f,.48f+i*.09f,.12f), 50);
                var station = new CustomPiece(Station, "piece_workbench", new PieceConfig {
                    Name = "Geologist's Workstation", Description = "Crack geodes, shape Earth crystals, bind weapons and craft Earth staves.",
                    PieceTable = "Hammer", Category = "Crafting", CraftingStation = "piece_workbench",
                    Requirements = new[] { Cost("Wood",10), Cost("Stone",8), Cost("Flint",4), Cost("HardAntler",1) }
                });
                station.PiecePrefab.GetComponent<CraftingStation>().m_name = "Geologist's Workstation";
                PieceManager.Instance.AddPiece(station);
                Decorate(station.PiecePrefab);
                Upgrade("FracturingBlock", "piece_workbench_ext1", "Fracturing Block", new[] {Cost("RoundLog",5),Cost("Bronze",2),Cost("Flint",8)});
                Upgrade("FacetingWheel", "piece_workbench_ext3", "Faceting Wheel", new[] {Cost("FineWood",10),Cost("Iron",3),Cost("SharpeningStone",1)});
                Upgrade("ResonanceFrame", "piece_workbench_ext4", "Resonance Frame", new[] {Cost("Silver",4),Cost("BlackMetal",2),Cost("Crystal",4),Cost("FineWood",8)});
                for(int i=1;i<=4;i++) AddStaff(i);
                RegisterGeode();
                Log.LogInfo("Earth content registered: 7 materials, workstation, 3 upgrades, 4 staves and Meadows world geode.");
            }
            catch(Exception ex) { Log.LogError("Earth content registration failed: " + ex); }
        }
        private static RequirementConfig Cost(string item, int count) { return new RequirementConfig(item, count, 0, true); }
        private static void Upgrade(string suffix, string source, string name, RequirementConfig[] costs)
        {
            var piece = new CustomPiece("Magenheim_StationUpgrade_" + suffix, source, new PieceConfig {
                Name=name, Description="Improves the Geologist's Workstation.", PieceTable="Hammer", Category="Crafting",
                CraftingStation="piece_workbench", ExtendStation=Station, Requirements=costs });
            PieceManager.Instance.AddPiece(piece);
            Tint(piece.PiecePrefab, new Color(.55f,.65f,.45f));
        }
        private static void AddItem(string name, string source, string display, string description, Color tint, int stack)
        {
            var item = new CustomItem(name, source, new ItemConfig {Name=display, Description=description, StackSize=stack, Weight=.5f});
            Tint(item.ItemPrefab,tint);
            ItemManager.Instance.AddItem(item);
        }
        internal static void Tint(GameObject root, Color color)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    if(!materials[i]) continue;
                    materials[i]=new Material(materials[i]);
                    if(materials[i].HasProperty("_Color")) materials[i].SetColor("_Color",color);
                }
                renderer.sharedMaterials=materials;
            }
        }
        private static void Decorate(GameObject root)
        {
            RockVisual(root.transform, new Vector3(0,.9f,0), new Vector3(.85f,.14f,.48f), new Color(.3f,.33f,.29f));
            for(int i=0;i<3;i++) RockVisual(root.transform,new Vector3(-.35f+i*.17f,1.1f,.05f),new Vector3(.06f,.15f+i*.035f,.06f),new Color(.2f,.7f,.22f));
        }
        private static void RockVisual(Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var visual=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name="Magenheim_mineral"; visual.transform.SetParent(parent,false);
            visual.transform.localPosition=position; visual.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            var source=PrefabManager.Instance.GetPrefab("Stone").GetComponentInChildren<Renderer>(true);
            visual.GetComponent<Renderer>().sharedMaterial=new Material(source.sharedMaterial);
            Tint(visual,color);
        }
        private static void AddStaff(int tier)
        {
            string[] descriptions={"","Stonebolt: a fast stone projectile.","Boulder: a heavy arcing shot with a small impact radius.","Stonefan: three spreading stone projectiles.","Earthshatter: a massive, slow boulder with a broad impact."};
            var item=new CustomItem(Staff(tier),"StaffFireball",new ItemConfig {Name=Tiers[tier]+" Earth Staff",Description=descriptions[tier],Weight=2,StackSize=1});
            var shared=item.ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_damages=new HitData.DamageTypes {m_blunt=20f*tier};
            shared.m_damagesPerLevel=new HitData.DamageTypes();
            shared.m_maxQuality=1;
            shared.m_attack=shared.m_attack.Clone();
            shared.m_attack.m_attackEitr=0; shared.m_attack.m_attackStamina=8+4*tier;
            shared.m_attack.m_projectiles=tier==3?3:1;
            shared.m_attack.m_projectileAccuracy=tier==3?12:0;
            shared.m_attack.m_projectileVel=tier==4?16:30;
            var projectile=PrefabManager.Instance.CreateClonedPrefab(Staff(tier)+"_Projectile",shared.m_attack.m_attackProjectile);
            var p=projectile.GetComponent<Projectile>();
            p.m_aoe=tier==1?0:tier==2?2:tier==3?0:5;
            p.m_gravity=tier==1?0:6; p.m_ttl=8; p.m_hitOwner=false; p.m_hitFriendly=false; p.m_noDamageFriendly=true;
            p.m_spawnOnHit=null; p.m_randomSpawnOnHit=new List<GameObject>();
            p.m_hitEffects=new EffectList(); p.m_spawnOnHitEffects=new EffectList();
            foreach(var r in projectile.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
            RockVisual(projectile.transform,Vector3.zero,Vector3.one*(tier==4?1.1f:.22f*tier),new Color(.32f,.4f,.24f));
            PrefabManager.Instance.AddPrefab(projectile);
            shared.m_attack.m_attackProjectile=projectile;
            Tint(item.ItemPrefab,new Color(.35f,.65f,.23f));
            ItemManager.Instance.AddItem(item);
        }
        private static void RegisterGeode()
        {
            var root=PrefabManager.Instance.CreateClonedPrefab("Magenheim_GeodeWorld_Meadows","Pickable_Flint");
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Pickable>());
            foreach(var r in root.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
            foreach(var c in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            root.transform.localScale=Vector3.one;
            var collider=root.AddComponent<SphereCollider>(); collider.center=new Vector3(0,.4f,0); collider.radius=.55f;
            RockVisual(root.transform,new Vector3(0,.4f,0),new Vector3(1.3f,.95f,1.1f),new Color(.32f,.37f,.27f));
            for(int i=0;i<4;i++) RockVisual(root.transform,new Vector3(-.35f+i*.22f,.8f,0),new Vector3(.07f,.22f,.12f),new Color(.15f,.6f,.2f));
            var d=root.AddComponent<Destructible>(); d.m_health=1; d.m_minToolTier=0; d.m_autoCreateFragments=false;
            d.m_hitEffect=new EffectList(); d.m_destroyedEffect=new EffectList();
            root.AddComponent<GeodeMarker>();
            var drops=root.AddComponent<DropOnDestroyed>();
            drops.m_dropWhenDestroyed=new DropTable {m_dropMin=1,m_dropMax=1,m_dropChance=1,m_drops=new List<DropTable.DropData> {
                new DropTable.DropData {m_item=PrefabManager.Instance.GetPrefab(Geode),m_stackMin=1,m_stackMax=1,m_weight=1} }};
            ZoneManager.Instance.AddCustomVegetation(new CustomVegetation(root,false,new VegetationConfig {
                Biome=Heightmap.Biome.Meadows,Min=1,Max=3,MinAltitude=1,MaxAltitude=1000,MaxTilt=25,
                GroupSizeMin=1,GroupSizeMax=1,ScaleMin=1,ScaleMax=1,BlockCheck=true }));
        }
    }
    public sealed class GeodeMarker : MonoBehaviour, Hoverable
    {
        public string GetHoverName() { return "Meadows Geode"; }
        public string GetHoverText() { return "Meadows Geode\nRequires a pickaxe"; }
        public float GetHoverOffset() { return .8f; }
    }
}
