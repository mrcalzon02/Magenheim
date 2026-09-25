using UnityEngine;
using Magenheim.Runtime;
using Newtonsoft.Json.Linq;
var count=0;
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
