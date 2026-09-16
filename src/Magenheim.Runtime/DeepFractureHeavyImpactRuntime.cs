using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Attack-synchronized presentation for the heavy Deep Fracture chassis. Damage remains owned by the inherited Valheim attack items.</summary>
internal sealed class DeepFractureHeavyImpactRuntime:MonoBehaviour
{
    internal enum Profile{CrystalGolem,DeepColossus}
    internal Profile ImpactProfile;
    private Humanoid? _humanoid;
    private bool _wasAttacking,_armed;
    private float _impactAt;
    private void Awake()=>_humanoid=GetComponent<Humanoid>();
    private void Update(){if(_humanoid==null)return;var attacking=_humanoid.InAttack();if(attacking&&!_wasAttacking){_armed=true;_impactAt=Time.time+(ImpactProfile==Profile.DeepColossus ? .62f : .46f);}if(_armed&&Time.time>=_impactAt){_armed=false;EmitImpact();}if(!attacking&&_wasAttacking)_armed=false;_wasAttacking=attacking;}
    private void EmitImpact(){var origin=transform.position+transform.forward*(ImpactProfile==Profile.DeepColossus?2.2f:1.45f)+Vector3.up*.08f;var rings=ImpactProfile==Profile.DeepColossus?3:2;for(var i=0;i<rings;i++){var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="Magenheim_HeavyImpactWave";ring.transform.position=origin+Vector3.up*(.015f*i);ring.transform.localScale=new Vector3(.45f+i*.55f,.018f,.45f+i*.55f);var c=ring.GetComponent<Collider>();if(c)Destroy(c);var r=ring.GetComponent<Renderer>();if(r){var m=new Material(Shader.Find("Standard"));m.color=ImpactProfile==Profile.DeepColossus?new Color(.42f,.18f,.62f,.62f):new Color(.34f,.62f,.78f,.56f);m.SetFloat("_Metallic",.15f);m.SetFloat("_Glossiness",.35f);r.material=m;}var fx=ring.AddComponent<HeavyImpactWave>();fx.Configure(ImpactProfile==Profile.DeepColossus?5.8f:3.8f,.48f+i*.08f);}var shards=ImpactProfile==Profile.DeepColossus?12:7;for(var i=0;i<shards;i++){var shard=GameObject.CreatePrimitive(PrimitiveType.Cube);shard.name="Magenheim_HeavyImpactShard";shard.transform.position=origin+Random.insideUnitSphere*.35f;shard.transform.localScale=new Vector3(.08f,.16f,.08f)*(ImpactProfile==Profile.DeepColossus?1.35f:1f);var c=shard.GetComponent<Collider>();if(c)Destroy(c);var body=shard.AddComponent<Rigidbody>();body.mass=.05f;body.velocity=(Quaternion.Euler(0,360f*i/shards,0)*Vector3.forward)*(2.2f+Random.value*1.8f)+Vector3.up*(1.8f+Random.value*2f);body.angularVelocity=Random.insideUnitSphere*8f;Destroy(shard,1.15f);}}
    private sealed class HeavyImpactWave:MonoBehaviour{private float _speed,_life,_age;internal void Configure(float speed,float life){_speed=speed;_life=life;}private void Update(){_age+=Time.deltaTime;var s=1f+_speed*Time.deltaTime;transform.localScale=new Vector3(transform.localScale.x*s,transform.localScale.y,transform.localScale.z*s);if(_age>=_life)Destroy(gameObject);}}
}
