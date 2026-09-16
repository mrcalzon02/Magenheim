using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;
using UnityEngine;
namespace Magenheim.Runtime;
using Magenheim.Core.DarkThrone;
internal static class DarkThroneVisuals {
    internal const string EncounterAnchorName="Magenheim_DarkThrone_EncounterAnchor",KingAnchorName="Magenheim_DarkThrone_KingAnchor",DaisRootName="Magenheim_DarkThrone_Dais";
    internal static void Build(GameObject locationContainer) {
        var model=ModelAssets.Load(locationContainer,"dark-throne",hideOriginal:false);
        var dais=new GameObject(DaisRootName);dais.transform.SetParent(locationContainer.transform,false);
        foreach(var part in model.GetComponentsInChildren<Transform>(true))if(part!=model.transform && (part.name.StartsWith("Dais_",StringComparison.Ordinal)||part.name.StartsWith("Throne_",StringComparison.Ordinal)||part.name.StartsWith("Rune_",StringComparison.Ordinal)))part.SetParent(dais.transform,false);
        var encounter=BuildEncounterAuthority(locationContainer.transform);dais.AddComponent<DarkThroneDaisRuntime>().Bind(encounter);
        Anchor(locationContainer.transform,KingAnchorName,new Vector3(0,1.2f,13.5f));BuildCrystalEcology(locationContainer.transform);
    }
private static DarkThroneEncounterRuntime BuildEncounterAuthority(Transform parent){var anchor=new GameObject(EncounterAnchorName);anchor.transform.SetParent(parent,false);anchor.transform.localPosition=Vector3.zero;anchor.AddComponent<ZNetView>();return anchor.AddComponent<DarkThroneEncounterRuntime>();}
private static void BuildCrystalEcology(Transform parent){var lesser=CrystalCreatureSpawnProfiles.DarkThroneLesser;var guardian=CrystalCreatureSpawnProfiles.DarkThroneGuardian;DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Lesser_West",new Vector3(-18f,.2f,-13f),lesser);DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Lesser_East",new Vector3(18f,.2f,-13f),lesser);DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Lesser_Approach",new Vector3(0f,.2f,-22f),lesser);DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Guardian_West",new Vector3(-13f,.8f,12f),guardian);DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Guardian_East",new Vector3(13f,.8f,12f),guardian);}
private static void Anchor(Transform parent,string name,Vector3 position){var anchor=new GameObject(name);anchor.transform.SetParent(parent,false);anchor.transform.localPosition=position;}
}
