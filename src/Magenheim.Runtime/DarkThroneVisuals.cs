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
        var encounter=BuildEncounterAuthority(locationContainer.transform);
        var interactionAnchor=Anchor(dais.transform,"Magenheim_DarkThrone_DaisInteraction",new Vector3(0f,13.15f,21.7f));
        dais.AddComponent<DarkThroneDaisRuntime>().Bind(encounter,interactionAnchor);
        Anchor(locationContainer.transform,KingAnchorName,new Vector3(0f,10.15f,15.5f));BuildCrystalEcology(locationContainer.transform);
    }
internal static readonly Vector3 CombatCenterLocal=new Vector3(0f,9.9f,0f);
private static DarkThroneEncounterRuntime BuildEncounterAuthority(Transform parent){var anchor=new GameObject(EncounterAnchorName);anchor.transform.SetParent(parent,false);anchor.transform.localPosition=Vector3.zero;anchor.AddComponent<ZNetView>();return anchor.AddComponent<DarkThroneEncounterRuntime>();}
private static void BuildCrystalEcology(Transform parent){var lesser=CrystalCreatureSpawnProfiles.DarkThroneLesser;var guardian=CrystalCreatureSpawnProfiles.DarkThroneGuardian;DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Lesser_West",new Vector3(-12f,5.9f,-2f),lesser);DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Lesser_East",new Vector3(12f,5.9f,-2f),lesser);DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Lesser_Approach",new Vector3(0f,2.0f,-18f),lesser);DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Guardian_West",new Vector3(-12f,10.1f,14f),guardian);DarkThroneCrystalSpawnerFactory.Create(parent,"CrystalSpawner_Guardian_East",new Vector3(12f,10.1f,14f),guardian);}
private static Transform Anchor(Transform parent,string name,Vector3 position){var anchor=new GameObject(name);anchor.transform.SetParent(parent,false);anchor.transform.localPosition=position;return anchor.transform;}
}
