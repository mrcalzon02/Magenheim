using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class CarrionCatacombsEncounterAuthority : MonoBehaviour
{
    private const string ClearedPrefix="magenheim.carrioncatacombs.cleared.";
    private const string HarvestedPrefix="magenheim.carrioncatacombs.harvested.";
    private ZNetView _view=null!;

    private void Awake()=>_view=GetComponent<ZNetView>();

    internal bool HasAuthority=>
        _view is not null&&
        _view.IsValid()&&
        _view.IsOwner()&&
        ZNet.instance is not null&&
        ZNet.instance.IsServer();

    internal bool IsCleared(string id)=>
        _view is not null&&_view.IsValid()&&
        _view.GetZDO().GetBool(ClearedPrefix+id,false);

    internal bool IsHarvested(string id)=>
        _view is not null&&_view.IsValid()&&
        _view.GetZDO().GetBool(HarvestedPrefix+id,false);

    internal void MarkCleared(string id)
    {
        if(HasAuthority&&!string.IsNullOrWhiteSpace(id))
            _view.GetZDO().Set(ClearedPrefix+id,true);
    }

    internal void MarkHarvested(string id)
    {
        if(HasAuthority&&!string.IsNullOrWhiteSpace(id))
            _view.GetZDO().Set(HarvestedPrefix+id,true);
    }
}

internal sealed class CarrionCatacombsEncounterDeathTracker : MonoBehaviour
{
    private Character _character=null!;
    private CarrionCatacombsEncounterAuthority _authority=null!;
    private string _identity=string.Empty;
    private bool _recorded;

    internal void Bind(
        Character character,
        CarrionCatacombsEncounterAuthority authority,
        string identity)
    {
        _character=character??throw new ArgumentNullException(nameof(character));
        _authority=authority??throw new ArgumentNullException(nameof(authority));
        _identity=identity??throw new ArgumentNullException(nameof(identity));
    }

    private void Update()
    {
        if(_recorded||_character is null||!_character.IsDead())return;
        _authority.MarkCleared(_identity);
        if(_authority.HasAuthority)_recorded=true;
    }
}

internal sealed class CarrionCatacombsResourceTracker : MonoBehaviour
{
    private CarrionCatacombsEncounterAuthority _authority=null!;
    private string _identity=string.Empty;

    internal void Bind(
        CarrionCatacombsEncounterAuthority authority,
        string identity)
    {
        _authority=authority??throw new ArgumentNullException(nameof(authority));
        _identity=identity??throw new ArgumentNullException(nameof(identity));
    }

    internal void RecordHarvest()
    {
        if(_authority is not null)_authority.MarkHarvested(_identity);
    }
}

[HarmonyPatch(typeof(Pickable),"RPC_Pick")]
internal static class CarrionCatacombsPickablePersistencePatch
{
    [HarmonyPrefix]
    private static void Prefix(Pickable __instance)=>
        __instance?.GetComponent<CarrionCatacombsResourceTracker>()?.RecordHarvest();
}

internal static class CarrionCatacombsEncounterSpawner
{
    private const string SpawnIdentityKey="magenheim.carrioncatacombs.spawn.id";
    private const string ResourceIdentityKey="magenheim.carrioncatacombs.resource.id";

    internal static void Populate(
        GameObject room,
        UnderworldDungeonRoomPlacement placement,
        UnderworldCarrionCatacombsRoomDefinition definition,
        CarrionCatacombsEncounterAuthority authority,
        int locationSeed)
    {
        if(room is null)throw new ArgumentNullException(nameof(room));
        if(placement is null)throw new ArgumentNullException(nameof(placement));
        if(definition is null)throw new ArgumentNullException(nameof(definition));
        if(authority is null)throw new ArgumentNullException(nameof(authority));
        if(!authority.HasAuthority)return;

        var roster=Roster(definition);
        for(var index=0;index<roster.Count;index++)
            SpawnCreature(
                room,placement,definition,authority,locationSeed,roster[index],index);

        if(definition.Room.Role==UnderworldDungeonRoomRole.Resource)
            PopulateResources(room,placement,definition,authority,locationSeed);
    }

    private static IReadOnlyList<string> Roster(
        UnderworldCarrionCatacombsRoomDefinition definition)
    {
        var suffix=definition.Room.Id.Substring(
            UnderworldGreatDecayCarrionCatacombsCatalog.RoomIdPrefix.Length);
        switch(suffix)
        {
            case "rotling-warrens":
                return new[]{"Rotling","Rotling","Rotling","Marrow Creeper"};
            case "spore-husk-cloister":
                return new[]{"Spore Husk","Spore Husk","Carrion Bloom"};
            case "graft-warden-hall":
                return new[]{"Graft Warden","Decay Hound","Decay Hound"};
            case "corpse-orchard-antechamber":
                return new[]{"Corpse Orchard","Graft Warden","Rotling"};
            case "miasma-nave":
                return new[]{"Carrion Bloom","Rotling","Marrow Creeper"};
            case "carrion-sluice":
                return new[]{"Marrow Creeper","Decay Hound"};
        }

        if(definition.Room.Role==UnderworldDungeonRoomRole.Hazard)
            return new[]{"Rotling","Marrow Creeper"};
        if(definition.Room.Role==UnderworldDungeonRoomRole.Encounter)
            return new[]{"Decay Hound","Spore Husk"};
        return Array.Empty<string>();
    }

    private static void SpawnCreature(
        GameObject room,
        UnderworldDungeonRoomPlacement placement,
        UnderworldCarrionCatacombsRoomDefinition definition,
        CarrionCatacombsEncounterAuthority authority,
        int locationSeed,
        string creatureName,
        int index)
    {
        var entry=UnderworldCreaturePrototypes.All.FirstOrDefault(x=>
                x.Name==creatureName&&x.Biome=="Great Decay")
            ??throw new InvalidOperationException(
                "Unknown Carrion Catacombs creature "+creatureName);
        var source=PrefabManager.Instance.GetPrefab(entry.Prefab)
            ??throw new InvalidOperationException(
                "Missing Carrion Catacombs creature prefab "+entry.Prefab);

        var identity=
            $"{unchecked((uint)locationSeed):X8}.{placement.InstanceId}.creature.{index+1:00}";
        if(authority.IsCleared(identity)||
           FindCreature(identity,room.scene.handle) is not null)
            return;

        var random=new System.Random(
            StableSeed(locationSeed,placement.InstanceId,index));
        var x=((float)random.NextDouble()-.5f)*
              (float)definition.Room.WidthMeters*.48f;
        var z=((float)random.NextDouble()-.5f)*
              (float)definition.Room.DepthMeters*.48f;
        var instance=UnityEngine.Object.Instantiate(
            source,
            room.transform.TransformPoint(new Vector3(x,.65f,z)),
            Quaternion.Euler(0f,(float)random.NextDouble()*360f,0f));

        var character=instance.GetComponent<Character>();
        var view=instance.GetComponent<ZNetView>();
        if(character is null||view is null||!view.IsValid())
        {
            UnityEngine.Object.Destroy(instance);
            throw new InvalidOperationException(
                "Carrion Catacombs creature lacks Character/ZNetView authority.");
        }

        view.GetZDO().Set(SpawnIdentityKey,identity);
        (instance.GetComponent<CarrionCatacombsEncounterDeathTracker>()??
         instance.AddComponent<CarrionCatacombsEncounterDeathTracker>())
            .Bind(character,authority,identity);
        instance.SetActive(true);
    }

    private static Character? FindCreature(string identity,int sceneHandle)
    {
        foreach(var character in Character.GetAllCharacters())
        {
            if(character is null||
               character.IsDead()||
               character.gameObject.scene.handle!=sceneHandle)
                continue;
            var view=character.GetComponent<ZNetView>();
            if(view is not null&&
               view.IsValid()&&
               view.GetZDO().GetString(SpawnIdentityKey,string.Empty)==identity)
                return character;
        }
        return null;
    }

    private static void PopulateResources(
        GameObject room,
        UnderworldDungeonRoomPlacement placement,
        UnderworldCarrionCatacombsRoomDefinition definition,
        CarrionCatacombsEncounterAuthority authority,
        int locationSeed)
    {
        var resources=UnderworldResourceCatalog.All
            .Where(x=>x.Biome==UnderworldTerrainBiome.GreatDecay)
            .ToArray();
        if(resources.Length==0)
            throw new InvalidOperationException(
                "Carrion Catacombs have no Great Decay resource vocabulary.");

        for(var index=0;index<6;index++)
        {
            var resource=resources[index%resources.Length];
            var identity=
                $"{unchecked((uint)locationSeed):X8}.{placement.InstanceId}.resource.{index+1:00}";
            if(authority.IsHarvested(identity)||
               FindResource(identity,room.scene.handle) is not null)
                continue;

            var source=PrefabManager.Instance.GetPrefab(resource.PickupPrefab)
                ??throw new InvalidOperationException(
                    "Missing Carrion Catacombs resource prefab "+resource.PickupPrefab);
            var random=new System.Random(
                StableSeed(locationSeed^0xCA7710,placement.InstanceId,index));
            var x=((float)random.NextDouble()-.5f)*
                  (float)definition.Room.WidthMeters*.52f;
            var z=((float)random.NextDouble()-.5f)*
                  (float)definition.Room.DepthMeters*.52f;
            var instance=UnityEngine.Object.Instantiate(
                source,
                room.transform.TransformPoint(new Vector3(x,.25f,z)),
                Quaternion.Euler(0f,(float)random.NextDouble()*360f,0f));

            var view=instance.GetComponent<ZNetView>();
            var pickable=instance.GetComponent<Pickable>();
            if(view is null||!view.IsValid()||pickable is null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    "Carrion Catacombs resource lacks Pickable/ZNetView authority.");
            }

            view.GetZDO().Set(ResourceIdentityKey,identity);
            (instance.GetComponent<CarrionCatacombsResourceTracker>()??
             instance.AddComponent<CarrionCatacombsResourceTracker>())
                .Bind(authority,identity);
            instance.SetActive(true);
        }
    }

    private static Pickable? FindResource(string identity,int sceneHandle)
    {
        foreach(var pickable in Resources.FindObjectsOfTypeAll<Pickable>())
        {
            if(!pickable||pickable.gameObject.scene.handle!=sceneHandle)
                continue;
            var view=pickable.GetComponent<ZNetView>();
            if(view is not null&&
               view.IsValid()&&
               view.GetZDO().GetString(ResourceIdentityKey,string.Empty)==identity)
                return pickable;
        }
        return null;
    }

    private static int StableSeed(int seed,string identity,int index)
    {
        unchecked
        {
            uint hash=(uint)seed^(uint)(index*486187739);
            foreach(var c in identity)
            {
                hash^=c;
                hash*=16777619u;
            }
            return (int)hash;
        }
    }
}
