using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Presentation-only Seidr territory for Obelisk Wardens. Combat authority remains with inherited Valheim attacks.</summary>
internal sealed class SeidrWardenSpatialLatticeRuntime:MonoBehaviour
{
    private Humanoid? _humanoid;
    private bool _wasAttacking,_armed;
    private float _emitAt;
    private void Awake()=>_humanoid=GetComponent<Humanoid>();
    private void Update()
    {
        if(_humanoid==null)return;
        var attacking=_humanoid.InAttack();
        if(attacking&&!_wasAttacking){_armed=true;_emitAt=Time.time+.54f;}
        if(_armed&&Time.time>=_emitAt){_armed=false;EmitSpatialLattice();}
        _wasAttacking=attacking;
    }
    private void EmitSpatialLattice()
    {
        var origin=transform.position+Vector3.up*.12f;
        var nodes=new Vector3[8];
        for(var i=0;i<nodes.Length;i++)
        {
            var direction=Quaternion.Euler(0,i*45f+(i%2)*9f,0)*Vector3.forward;
            var radial=2.45f+(i%3)*.34f;
            var displacement=new Vector3(((i*37)%5-2)*.11f,(i%2)*.18f,((i*23)%5-2)*.09f);
            nodes[i]=origin+direction*radial+displacement;
            EmitRift(nodes[i],i);
        }
        for(var i=0;i<nodes.Length;i++)
        {
            var next=nodes[(i+1)%nodes.Length];
            EmitBrokenLink(nodes[i],next,i);
            if(i%2==0)EmitFalsePosition(nodes[i],origin,i);
        }
        EmitRift(origin+Vector3.up*.22f,11);
    }
    private static void EmitBrokenLink(Vector3 start,Vector3 end,int seed)
    {
        var delta=end-start;
        var side=Vector3.Cross(Vector3.up,delta.normalized);
        var gapCenter=.42f+(seed%3)*.08f;
        var gap=.16f+(seed%2)*.05f;
        var exit=start+delta*(gapCenter-gap)+side*((seed%2==0)?.18f:-.14f)+Vector3.up*.08f;
        var reentry=start+delta*(gapCenter+gap)-side*((seed%3==0)?.22f:-.11f)+Vector3.up*((seed%2==0)?.22f:.05f);
        EmitSegment(start,exit,seed);
        EmitSegment(reentry,end,seed+13);
        EmitRift(exit,seed+20);
        EmitRift(reentry,seed+30);
    }
    private static void EmitSegment(Vector3 start,Vector3 end,int seed)
    {
        var go=new GameObject("Magenheim_WardenSeidrBrokenLattice");
        var line=go.AddComponent<LineRenderer>();
        line.useWorldSpace=true;line.positionCount=3;line.startWidth=.065f;line.endWidth=.018f;
        var color=new Color(.66f,.34f,.9f,.88f);line.startColor=color;line.endColor=new Color(.38f,.18f,.68f,.16f);
        var material=new Material(Shader.Find("Standard"));material.color=color;material.SetFloat("_EmissionColor",color*1.1f);line.material=material;
        var middle=Vector3.Lerp(start,end,.5f)+Vector3.up*(.05f+(seed%3)*.045f);
        line.SetPosition(0,start);line.SetPosition(1,middle);line.SetPosition(2,end);
        go.AddComponent<LatticeLifetime>().Configure(.48f+(seed%2)*.08f);
    }
    private static void EmitRift(Vector3 position,int seed)
    {
        var rift=EffectModelAssets.Create("effect-ring");
        rift.name="Magenheim_WardenSeidrDisplacedNode";
        rift.transform.position=position;
        rift.transform.localScale=new Vector3(.18f+(seed%3)*.035f,.025f,.11f+(seed%2)*.04f);
        rift.transform.rotation=Quaternion.Euler(70f+(seed%2)*12f,seed*47f,seed%3*9f);
        var collider=rift.GetComponent<Collider>();if(collider)Destroy(collider);
        var renderer=rift.GetComponent<Renderer>();
        if(renderer){var material=new Material(Shader.Find("Standard"));var color=new Color(.58f,.25f,.82f,.76f);material.color=color;material.SetFloat("_EmissionColor",color*.72f);renderer.material=material;}
        rift.AddComponent<RiftLifetime>().Configure(.62f+(seed%3)*.05f,seed%2==0?1f:-1f);
    }
    private static void EmitFalsePosition(Vector3 source,Vector3 origin,int seed)
    {
        var outward=(source-origin).normalized;
        var falsePosition=source+Vector3.Cross(Vector3.up,outward)*((seed%4-1.5f)*.42f)+outward*.38f+Vector3.up*(.28f+(seed%3)*.11f);
        EmitRift(falsePosition,seed+40);
        EmitSegment(source+Vector3.up*.04f,falsePosition,seed+50);
    }
    private sealed class LatticeLifetime:MonoBehaviour
    {
        private float _life;
        internal void Configure(float life)=>_life=life;
        private void Update(){_life-=Time.deltaTime;if(_life<=0f)Destroy(gameObject);}
        private void OnDestroy(){var line=GetComponent<LineRenderer>();if(line&&line.material)Destroy(line.material);}
    }
    private sealed class RiftLifetime:MonoBehaviour
    {
        private float _life,_age,_spin;
        internal void Configure(float life,float spin){_life=life;_spin=spin;}
        private void Update(){_age+=Time.deltaTime;transform.Rotate(Vector3.up,_spin*190f*Time.deltaTime,Space.World);var t=Mathf.Clamp01(_age/_life);var scale=1f-(t*.72f);transform.localScale=new Vector3(transform.localScale.x*scale,Mathf.Max(.004f,transform.localScale.y*(1f+t*.8f)),transform.localScale.z*scale);if(_age>=_life)Destroy(gameObject);}
        private void OnDestroy(){var renderer=GetComponent<Renderer>();if(renderer&&renderer.material)Destroy(renderer.material);}
    }
}
