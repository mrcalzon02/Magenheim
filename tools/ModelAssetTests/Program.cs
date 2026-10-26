using UnityEngine;
using Magenheim.Runtime;
using Newtonsoft.Json.Linq;
// Reported rear-facing creature previews exposed two opposite export frames.
// Check actual named anatomical landmarks, not a symmetric bounding box.
{
 var modelRoot=Path.Combine(AppContext.BaseDirectory,"assets/models/runtime");
 var legacy=CreatureVisualOrientation.Correction("rigid-segment-creature-v1");
 var weighted=CreatureVisualOrientation.Correction("weighted-creature-v1");
 if(Vector3.Dot(legacy*(-Vector3.forward),Vector3.forward)<.99999f || Vector3.Dot(weighted*Vector3.forward,Vector3.forward)<.99999f)
  throw new Exception("Creature forward conventions do not reach native +Z.");
 foreach(var (id,front,back) in new[]{
  ("glacier-stalker","GS_Jaw","GS_Tail_2"),
  ("cinder-hound","CH_LowerJaw","CH_Tail_3")})
 {
  var doc=JObject.Parse(File.ReadAllText(Path.Combine(modelRoot,"underworld-creature-"+id+".model.json")));
  Vector3 Center(string name) {
   var part=((JArray)doc["parts"]!).Single(p=>(string?)p["name"]==name);
   var points=((JArray)part["vertices"]!).Select(ModelAssets.Vector).ToArray();
   return points.Aggregate(Vector3.zero,(a,b)=>a+b)*(1f/points.Length);
  }
  var head=Center(front);var tail=Center(back);
  if(head.z>=0 || tail.z<=0 || (legacy*head).z<=0 || (legacy*tail).z>=0)
   throw new Exception("Actual jaw/tail orientation regression: "+id);
  if(MathF.Abs((legacy*head).y-head.y)>.00001f || MathF.Abs((legacy*head).magnitude-head.magnitude)>.00001f)
   throw new Exception("Creature orientation changed height or scale: "+id);
 }
 var rigidCount=Directory.GetFiles(modelRoot,"underworld-creature-*.model.json").Count(p=>(string?)JObject.Parse(File.ReadAllText(p))["creatureRig"]?["kind"]=="rigid-segment-creature-v1");
 if(rigidCount!=42)throw new Exception("Creature orientation coverage changed; review model conventions: "+rigidCount);
 var rejected=false;try{CreatureVisualOrientation.Correction("unknown");}catch(InvalidOperationException){rejected=true;}
 if(!rejected)throw new Exception("Unknown creature orientation silently admitted.");
 Console.WriteLine("PASS: 42 legacy creature frames rotate together to +Z; actual exported jaws/tails, height and scale verified; weighted boss forward preserved.");
}
// Regression from actual exported geometry: two 2m root pillars snapped end to
// end leave a gap greater than the installed engine's .15m neighbour tolerance.
{
 var path = Path.Combine(AppContext.BaseDirectory,"assets/models/runtime/rootforged-worldroot-pillar-2m.model.json");
 var parts = ((JArray)JObject.Parse(File.ReadAllText(path))["parts"]!).OfType<JObject>().Where(p=>(bool?)p["collider"]==true);
 var vertices = parts.SelectMany(p=>((JArray)p["vertices"]!).OfType<JArray>()).ToArray();
 var bottom = vertices.Min(v=>(float)v[1]!);
 var top = vertices.Max(v=>(float)v[1]!);
 if (2f+bottom-top <= .15f) throw new Exception("Recessed-end regression fixture no longer reproduces the reported contact gap.");
 var contacts = new GameObject("collision") { layer=10 };
 var lower = ConstructionJointContacts.Add(contacts,new Vector3(0,2,0));
 var upper = ConstructionJointContacts.Add(contacts,Vector3.zero);
 // Evaluate touching faces both upright and perpendicular: rotate the upper
// native box 90 degrees and snap its foot onto the lower pillar's crown.
 foreach(var rotation in new[]{Quaternion.identity,Quaternion.Euler(0,0,90)})
 {
  var half=upper.size*.5f;
  var axis=rotation*Vector3.up;
  var upperHalfY=MathF.Abs(axis.y)*half.y+MathF.Abs((rotation*Vector3.right).y)*half.x+MathF.Abs((rotation*Vector3.forward).y)*half.z;
  if (2f-upperHalfY > lower.center.y+lower.size.y*.5f+.15f)
   throw new Exception("Native joint boxes fail stacked/perpendicular support overlap.");
 }
 // Collision must survive the native visual-state switch.
 var visual=new GameObject("new");visual.transform.SetParent(contacts.transform.parent);visual.SetActive(false);
 if(!contacts.activeSelf || contacts.GetComponentsInChildren<BoxCollider>().Length!=2)
  throw new Exception("Wear visual state removed structural contacts.");
 Console.WriteLine("PASS: exported recessed root endpoints reproduced; native joint contacts bridge stacked/perpendicular connections.");
}
foreach(var size in new[]{2f,4f,8f})
{
 var prefab=new GameObject("foundation");
 var inherited=new GameObject("old-snap") {tag="snappoint"};inherited.transform.SetParent(prefab.transform);
 ConstructionJointContacts.FoundationSnaps(prefab,size,.34f);
 var snaps=prefab.GetComponentsInChildren<Transform>(true).Where(t=>t.CompareTag("snappoint")).ToArray();
 if(inherited.tag!="Untagged" || snaps.Length!=18 || snaps.Any(t=>t.parent!=prefab.transform))
  throw new Exception("Crystal foundation retains donor snaps or hides native snaps under children.");
 foreach(var y in new[]{0f,.34f})
  if(snaps.Count(t=>MathF.Abs(t.localPosition.y-y)<.0001f)!=9)
   throw new Exception("Crystal foundation lost bottom/top contact planes.");
 if(snaps.Any(t=>MathF.Abs(t.localPosition.x)>size*.5f || MathF.Abs(t.localPosition.z)>size*.5f))
  throw new Exception("Crystal foundation snap escapes its solid collision footprint.");
}
Console.WriteLine("PASS: 2/4/8m crystal foundations expose 18 native direct-child snaps on separate bottom/top surfaces.");
// A beehive is the persistent producer donor for Crystal Beds and the Ice Box. Its vegetation
// shader interprets arbitrary vertex data as wind input and visibly pulls the otherwise-correct
// model parts apart. Verify this before the shared material cache is populated by the library pass.
{
 var host=HostForUnsafeShader("Custom/Vegetation");
 var loaded=ModelAssets.Load(host,"crystal-bed-frost");
 var shaders=loaded.GetComponentsInChildren<MeshRenderer>(true).Select(r=>r.sharedMaterial?.shader?.name).Distinct().ToArray();
 if(shaders.Length!=1 || shaders[0]!="Custom/StaticRock")throw new Exception("Vegetation donor shader reached authored bed meshes.");
 Console.WriteLine("PASS: vegetation donor shader replaced before authored bed rendering.");
}
var count=0;
// Shader names vary across native donors/mods. A gameplay donor's unrecognized wind
// shader must never bypass the material template used by the working dais.
{
 var template = new Material(Shader.Find("Custom/StaticRock"));
 ModelAssets.SurfaceMaterialProvider = () => template;
 foreach(var id in new[]{"crystal-bed-earth","underworld-station-mycelial-bench","crystalline-ice-box"})
 {
  if(!ModelAssets.Exists(id)) throw new Exception("Material regression fixture is missing: "+id);
  var host=HostForUnsafeShader("Custom/UnlistedWindVariant");
  var loaded=ModelAssets.Load(host,id);
  if(loaded.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.sharedMaterial?.shader?.name!="Custom/StaticRock"))
   throw new Exception("Gameplay donor shader bypassed native static material template: "+id);
 }
 ModelAssets.SurfaceMaterialProvider=()=>null;
 var missingTemplateRejected=false;
 try { ModelAssets.Load(HostForUnsafeShader("Custom/UnlistedWindVariant"),"crystal-bed-earth"); }
 catch(InvalidOperationException e) when(e.Message.Contains("Native Rock_4")) { missingTemplateRejected=true; }
 if(!missingTemplateRejected) throw new Exception("Missing native template silently fell back to an unsafe donor shader.");
 ModelAssets.SurfaceMaterialProvider=null;
 Console.WriteLine("PASS: native static material template overrides unrecognized gameplay donor shaders.");
}
var libraryTemplate = new Material(Shader.Find("Custom/StaticRock"));
libraryTemplate.SetFloat("_Parallax",2f);
libraryTemplate.SetFloat("_Displacement",3f);
libraryTemplate.SetTexture("_MossTex",Texture2D.whiteTexture);
ModelAssets.SurfaceMaterialProvider=()=>libraryTemplate;
var cleanTemplate=ModelAssets.CreateSurfaceMaterial();
if(cleanTemplate.shader?.name!="Custom/StaticRock" || cleanTemplate.Floats["_Parallax"]!=0 || cleanTemplate.Floats["_Displacement"]!=0 || cleanTemplate.Textures["_MossTex"]!=null)
 throw new Exception("Legacy/effect material factory retained donor displacement state.");
if(libraryTemplate.Floats["_Parallax"]!=2f || libraryTemplate.Textures["_MossTex"]==null)
 throw new Exception("Material cleanup modified the shared native donor.");
foreach(var id in new[]{"underworld-descent-monolith","underworld-dais"})
 if(ModelAssets.LoadSingleMaterial(id).shader?.name!="Custom/StaticRock") throw new Exception("Single-mesh structure bypassed native material template.");
foreach(var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"assets/models/runtime"),"*.model.json")) {
 var id=Path.GetFileName(file).Replace(".model.json","");
 var document=JObject.Parse(File.ReadAllText(file));
 if((string?)document["creatureRig"]?["kind"]=="weighted-creature-v1")
 {
  (GameObject Host,GameObject Root,Transform[] Bones) WeightedHost()
  {
   var host=new GameObject(id);host.AddComponent<MeshRenderer>().sharedMaterial=new Material();
   var root=new GameObject("weighted visual");root.transform.SetParent(host.transform);
   var bones=((JArray)document["creatureRig"]!["bones"]!).Select(row=>{
    var go=new GameObject("magenheim.creature-bone."+(string)row["name"]!);go.transform.SetParent(root.transform);
    go.transform.localPosition=ModelAssets.Vector(row["head"]!);return go.transform;
   }).ToArray();return(host,root,bones);
  }
  var weightedFirst=WeightedHost();var weightedSecond=WeightedHost();
  ModelAssets.LoadCreatureSkin(weightedFirst.Host,id,weightedFirst.Root.transform,weightedFirst.Bones);
  ModelAssets.LoadCreatureSkin(weightedSecond.Host,id,weightedSecond.Root.transform,weightedSecond.Bones);
  var weightedA=weightedFirst.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();var weightedB=weightedSecond.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
  if(!ReferenceEquals(weightedA.sharedMesh,weightedB.sharedMesh)||weightedA.bones.Any(bone=>!bone.IsChildOf(weightedFirst.Root.transform))||weightedB.bones.Any(bone=>!bone.IsChildOf(weightedSecond.Root.transform)))throw new Exception("Weighted creature cache or instance bone binding failed: "+id);
  if(weightedA.sharedMesh!.boneWeights.Length!=weightedA.sharedMesh.vertices.Length||weightedA.sharedMesh.bindposes.Length!=weightedFirst.Bones.Length||weightedA.rootBone!=weightedFirst.Root.transform||weightedA.localBounds.size.y<=0)throw new Exception("Weighted creature lost skin, bind poses or animated bounds: "+id);
  if(weightedA.sharedMaterial!.shader!.name!="Custom/StaticRock"||weightedFirst.Root.GetComponentsInChildren<MeshCollider>(true).Length!=0)throw new Exception("Weighted creature shader or native collision ownership changed: "+id);
  if(weightedA.sharedMesh.boneWeights.Any(w=>MathF.Abs(w.weight0+w.weight1-1)>.0001f||w.weight2!=0||w.weight3!=0))throw new Exception("Weighted creature lost its two-influence normalization: "+id);
  if(!weightedA.sharedMesh.boneWeights.Any(w=>w.weight1>0))throw new Exception("Weighted creature lost all blended joints: "+id);
  var rejected=false;try{ModelAssets.Load(weightedFirst.Host,id);}catch(InvalidOperationException e)when(e.Message.Contains("skinned presentation")){rejected=true;}
  if(!rejected)throw new Exception("Weighted creature silently admitted as a static model: "+id);
  var wrong=WeightedHost();rejected=false;
  try{ModelAssets.LoadCreatureSkin(wrong.Host,id,wrong.Root.transform,wrong.Bones.Reverse().ToArray());}catch(InvalidDataException){rejected=true;}
  if(!rejected||wrong.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=0)throw new Exception("Wrong bone order partially admitted: "+id);
  Console.WriteLine("PASS: "+id+" imported twice with shared mesh, instance bones, blended joints, native shader and animated bounds.");count++;continue;
 }
 GameObject Host(){var h=new GameObject(id);h.AddComponent<Character>();h.AddComponent<ZNetView>();h.AddComponent<MeshRenderer>().sharedMaterial=new Material();var b=new GameObject("body");b.transform.SetParent(h.transform);h.AddComponent<Turret>().m_turretBody=b;return h;}
 var first=Host();var second=Host();ModelAssets.Load(first,id);ModelAssets.Load(second,id);
 foreach(var renderer in first.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))
 {
  var material=renderer.sharedMaterial!;
  if(material.shader?.name!="Custom/StaticRock" || material.Floats["_Parallax"]!=0 || material.Floats["_Displacement"]!=0)
   throw new Exception("Unsafe shader or inherited displacement in library model: "+id);
 }
 var a=first.GetComponentsInChildren<MeshFilter>(true);var b=second.GetComponentsInChildren<MeshFilter>(true);
 var expected=((JArray)JObject.Parse(File.ReadAllText(file))["parts"]!).Count;
 if(a.Length!=expected || b.Length!=expected)throw new Exception("Part loss: "+id);
 for(int i=0;i<a.Length;i++){if(!ReferenceEquals(a[i].sharedMesh,b[i].sharedMesh))throw new Exception("Mesh cache failed: "+id);if(a[i].sharedMesh!.uv.Length!=a[i].sharedMesh!.vertices.Length)throw new Exception("UV loss: "+id);}
 if(first.GetComponent<MeshRenderer>()!.enabled)throw new Exception("Inherited geometry still visible: "+id);
 if(id.StartsWith("underworld-flora-fungal-",StringComparison.Ordinal))
 {
  if(a.Sum(m=>m.sharedMesh!.triangles.Length/3)>596) throw new Exception("Fungal mesh exceeds measured native Fir budget: "+id);
  var lod=first.GetComponentsInChildren<LODGroup>(true).SingleOrDefault();
  if(lod==null || !lod.enabled || lod.GetLODs().Length!=1 || lod.GetLODs()[0].renderers.Length!=a.Length)
   throw new Exception("Authored fungus has no active distance-culling group: "+id);
 }
 count++;
}
{
 var pickup=new GameObject("Glowcap Flesh"){layer=9};
 pickup.AddComponent<Pickable>();
 var old=new GameObject("native-interaction"){layer=12};old.transform.SetParent(pickup.transform);
 old.AddComponent<CapsuleCollider>();
 UnderworldResourceVisuals.Apply(pickup,"underworld-resource-glowcap-flesh");
 var box=pickup.GetComponent<BoxCollider>()!;
 if(pickup.layer!=12 || box.size.y<.5f || box.center.y-box.size.y*.5f<0 || box.size.x<.4f)
  throw new Exception("Small Glowcap pickup lost its donor interaction layer or above-ground interaction volume.");
 Console.WriteLine("PASS: tiny authored Glowcap pickup retains interaction layer and accessible above-ground target.");
}
ModelAssets.SurfaceMaterialProvider=null;
// The actual exported grip must land on the hand after both mortal scaling and rotation.
foreach (var id in new[] { "nowhere-king-sword-firmament", "nowhere-king-sword-null-gate", "underworld-weapon-amber-blade", "crystal-weapon-sword", "underworld-tool-sporelight-lantern" })
foreach (var scale in new[] { .43f, 1f })
{
    var hand = new GameObject("hand");
    hand.transform.localPosition = new Vector3(3f, 2f, -4f);
    hand.transform.localRotation = Quaternion.Euler(20f, 35f, 15f);
    var model = ModelAssets.Load(hand, id, hideOriginal: false, scale: scale);
    model.transform.localRotation = Quaternion.Euler(90f, 0f, 180f);
    var partName = id == "underworld-tool-sporelight-lantern" ? "handle-crown" : "grip";
    var grip = model.GetComponentsInChildren<MeshFilter>(true).Single(m => m.gameObject.name == partName);
    var before = hand.transform.InverseTransformPoint(grip.transform.TransformPoint(grip.sharedMesh!.bounds.center));
    HeldModelAlignment.SeatGrip(hand.transform, model, id, partName);
    var after = hand.transform.InverseTransformPoint(grip.transform.TransformPoint(grip.sharedMesh!.bounds.center));
    if (after.sqrMagnitude > 1e-8f || before.sqrMagnitude < .01f)
        throw new Exception("Authored grip seating failed: " + id + " scale=" + scale);
}
Console.WriteLine("PASS: ten real-payload grip/lantern-handle seating checks at full and paired scale.");
foreach (var (id, profile) in new[] {
    ("worldroot-club", "crystal-weapon-mace"), ("worldroot-bow", "crystal-weapon-bow"),
    ("flowstone-maul", "crystal-weapon-mace"), ("blackwater-harpoon", "crystal-weapon-spear"),
    ("emberiron-axe", "crystal-weapon-axe"), ("emberiron-greatsword", "crystal-weapon-greatsword"),
    ("rimesilver-spear", "crystal-weapon-spear"), ("icebind-staff", "staff-frost-crystal"),
    ("titanbone-atgeir", "crystal-weapon-atgeir"), ("shardstone-crossbow", "crystal-weapon-crossbow"),
    ("amber-blade", "crystal-weapon-sword"), ("crown-sceptre", "crystal-weapon-mace") })
{
    var host = new GameObject("attach");
    var hidden = GameObject.CreatePrimitive(PrimitiveType.Cube);
    hidden.transform.SetParent(host.transform, false);
    hidden.transform.localScale = new Vector3(15f, 1f, 3f);
    hidden.GetComponent<MeshRenderer>()!.enabled = false;
    var ancestor = ModelAssets.Load(host, profile, hideOriginal: false);
    ancestor.transform.localRotation = Quaternion.Euler(90f, 0f, 17f);
    ancestor.transform.localPosition = new Vector3(0f, 0f, .18f);
    var originals = host.GetComponentsInChildren<Renderer>(true);
    var modelId = "underworld-weapon-" + id;
    var model = ModelAssets.Load(host, modelId, hideOriginal: false);
    HeldModelAlignment.Apply(host.transform, originals, model, modelId, null);
    if (Quaternion.Angle(model.transform.localRotation, ancestor.transform.localRotation) > .01f)
        throw new Exception("Inherited weapon pose changed due to hidden donor geometry: " + modelId);
    if (id != "amber-blade" && (model.transform.localPosition-ancestor.transform.localPosition).sqrMagnitude > 1e-8f)
        throw new Exception("Inherited weapon seating was trimmed twice: " + modelId);
    if (id == "amber-blade")
    {
        var grip = model.GetComponentsInChildren<MeshFilter>(true).Single(m => m.gameObject.name == "grip");
        if (host.transform.InverseTransformPoint(grip.transform.TransformPoint(grip.sharedMesh!.bounds.center)).sqrMagnitude > 1e-8f)
            throw new Exception("Amber Blade lost its identity-specific grip seating.");
    }
}
Console.WriteLine("PASS: all twelve Underworld upgrades retain ancestor pose despite hidden donor meshes; trims are not doubled.");
Console.WriteLine($"PASS: {count} library models use the native static template with zero inherited displacement; single-mesh and legacy/effect factories match.");
// Geode rigidbodies require caller-owned primitive collision, while ordinary authored
// static geometry must retain its mesh collision.
{
 var boxed=new GameObject("geode-box-collision");
 var visual=ModelAssets.Load(boxed,"geode-sample",createColliders:false);
 if(visual.GetComponentsInChildren<MeshCollider>(true).Length!=0)throw new Exception("Geode visual created concave collision.");
 if(visual.GetComponentsInChildren<MeshFilter>(true).Length==0)throw new Exception("Geode collider suppression lost its visible mesh.");
 var normal=new GameObject("geode-default-collision");
 ModelAssets.Load(normal,"geode-sample");
 if(normal.GetComponentsInChildren<MeshCollider>(true).Length==0)throw new Exception("Collider opt-out disabled normal model collision.");
 Console.WriteLine("PASS: caller-owned geode collision suppresses concave colliders without losing art or changing default model collision.");
}
// Runtime PBR regression: a payload that declares authored normal, metallic/smoothness and
// emission maps must survive donor cleanup and reach the final Unity material with the correct
// color-space intent and shader keywords.
{
 var textureDirectory=Path.Combine(AppContext.BaseDirectory,"assets/models/textures");
 var sourceTexture=Directory.GetFiles(textureDirectory,"*.png").First();
 var textureName=Path.GetFileName(sourceTexture);
 var pbrId="__magenheim-pbr-runtime-test";
 var runtimeDirectory=Path.Combine(AppContext.BaseDirectory,"assets/models/runtime");
 var pbrPath=Path.Combine(runtimeDirectory,pbrId+".model.json");
 var payload=JObject.FromObject(new {
  parts=new[]{new {
   name="surface",path="surface",
   vertices=new[]{new[]{0f,0f,0f},new[]{1f,0f,0f},new[]{0f,1f,0f}},
   normals=new[]{new[]{0f,0f,1f},new[]{0f,0f,1f},new[]{0f,0f,1f}},
   uv=new[]{new[]{0f,0f},new[]{1f,0f},new[]{0f,1f}},triangles=new[]{0,1,2},
   material=new {
    doubleSided=false,name="magenheim.test.pbr",color=new[]{1f,1f,1f,1f},
    metallic=.4f,roughness=.6f,emission=new[]{.8f,.8f,.8f},
    texture=textureName,normalTexture=textureName,normalScale=.42f,
    metallicGlossTexture=textureName,emissionTexture=textureName
   },collider=false,crystal=(object?)null
  }},lights=Array.Empty<object>()
 });
 File.WriteAllText(pbrPath,payload.ToString(Newtonsoft.Json.Formatting.None));
 try
 {
  var host=new GameObject("pbr-host");host.AddComponent<Character>();host.AddComponent<ZNetView>();
  host.AddComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find("Standard"));
  var body=new GameObject("body");body.transform.SetParent(host.transform);host.AddComponent<Turret>().m_turretBody=body;
  var loaded=ModelAssets.Load(host,pbrId,hideOriginal:false);
  var material=loaded.GetComponentsInChildren<MeshRenderer>(true).Single().sharedMaterial!;
  foreach(var slot in new[]{"_BumpMap","_MetallicGlossMap","_EmissionMap"})
   if(!material.Textures.TryGetValue(slot,out var value) || value is not Texture2D)throw new Exception("Runtime PBR texture missing: "+slot);
  if(material.Textures["_BumpMap"] is not Texture2D normal || !normal.linear)throw new Exception("Normal map must load linear.");
  if(material.Textures["_MetallicGlossMap"] is not Texture2D metalGloss || !metalGloss.linear)throw new Exception("Metallic/smoothness map must load linear.");
  if(material.Textures["_EmissionMap"] is not Texture2D emissionMap || emissionMap.linear)throw new Exception("Emission map must load sRGB.");
  foreach(var keyword in new[]{"_NORMALMAP","_METALLICGLOSSMAP","_EMISSION"})
   if(!material.Keywords.Contains(keyword))throw new Exception("Runtime PBR keyword missing: "+keyword);
  if(!material.Floats.TryGetValue("_BumpScale",out var bump) || MathF.Abs(bump-.42f)>.001f)throw new Exception("Runtime normal scale drift.");
  if(!material.Floats.TryGetValue("_Metallic",out var metallicScale) || MathF.Abs(metallicScale-1f)>.001f)throw new Exception("Packed metallic map is being multiplied by a non-neutral scalar.");
  if(!material.Floats.TryGetValue("_GlossMapScale",out var glossScale) || MathF.Abs(glossScale-1f)>.001f)throw new Exception("Packed smoothness alpha is being multiplied by donor gloss scale.");
  if(!material.Floats.TryGetValue("_SmoothnessTextureChannel",out var smoothChannel) || MathF.Abs(smoothChannel)>.001f)throw new Exception("Smoothness must come from metallic-map alpha.");
  Console.WriteLine("PASS: explicit runtime PBR maps survive donor cleanup with linear normal/metal maps, authoritative metallic/smoothness scaling, and sRGB emission.");
 }
 finally { if(File.Exists(pbrPath))File.Delete(pbrPath); }
}

{
 var textureName=Path.GetFileName(Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"assets/models/textures"),"*.png").First());
 var id="__magenheim-skin-runtime-test";var path=Path.Combine(AppContext.BaseDirectory,"assets/models/runtime",id+".model.json");
 var payload=JObject.FromObject(new {skinRig=new{kind="valheim-player-attach-skin",bones=new[]{"Hips"},root="Hips"},parts=new[]{new{name="skin",path="skin",vertices=new[]{new[]{0f,0f,0f},new[]{1f,0f,0f},new[]{0f,1f,0f}},normals=new[]{new[]{0f,0f,1f},new[]{0f,0f,1f},new[]{0f,0f,1f}},uv=new[]{new[]{0f,0f},new[]{1f,0f},new[]{0f,1f}},triangles=new[]{0,1,2},skinWeights=new object[]{new object[]{new object[]{0,1f}},new object[]{new object[]{0,1f}},new object[]{new object[]{0,1f}}},material=new{doubleSided=false,name="magenheim.test.skin",color=new[]{1f,1f,1f,1f},metallic=0f,roughness=.6f,emission=new[]{0f,0f,0f},texture=textureName,normalTexture=(string?)null,normalScale=(float?)null,metallicGlossTexture=(string?)null,emissionTexture=(string?)null},collider=false,crystal=(object?)null}},lights=Array.Empty<object>()});
 File.WriteAllText(path,payload.ToString(Newtonsoft.Json.Formatting.None));
 try{
  var host=new GameObject("skin-donor");var attach=new GameObject("attach_skin");attach.transform.SetParent(host.transform);var hips=new GameObject("Hips");hips.transform.SetParent(attach.transform);
  var donor=attach.AddComponent<SkinnedMeshRenderer>();donor.sharedMesh=new Mesh();donor.sharedMaterial=new Material(Shader.Find("Standard"));donor.bones=new[]{hips.transform};donor.rootBone=hips.transform;
  var loaded=ModelAssets.LoadSkinnedEquipment(host,id);var renderer=loaded.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
  if(donor.enabled||renderer.rootBone!=hips.transform||renderer.bones.Length!=1)throw new Exception("attach_skin binding failed.");
  if(renderer.sharedMesh!.boneWeights.Length!=3||renderer.sharedMesh.bindposes.Length!=1||renderer.sharedMesh.boneWeights.Any(w=>w.boneIndex0!=0||MathF.Abs(w.weight0-1f)>.0001f))throw new Exception("Skin weight stream failed.");
  Console.WriteLine("PASS: wearable skin stream mapped onto donor attach_skin bones.");
 }finally{if(File.Exists(path))File.Delete(path);}
}

var untouched=new GameObject("missing");var original=untouched.AddComponent<MeshRenderer>();original.sharedMaterial=new Material();
try{ModelAssets.Load(untouched,"missing-asset-for-test");throw new Exception("Missing asset silently accepted");}catch(FileNotFoundException){}
if(!original.enabled || untouched.transform.childCount!=0)throw new Exception("Failed load mutated host");
try{ModelAssets.Load(untouched,"../outside");throw new Exception("Traversal accepted");}catch(ArgumentException){}
foreach(var id in new[]{"underworld-standing-stone","underworld-dais"})
{
 var mesh=ModelAssets.LoadSingleMesh(id);
 if(!ReferenceEquals(mesh,ModelAssets.LoadSingleMesh(id)))throw new Exception("Standalone mesh cache failed: "+id);
 if(mesh.vertices.Length==0 || mesh.triangles.Length==0)throw new Exception("Empty standalone mesh: "+id);
}
try{ModelAssets.LoadSingleMesh("../outside");throw new Exception("Standalone traversal accepted");}catch(ArgumentException){}
try{ModelAssets.LoadSingleMesh("crystal-weapon-bow");throw new Exception("Multipart mesh silently truncated");}catch(InvalidDataException){}
Console.WriteLine($"PASS: {count} model assets imported twice; complete parts/UVs, shared meshes, missing-file and path guards.");

GameObject HostForUnsafeShader(string shaderName)
{
 var host=new GameObject("unsafe-shader-host");
 host.AddComponent<Character>();host.AddComponent<ZNetView>();
 host.AddComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find(shaderName));
 var body=new GameObject("body");body.transform.SetParent(host.transform);host.AddComponent<Turret>().m_turretBody=body;
 return host;
}

// HeldModelAlignment regression test. Donor bounds are hardcoded from LogOutput.log (0.0.77, in
// attach space) because the vanilla donor meshes only exist inside the game process -- this shim
// cannot load them. Replacement bounds are computed fresh from the shipped model.json payload each
// run, exactly as ModelAssets/HeldModelAlignment measure them (root parented at identity, scale 1,
// before any rotation), so a source re-export (e.g. the greatsword reshape on 2026-09-19) is picked
// up automatically rather than silently tested against stale numbers.
//
// All nine non-crossbow weapons are now independently confirmed correct (axe, greatsword, bow since
// 0.0.75; battleaxe, spear, mace since the 2026-09-19 tolerant-margin fix; sword since the same-day
// source flip below). All nine must land on ONE identical rotation -- ours X/Y/Z -> donor X/Z/Y with
// signs (+1,+1,-1) -- which this test checks by transforming unit vectors rather than comparing
// Euler angles, since Quaternion.eulerAngles is not implemented in this shim and Euler decomposition
// has its own ambiguities near 90/270 degrees that have nothing to do with correctness.
//
// The sword reproduced the exact mirror of the good pattern for a while, on a real 43.7% margin
// (not measurement noise, unlike every other fix here), and nobody had confirmed in game whether it
// was right or wrong -- so it was measured and printed, not asserted on, rather than forced to match.
// It turned out to be the same defect as the four staves in the next commit: authored back-to-front,
// blade at negative Blender Z where the axe and knife both keep theirs at positive Z. Fixed by
// rotating the source 180 degrees and confirmed by a colour-coded debug render before re-export; it
// now belongs in mustMatchGood like everything else.
{
    var donors = new Dictionary<string, Bounds>
    {
        ["crystal-weapon-sword"]       = new(new Vector3(0f, 0f, 0.503f),        new Vector3(0.195f, 0.059f, 1.288f)),
        ["crystal-weapon-greatsword"]  = new(new Vector3(0f, 0f, 0.625f),        new Vector3(0.245f, 0.052f, 1.911f)),
        ["crystal-weapon-axe"]         = new(new Vector3(0.043f, 0.001f, 0.317f), new Vector3(0.449f, 0.105f, 0.977f)),
        ["crystal-weapon-battleaxe"]   = new(new Vector3(0.152f, -0.01f, 0.604f), new Vector3(0.401f, 0.128f, 1.611f)),
        ["crystal-weapon-mace"]        = new(new Vector3(0.001f, 0.003f, 0.312f), new Vector3(0.436f, 0.147f, 0.944f)),
        ["crystal-weapon-spear"]       = new(new Vector3(-0.004f, -0.013f, 0.189f), new Vector3(0.286f, 0.147f, 2.432f)),
        ["crystal-weapon-knife"]       = new(new Vector3(0.048f, 0f, 0.206f),    new Vector3(0.133f, 0.026f, 0.543f)),
        ["crystal-weapon-atgeir"]      = new(new Vector3(0.268f, -0.1f, 0.64f),  new Vector3(1.23f, 0.453f, 2.803f)),
        ["crystal-weapon-bow"]         = new(new Vector3(-0.065f, -0.003f, 0.001f), new Vector3(0.852f, 0.48f, 1.728f)),
        ["crystal-weapon-crossbow"]    = new(new Vector3(0f, 0.014f, -0.119f),   new Vector3(1.272f, 0.268f, 1.725f)),
    };
    var mustMatchGood = new[]
    {
        "crystal-weapon-axe", "crystal-weapon-greatsword", "crystal-weapon-bow",
        "crystal-weapon-battleaxe", "crystal-weapon-spear", "crystal-weapon-mace",
        "crystal-weapon-knife", "crystal-weapon-atgeir", "crystal-weapon-crossbow",
        "crystal-weapon-sword",
    };
    var fakeAttach = new GameObject("attach").transform;
    var alignmentChecks = 0;
    foreach (var (id, donor) in donors)
    {
        var payload = Path.Combine(AppContext.BaseDirectory, "assets/models/runtime", id + ".model.json");
        var parts = (JArray)JObject.Parse(File.ReadAllText(payload))["parts"]!;
        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        foreach (var part in parts)
            foreach (var v in (JArray)part["vertices"]!)
            {
                var p = new Vector3((float)v[0]!, (float)v[1]!, (float)v[2]!);
                min = new Vector3(Mathf.Min(min.x, p.x), Mathf.Min(min.y, p.y), Mathf.Min(min.z, p.z));
                max = new Vector3(Mathf.Max(max.x, p.x), Mathf.Max(max.y, p.y), Mathf.Max(max.z, p.z));
            }
        var replacement = new Bounds((min + max) * 0.5f, max - min);
        int? forwardAxis = id == "crystal-weapon-crossbow" ? 1 : null;
        var rotation = HeldModelAlignment.Measure(fakeAttach, donor, replacement, forwardAxis);

        bool Aligned(Vector3 a, Vector3 b) => Vector3.Dot(a, b) > 0.99f;
        var good = Aligned(rotation * Vector3.right, Vector3.right)
                && Aligned(rotation * Vector3.up, Vector3.forward)
                && Aligned(rotation * Vector3.forward, -Vector3.up);

        if (Array.IndexOf(mustMatchGood, id) >= 0 && !good)
            throw new Exception($"HeldModelAlignment regression: '{id}' no longer matches the confirmed rotation (ours X/Y/Z -> donor X/Z/Y, signs +1/+1/-1).");
        alignmentChecks++;
    }
    Console.WriteLine($"PASS: {alignmentChecks} held-model alignments checked against the confirmed rotation.");
}

// Cover must remain diverse, nonblocking, and habitat-specific across all supported biomes.
foreach (var biome in Enum.GetValues<Magenheim.Core.Underworld.UnderworldTerrainBiome>())
{

 var names = new HashSet<string>();
 for (var i = 0; i < UnderworldVanillaDonorCatalog.CoverCount(biome); i++)
 {
  var cover = UnderworldVanillaDonorCatalog.SelectCover(biome, i);
  if (!names.Add(cover.Name) || cover.Donor.Collidable) throw new Exception("Invalid cover identity/collision: " + biome);
  if (cover.Donor.MinScale <= 0 || cover.Donor.MaxScale < cover.Donor.MinScale || cover.MinHeightAboveWater > cover.MaxHeightAboveWater)
   throw new Exception("Invalid cover habitat/scale: " + cover.Name);
  if (biome == Magenheim.Core.Underworld.UnderworldTerrainBiome.BlackwaterDeep
      && cover.Name.Contains("fern", StringComparison.OrdinalIgnoreCase)
      && Magenheim.Core.Underworld.UnderworldFloraPlacement.CanPlaceCover(-5, 0, cover.MinHeightAboveWater, cover.MaxHeightAboveWater, cover.MaxSlope))
   throw new Exception("Terrestrial fern admitted underwater");
 }
 if (names.Count < 12) throw new Exception("Incomplete biome cover: " + biome);
}
Console.WriteLine("PASS: biome cover diversity, nonblocking donors and shoreline habitat contracts.");

// Every fungal catalogue identity must resolve to an actual imported model with solid stems.
var canopyIds = new HashSet<string>();
var fungalBiome = Magenheim.Core.Underworld.UnderworldTerrainBiome.FungalForest;
for (var i = 0; i < UnderworldVanillaDonorCatalog.Count(fungalBiome); i++)
{
 var donor = UnderworldVanillaDonorCatalog.Select(fungalBiome, i);
 if (!UnderworldVanillaDonorCatalog.IsModel(donor) || !donor.Collidable)
  throw new Exception("Fungal canopy lost its authored model or collision");
 var id = UnderworldVanillaDonorCatalog.ModelId(donor);
 var path = Path.Combine(AppContext.BaseDirectory, "assets/models/runtime", id + ".model.json");
 if (!File.Exists(path)) throw new Exception("Unpackaged fungal canopy: " + id);
 canopyIds.Add(id);
}
if (canopyIds.Count != 16) throw new Exception("Fungal forest must contain sixteen distinct canopy models");
Console.WriteLine("PASS: all sixteen fungal canopy models reachable through the runtime donor catalogue.");

// New surface masses resolve through the real importer and stay separate from solid scenery.
var groundIds = new HashSet<string>();
foreach (var biome in Enum.GetValues<Magenheim.Core.Underworld.UnderworldTerrainBiome>())
for (var i = 0; i < UnderworldVanillaDonorCatalog.GroundFeatureCount(biome); i++)
{
 var cover = UnderworldVanillaDonorCatalog.SelectCover(biome, i);
 var id = UnderworldVanillaDonorCatalog.ModelId(cover.Donor);
 if (!id.StartsWith("underworld-ground-")) throw new Exception("Missing native ground feature: " + biome);
 var host = new GameObject(id);
 ModelAssets.Load(host, id, hideOriginal: false);
 if (host.GetComponentsInChildren<Collider>(true).Length != 0) throw new Exception("Ground cover blocks movement: " + id);
 groundIds.Add(id);
}
if (groundIds.Count != 10) throw new Exception("Expected ten distinct biome ground features.");
Console.WriteLine("PASS: all ten ground feature models imported through their biome palette without collision.");
