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
        if(!attacking&&_wasAttacking)_armed=false;
        _wasAttacking=attacking;
    }
    private void EmitPulse()
    {
        var origin=transform.position+Vector3.up*.06f;
        for(var i=0;i<3;i++)
        {
            var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name="Magenheim_WardenTerritoryPulse";
            ring.transform.position=origin+Vector3.up*(i*.012f);
            ring.transform.localScale=new Vector3(.7f+i*.5f,.014f,.7f+i*.5f);
            var collider=ring.GetComponent<Collider>();if(collider)Destroy(collider);
            var renderer=ring.GetComponent<Renderer>();
            if(renderer){var material=new Material(Shader.Find("Standard"));material.color=PulseColor();material.SetFloat("_Metallic",.2f);material.SetFloat("_Glossiness",.45f);renderer.material=material;}
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
            Destroy(marker,.85f);
        }
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
    }
}
