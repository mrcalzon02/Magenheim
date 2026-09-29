using System.Collections;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Server-owned 70%-35% gravity-control phase for the Nowhere King.</summary>
internal sealed class NowhereKingPhaseTwoCombat : MonoBehaviour
{
    private const float Upper=.70f,Lower=.35f,GravityRadius=27f;
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
           _view==null||!_view.IsValid()||!_view.IsOwner()||Time.time<_next)return;
        var hp=_king.GetHealthPercentage();
        if(hp>Upper||hp<=Lower)return;

        var target=Target();
        if(target==null)return;

        switch((_sequence++)%5)
        {
            case 0:StartCoroutine(GravityInversion());break;
            case 1:StartCoroutine(KingsGrasp(target));break;
            case 2:StartCoroutine(Crownfall(target));break;
            case 3:StartCoroutine(RoyalRepulse());break;
            default:StartCoroutine(EventHorizon(target));break;
        }
    }

    private IEnumerator GravityInversion()
    {
        _busy=true;
        _marker=LightMarker("NowhereKing_GravityInversion",transform.position+Vector3.up*.25f,new Color(.56f,.22f,.92f),7f,2.8f);
        var light=_marker.GetComponent<Light>();
        var elapsed=0f;
        const float windup=2.25f;
        while(elapsed<windup)
        {
            if(!Engaged()||Staggered()){DestroyMarker();_next=Time.time+3f;_busy=false;yield break;}
            elapsed+=Time.deltaTime;
            light.range=Mathf.Lerp(7f,18f,elapsed/windup);
            light.intensity=Mathf.Lerp(2.8f,6f,elapsed/windup);
            yield return null;
        }
        DestroyMarker();

        foreach(var p in Player.GetAllPlayers())
        {
            if(!Valid(p))continue;
            var offset=p.transform.position-transform.position;
            var planar=Planar(offset);
            var distance=planar.magnitude;
            if(distance>GravityRadius)continue;
            var strength=1f-Mathf.Clamp01(distance/GravityRadius);
            var outward=planar.sqrMagnitude>.01f?planar.normalized:transform.forward;
            var body=p.GetComponent<Rigidbody>();
            if(body!=null)
            {
                var launch=Vector3.up*Mathf.Lerp(7.5f,13.5f,strength)+outward*Mathf.Lerp(2f,5f,strength);
                body.linearVelocity=new Vector3(body.linearVelocity.x,Mathf.Max(body.linearVelocity.y,0f),body.linearVelocity.z);
                body.AddForce(launch,ForceMode.VelocityChange);
            }
            Hit(p,0f,30f,.7f);
        }

        Flash(transform.position,new Color(.82f,.66f,1f),GravityRadius,7f,.45f);
        _next=Time.time+8.8f;_busy=false;
    }

    private IEnumerator KingsGrasp(Player target)
    {
        _busy=true;
        var point=target.transform.position;
        _marker=LightMarker("NowhereKing_KingsGrasp",point+Vector3.up*.5f,new Color(.38f,.08f,.62f),3.5f,2.6f);
        yield return new WaitForSeconds(.72f);
        DestroyMarker();
        if(Valid(target))
        {
            var delta=transform.position-target.transform.position;
            var planar=Planar(delta);
            if(planar.magnitude<=18f&&planar.sqrMagnitude>.01f)
            {
                var body=target.GetComponent<Rigidbody>();
                if(body!=null)body.AddForce(planar.normalized*Mathf.Min(10f,3f+planar.magnitude*.45f),ForceMode.VelocityChange);
                else target.transform.position=ClampPlayer(target.transform.position+planar.normalized*Mathf.Min(5f,planar.magnitude*.45f));
                Hit(target,0f,18f,.85f);
            }
        }
        _next=Time.time+3.8f;_busy=false;
    }

    private IEnumerator Crownfall(Player target)
    {
        _busy=true;
        var point=ClampPlayer(target.transform.position);
        _marker=LightMarker("NowhereKing_Crownfall",point+Vector3.up*.12f,new Color(.46f,.10f,.74f),4.8f,3.2f);
        yield return new WaitForSeconds(1.05f);
        DestroyMarker();
        foreach(var p in Player.GetAllPlayers())
        {
            if(!Valid(p))continue;
            var planar=Planar(p.transform.position-point);
            if(planar.magnitude>5.2f)continue;
            var body=p.GetComponent<Rigidbody>();
            if(body!=null)body.AddForce(Vector3.down*9f,ForceMode.VelocityChange);
            Hit(p,0f,64f,1.55f);
        }
        Flash(point,new Color(.62f,.16f,.88f),5.5f,4.5f,.35f);
        _next=Time.time+4.2f;_busy=false;
    }

    private IEnumerator RoyalRepulse()
    {
        _busy=true;
        yield return TellAt(transform.position,.48f,7f);
        foreach(var p in Player.GetAllPlayers())
        {
            if(!Valid(p))continue;
            var planar=Planar(p.transform.position-transform.position);
            if(planar.magnitude>8.5f)continue;
            var outward=planar.sqrMagnitude>.01f?planar.normalized:transform.forward;
            var body=p.GetComponent<Rigidbody>();
            if(body!=null)body.AddForce(outward*8.5f+Vector3.up*2f,ForceMode.VelocityChange);
            Hit(p,0f,34f,1f);
        }
        _next=Time.time+3.4f;_busy=false;
    }

    private IEnumerator EventHorizon(Player target)
    {
        _busy=true;
        var point=ClampPlayer(target.transform.position+Planar(target.transform.forward)*2f);
        _marker=LightMarker("NowhereKing_EventHorizon",point+Vector3.up*.2f,new Color(.24f,.01f,.46f),5.5f,3.6f);
        var elapsed=0f;
        const float duration=2.8f;
        while(elapsed<duration&&Engaged())
        {
            elapsed+=.1f;
            foreach(var p in Player.GetAllPlayers())
            {
                if(!Valid(p))continue;
                var delta=point-p.transform.position;
                var planar=Planar(delta);
                if(planar.magnitude>10f||planar.sqrMagnitude<.01f)continue;
                var body=p.GetComponent<Rigidbody>();
                if(body!=null)body.AddForce(planar.normalized*Mathf.Lerp(1.2f,.25f,planar.magnitude/10f),ForceMode.VelocityChange);
            }
            yield return new WaitForSeconds(.1f);
        }
        DestroyMarker();
        foreach(var p in Player.GetAllPlayers())
            if(Valid(p)&&Planar(p.transform.position-point).magnitude<=4f)
                Hit(p,0f,46f,1.2f);
        Flash(point,new Color(.56f,.08f,.82f),6f,5f,.4f);
        _next=Time.time+5.0f;_busy=false;
    }

    private IEnumerator TellAt(Vector3 point,float seconds,float range)
    {
        _marker=LightMarker("NowhereKing_GravityTell",point+Vector3.up*.3f,new Color(.40f,.08f,.68f),range,2.4f);
        yield return new WaitForSeconds(seconds);
        DestroyMarker();
    }

    private GameObject LightMarker(string name,Vector3 point,Color color,float range,float intensity)
    {
        var marker=new GameObject(name);
        marker.transform.position=point;
        var light=marker.AddComponent<Light>();
        light.color=color;light.range=range;light.intensity=intensity;
        return marker;
    }

    private void Flash(Vector3 point,Color color,float range,float intensity,float lifetime)
    {
        var marker=LightMarker("NowhereKing_GravityRelease",point,color,range,intensity);
        Destroy(marker,lifetime);
    }

    private void DestroyMarker(){if(_marker!=null)Destroy(_marker);_marker=null;}

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

    private Vector3 ClampPlayer(Vector3 requested)
    {
        var local=requested-_center;
        local.x=Mathf.Clamp(local.x,-25f,25f);
        local.z=Mathf.Clamp(local.z,-29f,29f);
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
