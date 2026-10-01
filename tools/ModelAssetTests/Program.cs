using UnityEngine;
using Magenheim.Runtime;
using Newtonsoft.Json.Linq;
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
foreach(var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"assets/models/runtime"),"*.model.json")) {
 var id=Path.GetFileName(file).Replace(".model.json","");
 GameObject Host(){var h=new GameObject(id);h.AddComponent<Character>();h.AddComponent<ZNetView>();h.AddComponent<MeshRenderer>().sharedMaterial=new Material();var b=new GameObject("body");b.transform.SetParent(h.transform);h.AddComponent<Turret>().m_turretBody=b;return h;}
 var first=Host();var second=Host();ModelAssets.Load(first,id);ModelAssets.Load(second,id);
 var a=first.GetComponentsInChildren<MeshFilter>(true);var b=second.GetComponentsInChildren<MeshFilter>(true);
 var expected=((JArray)JObject.Parse(File.ReadAllText(file))["parts"]!).Count;
 if(a.Length!=expected || b.Length!=expected)throw new Exception("Part loss: "+id);
 for(int i=0;i<a.Length;i++){if(!ReferenceEquals(a[i].sharedMesh,b[i].sharedMesh))throw new Exception("Mesh cache failed: "+id);if(a[i].sharedMesh!.uv.Length!=a[i].sharedMesh!.vertices.Length)throw new Exception("UV loss: "+id);}
 if(first.GetComponent<MeshRenderer>()!.enabled)throw new Exception("Inherited geometry still visible: "+id);
 count++;
}
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
