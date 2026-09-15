using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Server-owned 70%-35% spatial-control phase.</summary>
internal sealed class NowhereKingPhaseTwoCombat : MonoBehaviour
{
    private const float Upper=.70f,Lower=.35f;
    private Character _king = null!; private ZNetView _view = null!; private Vector3 _center; private bool _configured; private float _next; private int _sequence;
    internal void Configure(Vector3 center){_center=center;_configured=true;}
    internal void ResetState(){_next=0f;_sequence=0;}
    private void Awake(){_king=GetComponent<Character>();_view=GetComponent<ZNetView>();}
    private bool EncounterEngaged(){var link=GetComponent<NowhereKingEncounterLink>();return link!=null&&link.IsEncounterEngaged;}
    private bool Staggered(){var stagger=GetComponent<NowhereKingRoyalStagger>();return stagger!=null&&stagger.IsOpening;}
    private void Update(){if(!_configured||!EncounterEngaged()||Staggered()||_king==null||_king.IsDead()||_view==null||!_view.IsValid()||!_view.IsOwner()||Time.time<_next)return;var hp=_king.GetHealthPercentage();if(hp>Upper||hp<=Lower)return;var target=Target();if(target==null)return;switch((_sequence++)%4){case 0:StepBetween(target);break;case 1:SeverTheWorld(target);break;case 2:EmptyThrone(target);break;default:KingsGrasp(target);break;}}
    private Player? Target(){Player? best=null;var bestSq=float.MaxValue;foreach(var p in Player.GetAllPlayers()){if(p==null||p.IsDead())continue;var a=p.transform.position-_center;if(Mathf.Abs(a.x)>34f||Mathf.Abs(a.z)>38f)continue;var sq=(p.transform.position-transform.position).sqrMagnitude;if(sq<bestSq){best=p;bestSq=sq;}}return best;}
    private void StepBetween(Player target){var away=transform.position-target.transform.position;away.y=0f;if(away.sqrMagnitude<.1f)away=transform.forward;away.Normalize();var side=Vector3.Cross(Vector3.up,away)*((_sequence&1)==0?1f:-1f);MoveLegal(target.transform.position+away*5.5f+side*3f);Strike(target,68f,26f,1f);_next=Time.time+2.8f;}
    private void SeverTheWorld(Player target){var origin=transform.position;var forward=target.transform.position-origin;forward.y=0f;if(forward.sqrMagnitude<.1f){_next=Time.time+.5f;return;}forward.Normalize();foreach(var p in Player.GetAllPlayers()){if(p==null||p.IsDead())continue;var offset=p.transform.position-origin;offset.y=0f;var along=Vector3.Dot(offset,forward);if(along<0f||along>18f)continue;var lateral=(offset-forward*along).magnitude;if(lateral<=1.65f)Strike(p,105f,20f,1.35f);}_next=Time.time+3.7f;}
    private void EmptyThrone(Player target){var radial=target.transform.position-_center;radial.y=0f;if(radial.sqrMagnitude<.1f)radial=Vector3.forward;radial.Normalize();MoveLegal(_center-radial*17f);_next=Time.time+2.4f;}
    private void KingsGrasp(Player target){var delta=transform.position-target.transform.position;delta.y=0f;var distance=delta.magnitude;if(distance>14f){_next=Time.time+1f;return;}if(distance>.1f){var destination=transform.position-delta.normalized*3.2f;var local=destination-_center;local.x=Mathf.Clamp(local.x,-25f,25f);local.z=Mathf.Clamp(local.z,-29f,29f);target.transform.position=_center+local;}Strike(target,54f,42f,1.45f);_next=Time.time+4.4f;}
    private void MoveLegal(Vector3 requested){var local=requested-_center;local.x=Mathf.Clamp(local.x,-24f,24f);local.z=Mathf.Clamp(local.z,-28f,28f);_king.transform.position=_center+local;var body=GetComponent<Rigidbody>();if(body!=null)body.linearVelocity=Vector3.zero;}
    private void Strike(Character target,float slash,float blunt,float stagger){if(target==null||target.IsDead())return;var hit=new HitData();hit.m_point=target.GetCenterPoint();hit.m_dir=(target.transform.position-transform.position).normalized;hit.m_damage.m_slash=slash;hit.m_damage.m_blunt=blunt;hit.m_staggerMultiplier=stagger;hit.SetAttacker(_king);target.Damage(hit);}
}
