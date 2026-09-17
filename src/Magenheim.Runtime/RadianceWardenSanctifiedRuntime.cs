using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Presentation-only Radiance territory for Obelisk Wardens. Combat authority remains with inherited Valheim attacks.</summary>
internal sealed class RadianceWardenSanctifiedRuntime:MonoBehaviour
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
        if(_armed&&Time.time>=_emitAt){_armed=false;EmitSanctifiedTerritory();}
        _wasAttacking=attacking;
    }
    private void EmitSanctifiedTerritory()
    {
        var origin=transform.position+Vector3.up*.055f;
        EmitZone(origin);
        for(var i=0;i<8;i++)
        {
            var direction=Quaternion.Euler(0,i*45f,0)*Vector3.forward;
            var perimeter=origin+direction*(3.05f+(i%2)*.28f);
            EmitPiercingLine(i%2==0?origin+Vector3.up*.34f:perimeter+Vector3.up*.12f,i%2==0?perimeter+Vector3.up*.12f:origin+Vector3.up*.34f,i);
        }
        for(var i=0;i<4;i++)
        {
            var direction=Quaternion.Euler(0,45f+i*90f,0)*Vector3.forward;
            EmitPillar(origin+direction*2.25f,i);
        }
    }
    private static void EmitZone(Vector3 origin)
    {
        var zone=EffectModelAssets.Create("effect-ring");
        zone.name="Magenheim_WardenRadianceSanctifiedZone";
        zone.transform.position=origin;
        zone.transform.localScale=new Vector3(2.75f,.012f,2.75f);
        var collider=zone.GetComponent<Collider>();if(collider)Destroy(collider);
        var renderer=zone.GetComponent<Renderer>();
        if(renderer){var material=new Material(Shader.Find("Standard"));var color=new Color(.96f,.83f,.39f,.34f);material.color=color;material.SetFloat("_Glossiness",.38f);material.SetColor("_EmissionColor",color*.72f);renderer.material=material;}
        zone.AddComponent<SanctifiedLifetime>().Configure(1.85f,1.12f);
    }
    private static void EmitPiercingLine(Vector3 start,Vector3 end,int seed)
    {
        var beam=new GameObject("Magenheim_WardenRadiancePiercingLine");
        var line=beam.AddComponent<LineRenderer>();
        line.useWorldSpace=true;line.positionCount=2;line.startWidth=seed%2==0?.085f:.055f;line.endWidth=.012f;
        var color=new Color(1f,.9f,.48f,.94f);line.startColor=color;line.endColor=new Color(1f,.96f,.7f,.18f);
        var material=new Material(Shader.Find("Standard"));material.color=color;material.SetColor("_EmissionColor",color*1.45f);line.material=material;
        line.SetPosition(0,start);line.SetPosition(1,end);
        beam.AddComponent<BeamLifetime>().Configure(.32f);
    }
    private static void EmitPillar(Vector3 position,int seed)
    {
        var pillar=EffectModelAssets.Create("effect-ring");
        pillar.name="Magenheim_WardenRadianceBoundaryPillar";
        pillar.transform.position=position+Vector3.up*.55f;
        pillar.transform.localScale=new Vector3(.055f,.55f,.055f);
        var collider=pillar.GetComponent<Collider>();if(collider)Destroy(collider);
        var renderer=pillar.GetComponent<Renderer>();
        if(renderer){var material=new Material(Shader.Find("Standard"));var color=new Color(1f,.86f,.38f,.82f);material.color=color;material.SetColor("_EmissionColor",color*(1.05f+seed*.08f));renderer.material=material;}
        pillar.AddComponent<PillarLifetime>().Configure(.72f+seed*.06f);
    }
    private sealed class SanctifiedLifetime:MonoBehaviour
    {
        private float _life,_age,_spread;
        internal void Configure(float life,float spread){_life=life;_spread=spread;}
        private void Update(){_age+=Time.deltaTime;var growth=1f+((_spread-1f)/Mathf.Max(.01f,_life))*Time.deltaTime;transform.localScale=new Vector3(transform.localScale.x*growth,transform.localScale.y,transform.localScale.z*growth);if(_age>=_life)Destroy(gameObject);}
        private void OnDestroy(){var renderer=GetComponent<Renderer>();if(renderer&&renderer.material)Destroy(renderer.material);}
    }
    private sealed class BeamLifetime:MonoBehaviour
    {
        private float _life;
        internal void Configure(float life)=>_life=life;
        private void Update(){_life-=Time.deltaTime;if(_life<=0f)Destroy(gameObject);}
        private void OnDestroy(){var line=GetComponent<LineRenderer>();if(line&&line.material)Destroy(line.material);}
    }
    private sealed class PillarLifetime:MonoBehaviour
    {
        private float _life;
        internal void Configure(float life)=>_life=life;
        private void Update(){_life-=Time.deltaTime;if(_life<=0f)Destroy(gameObject);}
        private void OnDestroy(){var renderer=GetComponent<Renderer>();if(renderer&&renderer.material)Destroy(renderer.material);}
    }
}
