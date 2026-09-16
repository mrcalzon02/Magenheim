using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Magenheim.Runtime;
internal static class RadianceVisuals
{
    private static readonly Mesh Box = MakeBox();
    private static readonly Dictionary<int, Mesh> Cylinders = new();
    private static readonly Dictionary<int, Mesh> Prisms = new();

    internal static void Apply(GameObject prefab, string assetName)
    {
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(x => x.sharedMaterial).FirstOrDefault(x => x)
            ?? throw new InvalidOperationException($"No material source exists on Radiance staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + assetName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var wood = Mat(source,"wood",new Color(.24f,.17f,.12f,1f),0f,.13f);
        var ivory = Mat(source,"ivory",new Color(.78f,.72f,.58f,1f),.03f,.23f);
        var bronze = Mat(source,"bronze",new Color(.62f,.43f,.19f,1f),.46f,.28f);
        var gold = Mat(source,"gold",new Color(.90f,.65f,.18f,1f),.68f,.43f);
        var light = Mat(source,"radiance-light-crystal",new Color(1f,.91f,.48f,1f),.02f,.78f,.42f);
        var white = Mat(source,"radiance-white-crystal",new Color(1f,.99f,.86f,1f),.01f,.88f,.60f);

        if (assetName == "staff-radiance-simple")
        {
            Shaft(root,wood,bronze,.034f); Part(root,"cross",Box,new Vector3(0,.69f,0),new Vector3(.26f,.036f,.038f),Quaternion.identity,bronze);
            Prism(root,"spark",new Vector3(0,.88f,0),.075f,.24f,6,light);
        }
        else if (assetName == "staff-radiance-crystal")
        {
            Shaft(root,ivory,gold,.038f); Cylinder(root,"ring",new Vector3(0,.64f,0),.12f,.04f,16,gold);
            for (var i=0;i<4;i++){ var a=i*Mathf.PI/2f; Part(root,"ray"+i,Box,new Vector3(.13f*Mathf.Cos(a),.78f,.13f*Mathf.Sin(a)),new Vector3(.03f,.30f,.03f),Quaternion.Euler(0,0,i%2==0?11f:-11f),gold); }
            Prism(root,"focus",new Vector3(0,.90f,0),.095f,.42f,6,light);
        }
        else if (assetName == "staff-radiance-advanced")
        {
            Shaft(root,wood,gold,.041f); Cylinder(root,"hub",new Vector3(0,.76f,0),.095f,.065f,14,gold);
            for (var i=0;i<8;i++){ var a=i*Mathf.PI/4f; var p=new Vector3(.19f*Mathf.Cos(a),.83f,.19f*Mathf.Sin(a)); Part(root,"corona"+i,Box,p,new Vector3(.028f,.34f,.028f),Quaternion.Euler(0,0,14f*Mathf.Cos(a)),gold); Prism(root,"tip"+i,p+Vector3.up*.17f,.025f,.11f,5,i%2==0?light:white); }
            Prism(root,"core",new Vector3(0,.91f,0),.105f,.36f,6,white);
        }
        else if (assetName == "staff-radiance-master")
        {
            Shaft(root,ivory,gold,.044f); foreach(var y in new[]{-.46f,-.10f,.26f,.53f}) Cylinder(root,"band"+y,new Vector3(0,y,0),.064f,.042f,14,gold);
            for (var i=0;i<12;i++){ var a=i*Mathf.PI/6f; Part(root,"daybreak"+i,Box,new Vector3(.23f*Mathf.Cos(a),.82f,.23f*Mathf.Sin(a)),new Vector3(.026f,.38f,.026f),Quaternion.Euler(0,0,18f*Mathf.Cos(a)),gold); }
            Cylinder(root,"halo",new Vector3(0,.78f,0),.24f,.045f,20,gold); Prism(root,"core",new Vector3(0,.95f,0),.135f,.52f,8,white); Prism(root,"heart",new Vector3(0,1.08f,0),.07f,.22f,6,light);
        }
        else throw new InvalidOperationException($"Unknown Radiance geometry '{assetName}'.");

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void Shaft(GameObject root, Material body, Material metal, float radius)
    {
        Cylinder(root,"shaft",new Vector3(0,-.08f,0),radius,1.48f,10,body);
        Cylinder(root,"pommel",new Vector3(0,-.77f,0),radius*1.38f,.10f,12,metal);
        Cylinder(root,"neck",new Vector3(0,.53f,0),radius*1.38f,.06f,12,metal);
    }
    private static void Cylinder(GameObject root,string name,Vector3 pos,float radius,float height,int sides,Material material)
    {
        if(!Cylinders.TryGetValue(sides,out var mesh)){mesh=MakeCylinder(sides);Cylinders.Add(sides,mesh);} Part(root,name,mesh,pos,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,material);
    }
    private static void Prism(GameObject root,string name,Vector3 pos,float radius,float height,int sides,Material material)
    {
        if(!Prisms.TryGetValue(sides,out var mesh)){mesh=MakePrism(sides);Prisms.Add(sides,mesh);} Part(root,name,mesh,pos,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,material);
    }
    private static void Part(GameObject root,string name,Mesh mesh,Vector3 pos,Vector3 scale,Quaternion rot,Material mat)
    {
        var go=new GameObject(name){layer=root.layer}; go.transform.SetParent(root.transform,false); go.transform.localPosition=pos; go.transform.localRotation=rot; go.transform.localScale=scale; go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }
    private static Material Mat(Material source,string name,Color color,float metallic,float gloss,float emission=0f)
    {
        var m=new Material(source){name="magenheim.radiance."+name}; GeneratedSurfaceTextures.Apply(m,name); if(m.HasProperty("_Color"))m.SetColor("_Color",color); if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic); if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",gloss); if(m.HasProperty("_BumpMap"))m.SetTexture("_BumpMap",null); m.DisableKeyword("_NORMALMAP"); if(m.HasProperty("_EmissionColor")&&emission>0f){m.SetColor("_EmissionColor",color*emission);m.EnableKeyword("_EMISSION");} else m.DisableKeyword("_EMISSION"); m.SetOverrideTag("RenderType","Opaque"); if(m.HasProperty("_ZWrite"))m.SetFloat("_ZWrite",1f); m.renderQueue=2000; return m;
    }
    private static Mesh MakeBox(){var m=new Mesh{name="magenheim.radiance.box"};m.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};m.triangles=new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2};m.RecalculateNormals();m.RecalculateBounds();return m;}
    private static Mesh MakeCylinder(int sides){var v=new List<Vector3>();var t=new List<int>();for(var r=0;r<2;r++){var y=r==0?-.5f:.5f;for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),y,.5f*Mathf.Sin(a)));}}var b=v.Count;v.Add(new Vector3(0,-.5f,0));var top=v.Count;v.Add(new Vector3(0,.5f,0));for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{i,sides+i,n,n,sides+i,sides+n,b,i,n,top,sides+n,sides+i});}var m=new Mesh{name="magenheim.radiance.cylinder."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
    private static Mesh MakePrism(int sides){var v=new List<Vector3>();var t=new List<int>();for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.34f*Mathf.Cos(a),-.45f,.34f*Mathf.Sin(a)));}for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),.18f,.5f*Mathf.Sin(a)));}var top=v.Count;v.Add(new Vector3(0,.68f,0));var bottom=v.Count;v.Add(new Vector3(0,-.5f,0));for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{bottom,i,n,i,sides+i,n,n,sides+i,sides+n,top,sides+n,sides+i});}var m=new Mesh{name="magenheim.radiance.prism."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
}
