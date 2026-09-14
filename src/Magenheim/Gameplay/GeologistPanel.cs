using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim
{
    public sealed class GeologistPanel : MonoBehaviour
    {
        internal static GeologistPanel Instance;
        internal static Skills.SkillType ShapingSkill;
        internal DefinitionSnapshot Data;
        private CraftingStation station;
        private string message="";
        private Vector2 scroll;
        private Rect window;
        private readonly System.Random random=new System.Random();
        internal void Open(CraftingStation value)
        {
            station=value; message="";
            window=new Rect(Mathf.Max(10,(Screen.width-570)/2),Mathf.Max(10,(Screen.height-640)/2),570,Mathf.Min(640,Screen.height-20));
            GUIManager.BlockInput(true);
        }
        private void Close() { station=null; GUIManager.BlockInput(false); }
        private void OnDestroy() { if(station) Close(); }
        private bool Ready()
        {
            var player=Player.m_localPlayer;
            return station && player && !player.IsDead() && Vector3.Distance(player.transform.position,station.transform.position)<4;
        }
        private void Update() { if(station && (!Ready() || Input.GetKeyDown(KeyCode.Escape))) Close(); }
        private void OnGUI()
        {
            if(!station) return;
            window=GUILayout.Window(194724,window,Draw,"Geologist's Workstation");
        }
        private void Draw(int id)
        {
            if(!Ready()) { Close(); return; }
            var player=Player.m_localPlayer;
            int level=station.GetLevel();
            float skill=player.GetSkillLevel(ShapingSkill);
            GUILayout.Label("EARTH • Station level "+level+" • Crystal Shaping "+skill.ToString("0.0"));
            GUILayout.Label("Magic begins as geology.");
            bool solo=ZNet.instance && ZNet.instance.IsServer() && ZNet.instance.GetPeerConnections()==0;
            if(!solo) GUILayout.Label("This prototype's workstation requires a solo world. Multiplayer transactions are not yet enabled.");
            scroll=GUILayout.BeginScrollView(scroll);
            GUI.enabled=solo;
            Button("Crack Meadows Geode (1)  →  1–3 Rough Earth Crystals", () => Crack());
            Button("Recombine "+Data.ShardsRequired+" Earth Shards  →  Simple Earth Crystal",()=>Recombine());
            GUILayout.Space(12); GUILayout.Label("SHAPING — failure returns Earth shards");
            for(int i=0;i<4;i++)
            {
                int tier=i;
                double chance=Refinement.FailureChance(Data.Steps[i].BaseFailure,skill,Data.MaximumFailureReduction);
                GUI.enabled=solo && level>=i+1;
                Button(EarthContent.Tiers[i]+" → "+EarthContent.Tiers[i+1]+"   |   "+(chance*100).ToString("0.##")+"% failure   |   Level "+(i+1),()=>Refine(tier));
            }
            GUILayout.Space(12); GUILayout.Label("EARTH STAVES — one matching crystal + 10 Wood");
            for(int i=1;i<=4;i++)
            {
                int tier=i; GUI.enabled=solo && level>=i;
                Button("Craft "+EarthContent.Tiers[i]+" Earth Staff   |   Level "+i,()=>CraftStaff(tier));
            }
            GUILayout.Space(12); GUILayout.Label("BIND A SIMPLE EARTH CRYSTAL — +5 blunt damage to this weapon");
            GUI.enabled=solo;
            foreach(var item in player.GetInventory().GetAllItems().ToArray())
            {
                if(!item.IsWeapon() || item.m_shared.m_itemType==ItemDrop.ItemData.ItemType.Tool || item.m_customData.Keys.Any(k=>k.StartsWith("magenheim.",StringComparison.Ordinal))) continue;
                var selected=item;
                Button("Bind: "+LocalizationManager.Instance.TryTranslate(item.m_shared.m_name),()=>Socket(selected));
            }
            GUI.enabled=true;
            GUILayout.EndScrollView();
            GUILayout.Label(message);
            if(GUILayout.Button("Close",GUILayout.Height(30))) Close();
            GUI.DragWindow(new Rect(0,0,570,25));
        }
        private void Button(string label, Action action)
        {
            if(GUILayout.Button(label,GUILayout.MinHeight(30)))
            {
                try { action(); }
                catch(Exception ex) { message=ex.Message; EarthContent.Log.LogWarning("Workstation: "+ex.Message); }
            }
        }
        private void Validate()
        {
            if(!Ready()) throw new InvalidOperationException("Move closer to the workstation.");
            if(!ZNet.instance.IsServer() || ZNet.instance.GetPeerConnections()!=0) throw new InvalidOperationException("Solo-world prototype only.");
        }
        private Inventory Inventory { get { return Player.m_localPlayer.GetInventory(); } }
        private int Count(string prefab) { return Inventory.GetAllItems().Where(i=>i.m_dropPrefab && i.m_dropPrefab.name==prefab).Sum(i=>i.m_stack); }
        private void Need(string prefab,int count)
        {
            if(Count(prefab)<count) throw new InvalidOperationException("Missing materials: "+prefab.Replace("Magenheim_","").Replace('_',' ')+" ×"+count);
        }
        private void Consume(string prefab,int count)
        {
            foreach(var item in Inventory.GetAllItems().Where(i=>i.m_dropPrefab && i.m_dropPrefab.name==prefab).ToArray())
            {
                int take=Math.Min(count,item.m_stack);
                if(!Inventory.RemoveItem(item,take)) throw new InvalidOperationException("Could not consume ingredient.");
                count-=take; if(count==0) return;
            }
            throw new InvalidOperationException("Ingredient disappeared.");
        }
        private void Give(string prefab,int count)
        {
            var item=ObjectDB.instance.GetItemPrefab(prefab);
            if(!item || !Inventory.AddItem(item,count)) throw new InvalidOperationException("Make room in your inventory.");
        }
        private void Commit(Dictionary<string,int> costs, Action outputs, float xp)
        {
            Validate();
            foreach(var cost in costs) Need(cost.Key,cost.Value);
            // Reserve enough room before drawing/committing. Restore exact serialized inventory if an operation fails.
            var backup=new ZPackage(); Inventory.Save(backup);
            try
            {
                foreach(var cost in costs) Consume(cost.Key,cost.Value);
                outputs();
            }
            catch { backup.SetPos(0); Inventory.Load(backup); throw; }
            if(xp>0) Player.m_localPlayer.RaiseSkill(ShapingSkill,xp);
        }
        private void Crack()
        {
            Validate(); Need(EarthContent.Geode,1);
            if(Inventory.GetEmptySlots()<3) throw new InvalidOperationException("Leave three empty inventory slots before cracking.");
            var result=CrystalCracking.Evaluate(Data,"meadows",station.GetLevel(),random.NextDouble);
            Commit(new Dictionary<string,int>{{EarthContent.Geode,1}},()=>Give(EarthContent.Crystal(0),result.Elements.Count),(float)result.Experience);
            message="Cracked geode: "+result.Elements.Count+" Rough Earth Crystal(s).";
        }
        private void Refine(int tier)
        {
            Validate(); Need(EarthContent.Crystal(tier),1);
            if(Inventory.GetEmptySlots()<1) throw new InvalidOperationException("Leave one empty inventory slot before shaping.");
            var result=Refinement.Evaluate(Data,"earth",EarthContent.Tiers[tier].ToLowerInvariant(),station.GetLevel(),Player.m_localPlayer.GetSkillLevel(ShapingSkill),random.NextDouble());
            Commit(new Dictionary<string,int>{{EarthContent.Crystal(tier),1}},()=>Give(result.Success?EarthContent.Crystal(tier+1):EarthContent.Shard,result.Success?1:result.Shards),(float)result.Experience);
            message=result.Success?"Shaping succeeded: "+EarthContent.Tiers[tier+1]+" Earth Crystal.":"Crystal shattered. Recovered "+result.Shards+" Earth Shards. Practice still grants experience.";
        }
        private void Recombine()
        {
            Validate();
            var result=ShardRecovery.Evaluate(Data,"earth",station.GetLevel(),Count(EarthContent.Shard));
            Commit(new Dictionary<string,int>{{EarthContent.Shard,result.ConsumedShards}},()=>Give(EarthContent.Crystal(1),1),0);
            message="Recombined shards into a Simple Earth Crystal.";
        }
        private void CraftStaff(int tier)
        {
            Validate(); if(station.GetLevel()<tier) throw new InvalidOperationException("Upgrade the workstation first.");
            Commit(new Dictionary<string,int>{{EarthContent.Crystal(tier),1},{"Wood",10}},()=>Give(EarthContent.Staff(tier),1),.5f);
            message="Crafted "+EarthContent.Tiers[tier]+" Earth Staff.";
        }
        private void Socket(ItemDrop.ItemData item)
        {
            Validate();
            if(!Inventory.GetAllItems().Contains(item) || !item.IsWeapon() || item.m_customData.Keys.Any(k=>k.StartsWith("magenheim.",StringComparison.Ordinal))) throw new InvalidOperationException("Weapon is no longer eligible.");
            Commit(new Dictionary<string,int>{{EarthContent.Crystal(1),1}},()=> {
                item.m_customData["magenheim.data_version"]="1";
                item.m_customData["magenheim.socket.count"]="1";
                item.m_customData["magenheim.socket.0.element"]="earth";
                item.m_customData["magenheim.socket.0.tier"]="simple";
                item.m_customData["magenheim.socket.0.definition_version"]="1";
            },.25f);
            message="Bound Simple Earth Crystal to this weapon. +5 blunt damage.";
        }
    }
    [HarmonyPatch(typeof(CraftingStation),"Interact")]
    internal static class StationInteraction
    {
        private static bool Prefix(CraftingStation __instance, bool hold, ref bool __result)
        {
            if(!__instance.name.StartsWith(EarthContent.Station,StringComparison.Ordinal)) return true;
            if(!hold && GeologistPanel.Instance) GeologistPanel.Instance.Open(__instance);
            __result=false; return false;
        }
    }
    [HarmonyPatch(typeof(Destructible),"Damage")]
    internal static class GeodeMining
    {
        private static bool Prefix(Destructible __instance,HitData hit)
        { return !__instance.GetComponent<GeodeMarker>() || hit.m_damage.m_pickaxe>0; }
    }
    [HarmonyPatch(typeof(ItemDrop.ItemData),"GetDamage",new Type[]{typeof(int),typeof(float)})]
    internal static class EarthSocketDamage
    {
        private static void Postfix(ItemDrop.ItemData __instance,ref HitData.DamageTypes __result)
        {
            string version,element,tier;
            if(__instance.m_customData.TryGetValue("magenheim.data_version",out version) && version=="1"
                && __instance.m_customData.TryGetValue("magenheim.socket.0.element",out element) && element=="earth"
                && __instance.m_customData.TryGetValue("magenheim.socket.0.tier",out tier) && tier=="simple") __result.m_blunt+=5;
        }
    }
}
