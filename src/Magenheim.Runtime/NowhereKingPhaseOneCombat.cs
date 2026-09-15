using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Server-owned opening combat language for the Nowhere King.</summary>
internal sealed class NowhereKingPhaseOneCombat : MonoBehaviour
{
    private const float RoyalCombinationRange=5.2f,CrownBreakerRange=4.4f,KingsReachRange=10.5f,RoyalAdvanceTrigger=13f,PhaseOneFloor=.70f;
    private Character _character = null!; private ZNetView _view = null!; private float _nextAction; private int _sequence; private Vector3 _arenaCenter; private bool _configured;
    internal void Configure(Vector3 arenaCenter){_arenaCenter=arenaCenter;_configured=true;}
    internal void ResetState(){_nextAction=0f;_sequence=0;}
    private void Awake(){_character=GetComponent<Character>();_view=GetComponent<ZNetView>();}
    private bool EncounterEngaged(){var link=GetComponent<NowhereKingEncounterLink>();return link!=null&&link.IsEncounterEngaged;}
    private bool Staggered(){var stagger=GetComponent<NowhereKingRoyalStagger>();return stagger!=null&&stagger.IsOpening;}
    private void Update(){if(!_configured||!EncounterEngaged()||Staggered()||_character==null||_character.IsDead()||_view==null||!_view.IsValid()||!_view.IsOwner())return;if(_character.GetHealthPercentage()<PhaseOneFloor||Time.time<_nextAction)return;var target=SelectTarget();if(target==null)return;var delta=target.transform.position-transform.position;var planar=new Vector3(delta.x,0f,delta.z);if(planar.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(planar),Time.deltaTime*5f);var distance=planar.magnitude;if(distance>=RoyalAdvanceTrigger)RoyalAdvance(target);else if(distance<=CrownBreakerRange&&(_sequence++%3)==2)CrownBreaker(target);else if(distance<=RoyalCombinationRange)RoyalCombination(target);else if(distance<=KingsReachRange)KingsReach(target);else RoyalAdvance(target);}
    private Player? SelectTarget(){Player? best=null;var bestSq=float.MaxValue;foreach(var player in Player.GetAllPlayers()){if(player==null||player.IsDead())continue;var d=player.transform.position-_arenaCenter;if(Mathf.Abs(d.x)>34f||Mathf.Abs(d.z)>38f)continue;var sq=(player.transform.position-transform.position).sqrMagnitude;if(sq<bestSq){best=player;bestSq=sq;}}return best;}
    private void RoyalCombination(Player target){Strike(target,72f,34f,1.1f);_nextAction=Time.time+1.35f;}
    private void CrownBreaker(Player target){Strike(target,118f,62f,1.8f);_nextAction=Time.time+2.7f;}
    private void KingsReach(Player target){var origin=transform.position+Vector3.up*1.4f;var to=target.transform.position+Vector3.up-origin;if(to.magnitude>KingsReachRange){_nextAction=Time.time+.35f;return;}Strike(target,84f,42f,1.25f);_nextAction=Time.time+2.05f;}
    private void RoyalAdvance(Player target){var direction=target.transform.position-transform.position;direction.y=0f;if(direction.sqrMagnitude<.01f){_nextAction=Time.time+.5f;return;}direction.Normalize();var requested=transform.position+direction*Mathf.Min(8f,Vector3.Distance(transform.position,target.transform.position)-3.2f);var local=requested-_arenaCenter;local.x=Mathf.Clamp(local.x,-24f,24f);local.z=Mathf.Clamp(local.z,-28f,28f);var destination=_arenaCenter+local;_character.transform.position=destination;var body=GetComponent<Rigidbody>();if(body!=null)body.linearVelocity=Vector3.zero;if(Vector3.Distance(destination,target.transform.position)<=RoyalCombinationRange+1f)Strike(target,64f,28f,.9f);_nextAction=Time.time+3.8f;}
    private void Strike(Character target,float slash,float blunt,float stagger){if(target==null||target.IsDead())return;var hit=new HitData();hit.m_point=target.GetCenterPoint();hit.m_dir=(target.transform.position-transform.position).normalized;hit.m_damage.m_slash=slash;hit.m_damage.m_blunt=blunt;hit.m_staggerMultiplier=stagger;hit.SetAttacker(_character);target.Damage(hit);}
}
