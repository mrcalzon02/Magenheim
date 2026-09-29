using System.Collections;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Server-owned 100%-70% twin-sword duel for the Nowhere King.</summary>
internal sealed class NowhereKingPhaseOneCombat : MonoBehaviour
{
    private const float Floor=.70f;
    private Character _king=null!;
    private ZNetView _view=null!;
    private Vector3 _center;
    private bool _configured,_busy;
    private float _next;
    private int _sequence;

    internal void Configure(Vector3 center){_center=center;_configured=true;}
    internal void ResetState(){StopAllCoroutines();_busy=false;_next=0f;_sequence=0;}
    private void Awake(){_king=GetComponent<Character>();_view=GetComponent<ZNetView>();}
    private bool Engaged(){var link=GetComponent<NowhereKingEncounterLink>();return link!=null&&link.IsEncounterEngaged;}
    private bool Staggered(){var stagger=GetComponent<NowhereKingRoyalStagger>();return stagger!=null&&stagger.IsOpening;}

    private void Update()
    {
        if(!_configured||_busy||!Engaged()||Staggered()||_king==null||_king.IsDead()||
           _view==null||!_view.IsValid()||!_view.IsOwner()||Time.time<_next||
           _king.GetHealthPercentage()<Floor)return;

        var target=Target();
        if(target==null)return;
        Face(target.transform.position);
        var distance=Planar(target.transform.position-transform.position).magnitude;
        if(distance>=13f)StartCoroutine(RoyalAdvance(target));
        else if(distance<=4.6f&&(_sequence++%3)==2)StartCoroutine(CrownBreaker());
        else if(distance<=5.5f)StartCoroutine(RoyalCombination());
        else if(distance<=10.5f)StartCoroutine(KingsReach());
        else StartCoroutine(RoyalAdvance(target));
    }

    private IEnumerator RoyalCombination()
    {
        _busy=true;
        yield return Tell(.28f,new Color(.26f,.18f,.42f),4.5f);
        ArcStrike(5.5f,78f,68f,20f,1.05f);
        yield return new WaitForSeconds(.24f);
        ArcStrike(5.7f,82f,76f,24f,1.10f);
        if((_sequence&1)==0)
        {
            yield return new WaitForSeconds(.32f);
            ArcStrike(6.0f,52f,92f,28f,1.25f);
        }
        _next=Time.time+1.0f;_busy=false;
    }

    private IEnumerator CrownBreaker()
    {
        _busy=true;
        yield return Tell(.92f,new Color(.46f,.22f,.12f),5.2f);
        ArcStrike(4.8f,52f,118f,64f,1.85f);
        _next=Time.time+2.0f;_busy=false;
    }

    private IEnumerator KingsReach()
    {
        _busy=true;
        yield return Tell(.62f,new Color(.22f,.10f,.36f),7.0f);
        ArcStrike(10.5f,112f,82f,34f,1.25f);
        _next=Time.time+1.7f;_busy=false;
    }

    private IEnumerator RoyalAdvance(Player target)
    {
        _busy=true;
        yield return Tell(.42f,new Color(.18f,.10f,.26f),4.2f);
        var start=transform.position;
        var direction=Planar(target.transform.position-start);
        if(direction.sqrMagnitude>.01f)
        {
            direction.Normalize();
            var distance=Mathf.Max(0f,Planar(target.transform.position-start).magnitude-3.6f);
            var destination=ClampToArena(start+direction*Mathf.Min(8.5f,distance));
            var duration=.52f;
            var elapsed=0f;
            while(elapsed<duration&&Engaged())
            {
                elapsed+=Time.deltaTime;
                transform.position=Vector3.Lerp(start,destination,Mathf.Clamp01(elapsed/duration));
                yield return null;
            }
            transform.position=destination;
            Face(target.transform.position);
            ArcStrike(6.2f,58f,72f,26f,1.0f);
        }
        _next=Time.time+2.6f;_busy=false;
    }

    private IEnumerator Tell(float seconds,Color color,float range)
    {
        var marker=new GameObject("NowhereKing_SwordTell");
        marker.transform.SetParent(transform,false);
        marker.transform.localPosition=Vector3.up*1.5f;
        var light=marker.AddComponent<Light>();
        light.color=color;light.range=range;light.intensity=1.8f;
        var elapsed=0f;
        while(elapsed<seconds&&Engaged()&&!Staggered())
        {
            elapsed+=Time.deltaTime;
            light.intensity=Mathf.Lerp(1.8f,4.2f,elapsed/seconds);
            yield return null;
        }
        Destroy(marker);
    }

    private void ArcStrike(float radius,float halfAngle,float slash,float blunt,float stagger)
    {
        var forward=Planar(transform.forward).normalized;
        foreach(var player in Player.GetAllPlayers())
        {
            if(!Valid(player))continue;
            var delta=Planar(player.transform.position-transform.position);
            if(delta.magnitude>radius)continue;
            var angle=delta.sqrMagnitude<.01f?0f:Vector3.Angle(forward,delta.normalized);
            if(angle>halfAngle)continue;
            Hit(player,slash,blunt,stagger);
        }
    }

    private Player? Target()
    {
        Player? best=null;var bestSq=float.MaxValue;
        foreach(var p in Player.GetAllPlayers())
        {
            if(!Valid(p))continue;
            var sq=(p.transform.position-transform.position).sqrMagnitude;
            if(sq<bestSq){best=p;bestSq=sq;}
        }
        return best;
    }

    private bool Valid(Player? p)
    {
        if(p==null||p.IsDead()||p.gameObject.scene.handle!=gameObject.scene.handle)return false;
        var d=p.transform.position-_center;
        return Mathf.Abs(d.x)<=34f&&Mathf.Abs(d.z)<=38f;
    }

    private void Face(Vector3 point)
    {
        var planar=Planar(point-transform.position);
        if(planar.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(planar.normalized);
    }

    private Vector3 ClampToArena(Vector3 requested)
    {
        var local=requested-_center;
        local.x=Mathf.Clamp(local.x,-24f,24f);
        local.z=Mathf.Clamp(local.z,-28f,28f);
        return _center+local;
    }

    private static Vector3 Planar(Vector3 value)=>new(value.x,0f,value.z);

    private void Hit(Character target,float slash,float blunt,float stagger)
    {
        var hit=new HitData();
        hit.m_point=target.GetCenterPoint();
        hit.m_dir=(target.transform.position-transform.position).normalized;
        hit.m_damage.m_slash=slash;
        hit.m_damage.m_blunt=blunt;
        hit.m_staggerMultiplier=stagger;
        hit.SetAttacker(_king);
        target.Damage(hit);
    }
}
