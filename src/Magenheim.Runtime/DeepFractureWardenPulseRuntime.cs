using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Attack-synchronized territorial presentation for Obelisk Wardens. Damage remains owned by Valheim attack items.</summary>
internal sealed class DeepFractureWardenPulseRuntime:MonoBehaviour
{
    internal ElementalAlignment Alignment;
    private Humanoid? _humanoid;
    private bool _wasAttacking,_armed;
    private float _pulseAt;
    private void Awake()=>_humanoid=GetComponent<Humanoid>();
    private void Update()
    {
        if(_humanoid==null)return;
        var attacking=_humanoid.InAttack();
        if(attacking&&!_wasAttacking){_armed=true;_pulseAt=Time.time+.54f;}
        if(_armed&&Time.time>=_pulseAt){_armed=false;EmitPulse();}
        _wasAttacking=attacking;
    }
    private void EmitPulse()
    {
        var origin=transform.position+Vector3.up*.06f;
        var color=PulseColor();
        for(var i=0;i<3;i++)
        {
            var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name="Magenheim_WardenTerritoryPulse";
            ring.transform.position=origin+Vector3.up*(i*.012f);
            ring.transform.localScale=new Vector3(.7f+i*.5f,.014f,.7f+i*.5f);
            var collider=ring.GetComponent<Collider>();if(collider)Destroy(collider);
            var renderer=ring.GetComponent<Renderer>();
            if(renderer){var material=new Material(Shader.Find("Standard"));material.color=color;material.SetFloat("_Metallic",.2f);material.SetFloat("_Glossiness",.45f);renderer.material=material;}
            ring.AddComponent<TerritoryPulse>().Configure(2.6f+i*.45f,.7f+i*.1f);
        }
        for(var i=0;i<8;i++)
        {
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name="Magenheim_WardenPulseMarker";
            var direction=Quaternion.Euler(0,i*45f,0)*Vector3.forward;
            marker.transform.position=origin+direction*(2.4f+i%2*.8f)+Vector3.up*.08f;
            marker.transform.localScale=new Vector3(.08f,.3f,.08f);
            marker.transform.rotation=Quaternion.Euler(0,i*45f,18f);
            var collider=marker.GetComponent<Collider>();if(collider)Destroy(collider);
            var renderer=marker.GetComponent<Renderer>();
            if(renderer){var material=new Material(Shader.Find("Standard"));material.color=color;material.SetFloat("_EmissionColor",color*.65f);renderer.material=material;}
            marker.AddComponent<PulseMarker>().Configure(.85f);
        }
        if(Alignment==ElementalAlignment.Storm)EmitStormNetwork(origin);
    }
    private void EmitStormNetwork(Vector3 origin)
    {
        var nodes=new Vector3[8];
        for(var i=0;i<nodes.Length;i++)
        {
            var direction=Quaternion.Euler(0,i*45f,0)*Vector3.forward;
            nodes[i]=origin+direction*(2.4f+i%2*.8f)+Vector3.up*.22f;
        }
        for(var i=0;i<nodes.Length;i++)
        {
            EmitStormArc(nodes[i],nodes[(i+1)%nodes.Length],i*.17f);
            if(i%2==0)EmitStormArc(nodes[i],nodes[(i+2)%nodes.Length],.55f+i*.11f);
        }
        for(var i=0;i<4;i++)EmitStormArc(origin+Vector3.up*.3f,nodes[i*2],.9f+i*.19f);
    }
    private static void EmitStormArc(Vector3 start,Vector3 end,float bendSeed)
    {
        var arc=new GameObject("Magenheim_WardenStormArc");
        var line=arc.AddComponent<LineRenderer>();
        line.useWorldSpace=true;
        line.positionCount=4;
        line.startWidth=.075f;
        line.endWidth=.018f;
        var color=new Color(.58f,.52f,1f,.92f);
        line.startColor=color;
        line.endColor=new Color(.72f,.68f,1f,.18f);
        var material=new Material(Shader.Find("Standard"));
        material.color=color;
        material.SetFloat("_EmissionColor",color*1.35f);
        line.material=material;
        var delta=end-start;
        var side=Vector3.Cross(Vector3.up,delta.normalized);
        var bend=.18f+Mathf.Abs(Mathf.Sin(bendSeed))* .22f;
        line.SetPosition(0,start);
        line.SetPosition(1,start+delta*.33f+side*bend+Vector3.up*.12f);
        line.SetPosition(2,start+delta*.67f-side*bend*.7f+Vector3.up*.05f);
        line.SetPosition(3,end);
        arc.AddComponent<StormArcLifetime>().Configure(.22f);
    }
    private Color PulseColor()=>Alignment switch
    {
        ElementalAlignment.Fire=>new Color(.9f,.28f,.08f,.58f),
        ElementalAlignment.Frost=>new Color(.38f,.75f,.95f,.58f),
        ElementalAlignment.Storm=>new Color(.48f,.42f,.95f,.58f),
        ElementalAlignment.Earth=>new Color(.55f,.4f,.22f,.58f),
        ElementalAlignment.Venom=>new Color(.35f,.78f,.2f,.58f),
        ElementalAlignment.Radiance=>new Color(.95f,.82f,.38f,.58f),
        _=>new Color(.62f,.45f,.82f,.58f)
    };
    private sealed class TerritoryPulse:MonoBehaviour
    {
        private float _speed,_life,_age;
        internal void Configure(float speed,float life){_speed=speed;_life=life;}
        private void Update(){_age+=Time.deltaTime;var growth=1f+_speed*Time.deltaTime;transform.localScale=new Vector3(transform.localScale.x*growth,transform.localScale.y,transform.localScale.z*growth);if(_age>=_life)Destroy(gameObject);}
        private void OnDestroy(){var renderer=GetComponent<Renderer>();if(renderer&&renderer.material)Destroy(renderer.material);}
    }
    private sealed class PulseMarker:MonoBehaviour
    {
        private float _life;
        internal void Configure(float life)=>_life=life;
        private void Update(){_life-=Time.deltaTime;if(_life<=0f)Destroy(gameObject);}
        private void OnDestroy(){var renderer=GetComponent<Renderer>();if(renderer&&renderer.material)Destroy(renderer.material);}
    }
    private sealed class StormArcLifetime:MonoBehaviour
    {
        private float _life;
        internal void Configure(float life)=>_life=life;
        private void Update(){_life-=Time.deltaTime;if(_life<=0f)Destroy(gameObject);}
        private void OnDestroy(){var line=GetComponent<LineRenderer>();if(line&&line.material)Destroy(line.material);}
    }
}
