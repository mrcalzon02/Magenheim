using System.Collections;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Server-owned 35%-0% Broken Crown phase, including the telegraphed gravity inversion wave.</summary>
internal sealed class NowhereKingPhaseThreeCombat : MonoBehaviour
{
    private const float Upper=.35f,GravityRadius=27f,GravityWindup=2.25f;
    private Character _king = null!; private ZNetView _view = null!; private Vector3 _center; private bool _configured,_busy; private float _next; private int _sequence; private GameObject? _gravityTelegraph;
    internal void Configure(Vector3 center){_center=center;_configured=true;}
    internal void ResetState(){StopAllCoroutines();DestroyGravityTelegraph();_busy=false;_next=0f;_sequence=0;}
    private void Awake(){_king=GetComponent<Character>();_view=GetComponent<ZNetView>();}
    private bool Staggered(){var stagger=GetComponent<NowhereKingRoyalStagger>();return stagger!=null&&stagger.IsOpening;}
    private void Update(){if(!_configured||_busy||Staggered()||_king==null||_king.IsDead()||_view==null||!_view.IsValid()||!_view.IsOwner()||Time.time<_next||_king.GetHealthPercentage()>Upper)return;var target=Target();if(target==null)return;switch((_sequence++)%4){case 0:BrokenCrown(target);break;case 1:StartCoroutine(GravityInversion());break;case 2:KingsJudgment(target);break;default:NoKingdomRemains();break;}}
    private Player? Target(){Player? best=null;var bestSq=float.MaxValue;foreach(var p in Player.GetAllPlayers()){if(p==null||p.IsDead())continue;var d=p.transform.position-_center;if(Mathf.Abs(d.x)>34f||Mathf.Abs(d.z)>38f)continue;var sq=(p.transform.position-transform.position).sqrMagnitude;if(sq<bestSq){best=p;bestSq=sq;}}return best;}
    private void BrokenCrown(Player target){Strike(target,92f,38f,1.45f);_next=Time.time+2.1f;}
    private void KingsJudgment(Player target){var d=target.transform.position-transform.position;d.y=0f;if(d.magnitude<=12f)Strike(target,132f,58f,1.8f);_next=Time.time+3.6f;}
    private void NoKingdomRemains(){foreach(var p in Player.GetAllPlayers()){if(p==null||p.IsDead())continue;var d=p.transform.position-_center;d.y=0f;var r=d.magnitude;if(r>=8f&&r<=22f)Strike(p,58f,46f,1.15f);}_next=Time.time+5.2f;}
    private IEnumerator GravityInversion(){_busy=true;_gravityTelegraph=new GameObject("NowhereKing_GravityTelegraph");_gravityTelegraph.transform.position=transform.position+Vector3.up*.25f;var light=_gravityTelegraph.AddComponent<Light>();light.color=new Color(.56f,.22f,.92f);light.range=7f;light.intensity=2.8f;var elapsed=0f;while(elapsed<GravityWindup){if(Staggered()){DestroyGravityTelegraph();_next=Time.time+3f;_busy=false;yield break;}elapsed+=Time.deltaTime;light.range=Mathf.Lerp(7f,18f,elapsed/GravityWindup);light.intensity=Mathf.Lerp(2.8f,6f,elapsed/GravityWindup);yield return null;}DestroyGravityTelegraph();ReleaseGravityWave();_next=Time.time+10.5f;_busy=false;}
    private void DestroyGravityTelegraph(){if(_gravityTelegraph!=null)Destroy(_gravityTelegraph);_gravityTelegraph=null;}
    private void ReleaseGravityWave(){foreach(var p in Player.GetAllPlayers()){if(p==null||p.IsDead())continue;var offset=p.transform.position-transform.position;var planar=new Vector3(offset.x,0f,offset.z);var distance=planar.magnitude;if(distance>GravityRadius)continue;var strength=1f-Mathf.Clamp01(distance/GravityRadius);var outward=planar.sqrMagnitude>.01f?planar.normalized:transform.forward;var body=p.GetComponent<Rigidbody>();if(body!=null){var launch=Vector3.up*Mathf.Lerp(7.5f,13.5f,strength)+outward*Mathf.Lerp(2f,5f,strength);body.linearVelocity=new Vector3(body.linearVelocity.x,Mathf.Max(body.linearVelocity.y,0f),body.linearVelocity.z);body.AddForce(launch,ForceMode.VelocityChange);}Strike(p,24f,34f,.7f);}var pulse=new GameObject("NowhereKing_GravityRelease");pulse.transform.position=transform.position+Vector3.up*.3f;var light=pulse.AddComponent<Light>();light.color=new Color(.82f,.66f,1f);light.range=GravityRadius;light.intensity=7f;Destroy(pulse,.45f);}
    private void Strike(Character target,float slash,float blunt,float stagger){if(target==null||target.IsDead())return;var hit=new HitData();hit.m_point=target.GetCenterPoint();hit.m_dir=(target.transform.position-transform.position).normalized;hit.m_damage.m_slash=slash;hit.m_damage.m_blunt=blunt;hit.m_staggerMultiplier=stagger;hit.SetAttacker(_king);target.Damage(hit);}
}
