using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Single interaction authority for the throne/dais monument. The physical throne and its
/// blackstone/metal rune dais are one gameplay object even though presentation uses child meshes.
/// Optional world-instance integrations attach through the destination hook rather than taking
/// ownership of the encounter or replacing the monument.
/// </summary>
internal sealed class DarkThroneDaisRuntime : MonoBehaviour, Hoverable, Interactable
{
    // Valheim 1.0 vanilla prefab: display name "Malicious Blood".
    internal const string MaliciousBloodPrefabName = "HatefulBlood";
    private DarkThroneEncounterRuntime _encounter;

    internal void Bind(DarkThroneEncounterRuntime encounter)=>_encounter=encounter;

    public string GetHoverName()=>"Dark Throne";

    public string GetHoverText()
    {
        if(_encounter==null)return "Dark Throne";
        if(_encounter.IsKingActive)return "Dark Throne\nThe throne is occupied.";
        if(_encounter.HasBeenDefeated)
            return UnderworldCompatibility.IsAvailable
                ? "Dark Throne\n[Use] Awaken the runes\n[Use + Malicious Blood] Summon the Nowhere King"
                : "Dark Throne\n[Use + Malicious Blood] Summon the Nowhere King";
        return "Dark Throne\nThe blackstone runes are dormant.";
    }

    public bool Interact(Humanoid user,bool hold,bool alt)
    {
        if(hold||user==null||_encounter==null||_encounter.IsKingActive)return false;
        if(_encounter.HasBeenDefeated&&TryConsumeMaliciousBlood(user))return _encounter.TryResummonKing();
        if(_encounter.HasBeenDefeated&&UnderworldCompatibility.IsAvailable)return UnderworldCompatibility.TryEnter(user,transform);
        return false;
    }

    public bool UseItem(Humanoid user,ItemDrop.ItemData item)
    {
        if(user==null||item==null||_encounter==null||_encounter.IsKingActive||!_encounter.HasBeenDefeated)return false;
        if(item.m_dropPrefab==null||item.m_dropPrefab.name!=MaliciousBloodPrefabName)return false;
        if(!_encounter.TryResummonKing())return false;
        user.GetInventory().RemoveItem(item,1);
        return true;
    }

    private static bool TryConsumeMaliciousBlood(Humanoid user)
    {
        var inventory=user.GetInventory();
        if(inventory==null||inventory.CountItems(MaliciousBloodPrefabName)<1)return false;
        inventory.RemoveItem(MaliciousBloodPrefabName,1);
        return true;
    }
}

/// <summary>
/// Additive compatibility boundary. Magenheim never hard-depends on Underworld; an integration
/// module can register a transport delegate when that mod is present. With no provider the dais
/// remains a fully functional boss monument and resummon altar.
/// </summary>
internal static class UnderworldCompatibility
{
    internal delegate bool TransportHandler(Humanoid user,Transform dais);
    private static TransportHandler _transport;
    internal static bool IsAvailable=>_transport!=null;
    internal static void Register(TransportHandler transport)=>_transport=transport;
    internal static void Unregister(TransportHandler transport){if(_transport==transport)_transport=null;}
    internal static bool TryEnter(Humanoid user,Transform dais)=>_transport!=null&&_transport(user,dais);
}
