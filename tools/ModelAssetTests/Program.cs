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
