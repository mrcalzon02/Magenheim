using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the unique boss chassis without mutating the vanilla source prefab.</summary>
internal sealed class NowhereKingRegistrar : IDisposable
{
    internal const string PrefabName="Magenheim_NowhereKing";
    private readonly ManualLogSource _log; private bool _subscribed,_registered;
    internal NowhereKingRegistrar(ManualLogSource log)=>_log=log??throw new ArgumentNullException(nameof(log));
    internal void Register(){if(_subscribed||_registered)return;PrefabManager.OnVanillaPrefabsAvailable+=RegisterContent;_subscribed=true;}
    private void RegisterContent()
    {
        if(_registered)return;
        try
        {
            if(PrefabManager.Instance.GetPrefab(PrefabName)!=null){_registered=true;_log.LogWarning($"Skipped Nowhere King registration because prefab identity '{PrefabName}' is occupied; existing content was left untouched.");return;}
            var source=PrefabManager.Instance.GetPrefab("DvergerMage")??throw new InvalidOperationException("Nowhere King requires verified vanilla humanoid source 'DvergerMage'.");
            if(source.GetComponent<Character>()==null||source.GetComponent<BaseAI>()==null||source.GetComponent<ZNetView>()==null)throw new InvalidOperationException("Nowhere King source lacks required Character/BaseAI/ZNetView behavior.");
            var prefab=PrefabManager.Instance.CreateClonedPrefab(PrefabName,source)??throw new InvalidOperationException("Unable to clone Nowhere King host prefab.");prefab.name=PrefabName;
            var character=prefab.GetComponent<Character>();character.m_name="The Nowhere King";character.m_health=7200f;character.m_walkSpeed=Mathf.Max(character.m_walkSpeed,2.4f);character.m_runSpeed=Mathf.Max(character.m_runSpeed,5.4f);prefab.transform.localScale=Vector3.one*1.82f;
            // The Dverger is only a networked humanoid/animation chassis. Its autonomous spell AI must never run beside authored King combat.
            foreach(var ai in prefab.GetComponents<BaseAI>())ai.enabled=false;
            NowhereKingVisuals.Apply(prefab);prefab.AddComponent<NowhereKingArenaLeash>();PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab,true));_registered=true;_log.LogInfo($"Registered unique boss prefab '{PrefabName}' with inherited Dverger AI disabled.");
        }
        catch(Exception exception){_log.LogError($"Nowhere King registration failed: {exception}");throw;}
        finally{Dispose();}
    }
    public void Dispose(){if(!_subscribed)return;PrefabManager.OnVanillaPrefabsAvailable-=RegisterContent;_subscribed=false;}
}

internal static class NowhereKingVisuals
{
    internal static void Apply(GameObject prefab)
    {
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)){var source=renderer.sharedMaterial;if(source==null)continue;var material=new Material(source){name="Magenheim_NowhereKing_Armor"};if(material.HasProperty("_Color"))material.SetColor("_Color",new Color(.018f,.02f,.027f,1f));if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",.82f);renderer.sharedMaterial=material;}
        var crown=GameObject.CreatePrimitive(PrimitiveType.Cylinder);crown.name="NowhereKing_BrokenCrown";crown.transform.SetParent(prefab.transform,false);crown.transform.localPosition=new Vector3(0f,2.05f,0f);crown.transform.localScale=new Vector3(.48f,.12f,.48f);UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());var light=crown.AddComponent<Light>();light.color=new Color(.32f,0f,.02f);light.range=3.5f;light.intensity=1.15f;
    }
}
