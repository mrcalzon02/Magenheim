using System;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class DarkThroneEncounterRuntime : MonoBehaviour
{
    private const string DefeatedKey="magenheim.darkthrone.defeated",EngagedKey="magenheim.darkthrone.engaged",RewardedKey="magenheim.darkthrone.rewarded",KingIdentityKey="magenheim.darkthrone.king.id",KingEncounterKey="magenheim.darkthrone.king.encounter",EncounterIdentityKey="magenheim.darkthrone.id";
    private const float DisengageGraceSeconds=12f; private ZNetView _view = null!; private Transform? _kingAnchor; private float _noParticipantsSince=-1f;
    private const string RewardSlotsKey = "magenheim.darkthrone.reward.slots";
    internal string EncounterIdentity => _view != null && _view.IsValid()
        ? _view.GetZDO().GetString(EncounterIdentityKey, string.Empty) : string.Empty;
    internal bool HasBeenDefeated=>_view!=null&&_view.IsValid()&&_view.GetZDO().GetBool(DefeatedKey,false);
    internal bool IsEngaged=>_view!=null&&_view.IsValid()&&!_view.GetZDO().GetBool(DefeatedKey,false)&&_view.GetZDO().GetBool(EngagedKey,false);
    internal bool IsKingActive{get{var king=FindEncounterKing();return king!=null&&!king.IsDead();}}
    private void Awake()=>_view=GetComponent<ZNetView>();
    private void Start(){_kingAnchor=transform.parent!=null?transform.parent.Find(DarkThroneVisuals.KingAnchorName):null;if(_kingAnchor==null){Debug.LogError("Dark Throne encounter has no Nowhere King anchor and will remain inert.");enabled=false;}}
    private void Update()
    {
        if (!HasAuthority()) return;
        if (!ValheimWorldInstanceExecution.TryGetContextForScene(gameObject.scene.handle, out var context) || context is null) return;
        using (ValheimWorldInstanceExecution.Enter(context)) UpdateEncounter();
    }
    private void UpdateEncounter(){if(!HasAuthority())return;var zdo=_view.GetZDO();if(zdo.GetBool(DefeatedKey,false)){UnderworldProgressionAuthority.UnlockFromNowhereKingVictory();SuspendEcology(false);TryReleaseRewards(zdo);return;}var king=FindEncounterKing();if(king!=null&&king.IsDead()){var deadView=king.GetComponent<ZNetView>();if(deadView!=null&&deadView.IsValid())MarkDefeated(deadView.GetZDO());return;}if(king==null){if(!UnderworldDeepstoneRuntimeAuthority.CanChallengeNowhereKing)return;king=SpawnKing();if(king==null)return;}BindRuntime(king);var participants=CountLivingParticipants();var engaged=zdo.GetBool(EngagedKey,false);if(participants>0){_noParticipantsSince=-1f;if(!engaged){zdo.Set(EngagedKey,true);SuspendEcology(true);}return;}if(!engaged)return;if(_noParticipantsSince<0f)_noParticipantsSince=Time.time;if(Time.time-_noParticipantsSince<DisengageGraceSeconds)return;ResetEncounter(king);}
    internal void MarkDefeated(ZDO kingZdo){if(!HasAuthority()||kingZdo==null)return;var encounterId=EnsureEncounterIdentity();if(!string.Equals(kingZdo.GetString(KingEncounterKey,string.Empty),encounterId,StringComparison.Ordinal))return;var zdo=_view.GetZDO();if(zdo.GetBool(DefeatedKey,false))return;zdo.Set(DefeatedKey,true);zdo.Set(EngagedKey,false);zdo.Set(KingIdentityKey,string.Empty);zdo.Set(RewardedKey,false);zdo.Set(RewardSlotsKey,0);UnderworldProgressionAuthority.UnlockFromNowhereKingVictory();_noParticipantsSince=-1f;SuspendEcology(false);TryReleaseRewards(zdo);}
    internal bool TryResummonKing()
    {
        if (!HasAuthority()) return false;
        var zdo = _view.GetZDO();
        if (!zdo.GetBool(DefeatedKey, false) || !zdo.GetBool(RewardedKey, false) ||
            IsKingActive || HasPersistedKing()) return false;
        // Commit the new encounter only after native creation succeeds. A failed summon
        // leaves the completed encounter intact, allowing the dais to restore its offering.
        Character? king;
        try { king = SpawnKing(); }
        catch
        {
            king = FindEncounterKing();
            if (king == null || king.IsDead()) throw;
        }
        if (king == null) return false;
        zdo.Set(DefeatedKey, false);
        zdo.Set(EngagedKey, false);
        _noParticipantsSince = -1f;
        SuspendEcology(false);
        return true;
    }
    private bool HasAuthority()=>_view!=null&&_view.IsValid()&&_view.IsOwner()&&ZNet.instance!=null&&ZNet.instance.IsServer();
    private int CountLivingParticipants(){var center=transform.position;var count=0;foreach(var player in Player.GetAllPlayers()){if(player==null||player.IsDead()||player.gameObject.scene.handle!=gameObject.scene.handle)continue;var d=player.transform.position-center;if(Mathf.Abs(d.x)<=34f&&Mathf.Abs(d.z)<=38f)count++;}return count;}
    private Character? FindEncounterKing(){if(_view==null||!_view.IsValid())return null;var encounterId=EnsureEncounterIdentity();foreach(var character in Character.GetAllCharacters()){if(character==null||character.gameObject.scene.handle!=gameObject.scene.handle)continue;var nview=character.GetComponent<ZNetView>();if(nview!=null&&nview.IsValid()&&string.Equals(nview.GetZDO().GetString(KingEncounterKey,string.Empty),encounterId,StringComparison.Ordinal))return character;}return null;}
    private Character? SpawnKing(){if(HasPersistedKing())return null;var prefab=PrefabManager.Instance.GetPrefab(NowhereKingRegistrar.PrefabName);if(prefab==null||_kingAnchor==null)return null;var instance=Instantiate(prefab,_kingAnchor.position,_kingAnchor.rotation);var character=instance.GetComponent<Character>();var nview=instance.GetComponent<ZNetView>();if(character==null||nview==null||!nview.IsValid()){Destroy(instance);return null;}nview.GetZDO().Set(KingEncounterKey,EnsureEncounterIdentity());_view.GetZDO().Set(KingIdentityKey,nview.GetZDO().m_uid.ToString());BindRuntime(character);return character;}
    private bool HasPersistedKing()
    {
        var identity = _view.GetZDO().GetString(KingIdentityKey, string.Empty);
        if (string.IsNullOrEmpty(identity)) return false;
        var fields = identity.Split(':');
        // The installed game's ZDOID.ToString serializes the long user ID and uint object ID.
        // An unloaded native ZDO remains the living encounter; do not duplicate it while its
        // GameObject is awaiting sector streaming or ownership reconstruction.
        if (fields.Length != 2 ||
            !long.TryParse(fields[0], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var owner) ||
            !uint.TryParse(fields[1], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var objectId)) return true;
        return ZDOMan.instance == null || ZDOMan.instance.GetZDO(new ZDOID(owner, objectId)) != null;
    }
    private Vector3 CombatCenter(){var root=transform.parent;return root!=null?root.TransformPoint(DarkThroneVisuals.CombatCenterLocal):transform.position+DarkThroneVisuals.CombatCenterLocal;}
    private void BindRuntime(Character king){var center=CombatCenter();var leash=king.GetComponent<NowhereKingArenaLeash>();if(leash!=null)leash.Configure(center);var link=king.GetComponent<NowhereKingEncounterLink>()??king.gameObject.AddComponent<NowhereKingEncounterLink>();link.Bind(this);var p1=king.GetComponent<NowhereKingPhaseOneCombat>()??king.gameObject.AddComponent<NowhereKingPhaseOneCombat>();p1.Configure(center);var p2=king.GetComponent<NowhereKingPhaseTwoCombat>()??king.gameObject.AddComponent<NowhereKingPhaseTwoCombat>();p2.Configure(center);var p3=king.GetComponent<NowhereKingPhaseThreeCombat>()??king.gameObject.AddComponent<NowhereKingPhaseThreeCombat>();p3.Configure(center);if(king.GetComponent<NowhereKingPhaseTransition>()==null)king.gameObject.AddComponent<NowhereKingPhaseTransition>();if(king.GetComponent<NowhereKingAdaptiveResistance>()==null)king.gameObject.AddComponent<NowhereKingAdaptiveResistance>();if(king.GetComponent<NowhereKingRoyalStagger>()==null)king.gameObject.AddComponent<NowhereKingRoyalStagger>();}
    private string EnsureEncounterIdentity(){var zdo=_view.GetZDO();var id=zdo.GetString(EncounterIdentityKey,string.Empty);if(!string.IsNullOrWhiteSpace(id))return id;var p=transform.position;id=string.Concat("darkthrone:",Mathf.RoundToInt(p.x),":",Mathf.RoundToInt(p.y),":",Mathf.RoundToInt(p.z));zdo.Set(EncounterIdentityKey,id);return id;}
    private void ResetEncounter(Character king){var zdo=_view.GetZDO();zdo.Set(EngagedKey,false);_noParticipantsSince=-1f;SuspendEcology(false);if(king==null)return;var p1=king.GetComponent<NowhereKingPhaseOneCombat>();if(p1!=null)p1.ResetState();var p2=king.GetComponent<NowhereKingPhaseTwoCombat>();if(p2!=null)p2.ResetState();var p3=king.GetComponent<NowhereKingPhaseThreeCombat>();if(p3!=null)p3.ResetState();var transition=king.GetComponent<NowhereKingPhaseTransition>();if(transition!=null)transition.ResetState();king.Heal(king.GetMaxHealth(),true);if(_kingAnchor!=null)king.transform.position=_kingAnchor.position;var resistance=king.GetComponent<NowhereKingAdaptiveResistance>();if(resistance!=null)resistance.ResetAdaptation();var stagger=king.GetComponent<NowhereKingRoyalStagger>();if(stagger!=null)stagger.ResetState();var body=king.GetComponent<Rigidbody>();if(body!=null){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}}
    private void TryReleaseRewards(ZDO zdo)
    {
        if (zdo.GetBool(RewardedKey, false)) return;
        var rewards = new[]
        {
            NowhereKingRewardRegistrar.FirmamentPrefabName,
            NowhereKingRewardRegistrar.NullMantlePrefabName,
            NowhereKingRewardRegistrar.TrophyPrefabName,
            NowhereKingRewardRegistrar.NullGatePrefabName,
        };
        var slots = zdo.GetInt(RewardSlotsKey, 0);
        var origin = _kingAnchor != null ? _kingAnchor.position : transform.position;
        for (var slot = 0; slot < rewards.Length; slot++)
        {
            var bit = 1 << slot;
            if ((slots & bit) != 0) continue;
            var prefab = PrefabManager.Instance.GetPrefab(rewards[slot]);
            if (!prefab) return;
            try
            {
                var reward = Instantiate(prefab, origin + new Vector3(-2.4f + slot * 1.6f, 1.1f, 0f), Quaternion.identity);
                var view = reward.GetComponent<ZNetView>();
                if (!view || !view.IsValid())
                {
                    Destroy(reward);
                    Debug.LogError("Dark Throne reward has no persistent native ZDO: " + rewards[slot]);
                    return;
                }
                slots |= bit;
                zdo.Set(RewardSlotsKey, slots);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Dark Throne reward release failed for {rewards[slot]}; completed slots remain durable: {exception}");
                return;
            }
        }
        zdo.Set(RewardedKey, true);
    }
    private void SuspendEcology(bool suspended){var root=transform.parent!=null?transform.parent:transform;foreach(var spawner in root.GetComponentsInChildren<DarkThroneCrystalSpawner>(true))spawner.SetEncounterSuspended(suspended);}
}
