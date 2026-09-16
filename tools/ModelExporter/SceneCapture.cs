using System.Text.Json;
using UnityEngine;
namespace Magenheim.ModelExporter;
internal static class SceneCapture
{
    internal static void Write(GameObject root,string path)
    {
        var parts=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh is { vertices.Length:>0 }).Select((f,index)=>{
            var mesh=f.sharedMesh!;var material=f.gameObject.GetComponent<Renderer>()?.sharedMaterial ?? new Material();
            var tex=material.mainTexture as Texture2D;
            var emission=material.Colors.GetValueOrDefault("_EmissionColor",Color.black);
            var c=f.gameObject.GetComponent<Magenheim.Runtime.CapturedCrystal>();
            return new {collider=f.gameObject.GetComponent<Collider>()?.enabled??false,crystal=c==null?null:new {c.id,c.function,c.health},name=f.gameObject.name,path=NodePath(f.transform,root.transform),
                vertices=mesh.vertices.Select(v=>{var w=f.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),
                triangles=mesh.triangles,uv=mesh.uv.Select(v=>new[]{v.x,v.y}).ToArray(),
                material=new {name=material.name,color=new[]{material.color.r,material.color.g,material.color.b,material.color.a},
                    metallic=material.Floats.GetValueOrDefault("_Metallic",0),roughness=1-material.Floats.GetValueOrDefault("_Glossiness",.3f),
                    emission=new[]{emission.r,emission.g,emission.b},
                    texture=tex is {pixels.Length:>0}?new {name=tex.name,width=tex.width,height=tex.height,pixels=tex.pixels.SelectMany(c=>new[]{c.r,c.g,c.b,c.a}).ToArray()}:null}};
        }).ToArray();
        File.WriteAllText(path,JsonSerializer.Serialize(new {name=root.name,parts,lights=root.GetComponentsInChildren<Light>(true).Select(l=>new {path=NodePath(l.transform,root.transform),position=new[]{l.transform.TransformPoint(Vector3.zero).x,l.transform.TransformPoint(Vector3.zero).y,l.transform.TransformPoint(Vector3.zero).z},color=new[]{l.color.r,l.color.g,l.color.b},l.range,l.intensity}).ToArray()}));
    }
    private static string NodePath(Transform t,Transform root)=>t==root?"":(t.parent==null||t.parent==root?"":NodePath(t.parent,root)+"/")+t.gameObject.name;
}
