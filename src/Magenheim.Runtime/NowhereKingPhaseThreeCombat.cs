using System.Collections;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Server-owned 35%-0% leap and ground-impact phase for the Nowhere King.</summary>
internal sealed class NowhereKingPhaseThreeCombat : MonoBehaviour
{
    private const float Upper=.35f;
    private Character _king=null!;
    private ZNetView _view=null!;
    private Vector3 _center;
    private bool _configured,_busy;
    private float _next;
    private int _sequence;
    private GameObject? _marker;

    internal void Configure(Vector3 center){_center=center;_configured=true;}
    internal void ResetState(){StopAllCoroutines();DestroyMarker();_busy=false;_next=0f;_sequence=0;}
    private void Awake(){_king=GetComponent<Character>();_view=GetComponent<ZNetView>();}
    private bool Engaged(){var link=GetComponent<NowhereKingEncounterLink>();return link!=null&&link.IsEncounterEngaged;}
    private bool Staggered(){var stagger=GetComponent<NowhereKingRoyalStagger>();return stagger!=null&&stagger.IsOpening;}

    private void Update()
    {
        if(!_configured||_busy||!Engaged()||Staggered()||_king==null||_king.IsDead()||
           _view==null||!_view.IsValid()||!_view.IsOwner()||Time.time<_next||
           _king.GetHealthPercentage()>Upper)return;

        var target=Target();
        if(target==null)return;
        switch((_sequence++)%4)
        {
            case 0:StartCoroutine(KingsDescent(target));break;
            case 1:StartCoroutine(Thronebreaker());break;
            case 2:StartCoroutine(RuinousPursuit(target));break;
            default:StartCoroutine(NoKingdomRemains(target));break;
        }
    }

    private IEnumerator KingsDescent(Player target)
    {
        _busy=true;
        var landing=ClampToArena(target.transform.position+Planar(target.transform.forward)*1.5f);
        _marker=Marker("NowhereKing_DescentLanding",landing,new Color(.48f,.08f,.16f),7f,3.2f);
        Face(landing);
        yield return new WaitForSeconds(.85f);
        DestroyMarker();
        yield return LeapTo(landing,.92f,7.5f);
        Impact(landing,7.2f,165f,2.1f);
        Flash(landing,9f,5.5f);
        _next=Time.time+2.7f;_busy=false;
    }

    private IEnumerator Thronebreaker()
    {
        _busy=true;
        var point=transform.position;
        _marker=Marker("NowhereKing_Thronebreaker",point,new Color(.62f,.15f,.08f),8f,3.8f);
        yield return new WaitForSeconds(1.15f);
        DestroyMarker();
        Impact(point,10.5f,132f,1.85f);
        Flash(point,12f,6f);
        _next=Time.time+3.2f;_busy=false;
    }

    private IEnumerator RuinousPursuit(Player target)
    {
        _busy=true;
        for(var hop=0;hop<3&&Engaged();hop++)
        {
            if(target==null||target.IsDead())break;
            var landing=ClampToArena(target.transform.position);
            _marker=Marker("NowhereKing_PursuitLanding",landing,new Color(.42f,.05f,.12f),4.5f,2.6f);
            Face(landing);
            yield return new WaitForSeconds(.38f);
            DestroyMarker();
            yield return LeapTo(landing,.52f,3.8f);
            Impact(landing,4.6f,58f,1.0f);
            yield return new WaitForSeconds(.18f);
        }
        _next=Time.time+2.4f;_busy=false;
    }

    private IEnumerator NoKingdomRemains(Player target)
    {
        _busy=true;
        for(var slam=0;slam<3&&Engaged();slam++)
        {
            var toward=target!=null&&!target.IsDead()?Planar(target.transform.position-transform.position):Planar(transform.forward);
            if(toward.sqrMagnitude>.01f)
            {
                toward.Normalize();
                var step=ClampToArena(transform.position+toward*3.2f);
                transform.position=step;
                Face(target!=null?target.transform.position:step+toward);
            }
            var radius=6f+slam*1.5f;
            _marker=Marker("NowhereKing_NoKingdomRemains",transform.position,new Color(.55f,.06f,.10f),radius,3f+slam);
            yield return new WaitForSeconds(.58f+slam*.12f);
            DestroyMarker();
            Impact(transform.position,radius,72f+slam*30f,1.2f+slam*.25f);
            Flash(transform.position,radius+2f,4.5f+slam);
            yield return new WaitForSeconds(.24f);
        }
        _next=Time.time+4.4f;_busy=false;
    }

    private IEnumerator LeapTo(Vector3 landing,float duration,float apex)
    {
        var start=transform.position;
        var body=GetComponent<Rigidbody>();
        if(body!=null){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        var elapsed=0f;
        while(elapsed<duration&&Engaged())
        {
            elapsed+=Time.deltaTime;
            var t=Mathf.Clamp01(elapsed/duration);
            var point=Vector3.Lerp(start,landing,t);
            point.y+=Mathf.Sin(t*Mathf.PI)*apex;
            transform.position=point;
            if(body!=null)body.linearVelocity=Vector3.zero;
            yield return null;
        }
        transform.position=landing;
        if(body!=null)body.linearVelocity=Vector3.zero;
    }

    private void Impact(Vector3 point,float radius,float centerDamage,float stagger)
    {
        foreach(var p in Player.GetAllPlayers())
        {
            if(!Valid(p))continue;
            var distance=Planar(p.transform.position-point).magnitude;
            if(distance>radius)continue;
            var falloff=1f-Mathf.Clamp01(distance/radius);
            var damage=centerDamage*Mathf.Lerp(.35f,1f,falloff);
            Hit(p,damage,Mathf.Lerp(.75f,stagger,falloff));
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

    private GameObject Marker(string name,Vector3 point,Color color,float range,float intensity)
    {
        var marker=new GameObject(name);
        marker.transform.position=point+Vector3.up*.18f;
        var light=marker.AddComponent<Light>();
        light.color=color;light.range=range;light.intensity=intensity;
        return marker;
    }

    private void Flash(Vector3 point,float range,float intensity)
    {
        var marker=Marker("NowhereKing_Impact",point,new Color(.78f,.18f,.10f),range,intensity);
        Destroy(marker,.42f);
    }

    private void DestroyMarker(){if(_marker!=null)Destroy(_marker);_marker=null;}
    private static Vector3 Planar(Vector3 value)=>new(value.x,0f,value.z);

    private void Hit(Character target,float blunt,float stagger)
    {
        var hit=new HitData();
        hit.m_point=target.GetCenterPoint();
        hit.m_dir=(target.transform.position-transform.position).normalized;
        hit.m_damage.m_blunt=blunt;
        hit.m_staggerMultiplier=stagger;
        hit.SetAttacker(_king);
        target.Damage(hit);
    }
}
