using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Owned Magenheim geometry for the four Storm staff tiers.</summary>
internal static class StormStaffVisuals
{
    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> Cylinders = new();
    private static readonly Dictionary<int, Mesh> Prisms = new();

    internal static void Apply(GameObject prefab, string prefabName)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(r => r.sharedMaterial).FirstOrDefault(m => m)
            ?? throw new InvalidOperationException($"No material source exists on Storm staff prefab '{prefab.name}'.");

        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + prefabName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var darkWood = Mat(source,"dark-wood",new Color(.18f,.14f,.11f,1f),0f,.11f);
        var ashWood = Mat(source,"ash-wood",new Color(.30f,.25f,.20f,1f),0f,.14f);
        var copper = Mat(source,"copper-metal",new Color(.55f,.31f,.18f,1f),.52f,.31f);
        var silver = Mat(source,"silver",new Color(.63f,.70f,.75f,1f),.70f,.38f);
        var blackMetal = Mat(source,"blackmetal",new Color(.12f,.15f,.18f,1f),.78f,.34f);
        var storm = Mat(source,"storm-crystal",new Color(.28f,.62f,1f,1f),.02f,.76f,.50f);
        var bright = Mat(source,"storm-bright-crystal",new Color(.72f,.90f,1f,1f),.01f,.88f,.72f);

        switch (prefabName)
        {
            case "Magenheim_Staff_Storm_Simple": BuildSimple(root, ashWood, copper, storm); break;
            case "Magenheim_Staff_Storm_Crystal": BuildCrystal(root, darkWood, silver, bright); break;
            case "Magenheim_Staff_Storm_Advanced": BuildAdvanced(root, darkWood, silver, blackMetal, storm, bright); break;
            case "Magenheim_Staff_Storm_Master": BuildMaster(root, darkWood, blackMetal, bright); break;
            default: throw new InvalidOperationException($"Unknown Storm staff model '{prefabName}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void BuildSimple(GameObject root, Material wood, Material metal, Material crystal)
    {
        Shaft(root,wood,metal,.036f);
        Box(root,"left-conductor",new Vector3(-.08f,.74f,0),new Vector3(.040f,.36f,.040f),metal,-14f);
        Box(root,"right-conductor",new Vector3(.08f,.74f,0),new Vector3(.040f,.36f,.040f),metal,14f);
        Cylinder(root,"conductor-band",new Vector3(0,.59f,0),.060f,.060f,10,metal);
        Prism(root,"static-focus",new Vector3(0,.83f,0),.075f,.29f,6,crystal);
    }

    private static void BuildCrystal(GameObject root, Material wood, Material metal, Material crystal)
    {
        Shaft(root,wood,metal,.039f);
        Cylinder(root,"neck-ring",new Vector3(0,.60f,0),.075f,.060f,12,metal);
        Box(root,"left-fork",new Vector3(-.13f,.82f,0),new Vector3(.035f,.44f,.035f),metal,-17f);
        Box(root,"right-fork",new Vector3(.13f,.82f,0),new Vector3(.035f,.44f,.035f),metal,17f);
        Box(root,"bridge",new Vector3(0,.69f,0),new Vector3(.30f,.035f,.040f),metal);
        Prism(root,"bolt-focus",new Vector3(0,.91f,0),.092f,.43f,6,crystal);
    }

    private static void BuildAdvanced(GameObject root, Material wood, Material silver, Material blackMetal, Material storm, Material bright)
    {
        Shaft(root,wood,blackMetal,.042f);
        Cylinder(root,"crown-hub",new Vector3(0,.67f,0),.095f,.072f,14,blackMetal);
        for(var i=0;i<4;i++)
        {
            var a=Mathf.PI*.5f*i;
            var x=.18f*Mathf.Cos(a); var z=.18f*Mathf.Sin(a);
            Box(root,"arc-prong-"+i,new Vector3(x,.88f,z),new Vector3(.036f,.42f,.036f),silver,12f*Mathf.Cos(a));
            Prism(root,"arc-tip-"+i,new Vector3(x,1.08f,z),.027f,.12f,5,bright);
        }
        Prism(root,"arc-core",new Vector3(0,.91f,0),.115f,.46f,6,storm);
    }

    private static void BuildMaster(GameObject root, Material wood, Material metal, Material crystal)
    {
        Shaft(root,wood,metal,.045f);
        foreach(var y in new[]{-.48f,-.12f,.24f,.55f}) Cylinder(root,"shaft-ring-"+y,new Vector3(0,y,0),.067f,.045f,14,metal);
        Cylinder(root,"storm-halo",new Vector3(0,.74f,0),.235f,.050f,20,metal);
        for(var i=0;i<6;i++)
        {
            var a=Mathf.PI*2f*i/6f;
            var x=.19f*Mathf.Cos(a); var z=.19f*Mathf.Sin(a);
            Box(root,"antenna-"+i,new Vector3(x,.89f,z),new Vector3(.035f,.46f,.035f),metal,13f*Mathf.Cos(a));
            Prism(root,"antenna-tip-"+i,new Vector3(x,1.12f,z),.030f,.14f,5,crystal);
        }
        Prism(root,"thunderhead-core",new Vector3(0,.98f,0),.145f,.60f,8,crystal);
    }

    private static void Shaft(GameObject root,Material wood,Material metal,float radius)
    {
        Cylinder(root,"shaft",new Vector3(0,-.08f,0),radius,1.48f,10,wood);
        Cylinder(root,"pommel",new Vector3(0,-.77f,0),radius*1.38f,.10f,10,metal);
        Cylinder(root,"neck",new Vector3(0,.50f,0),radius*1.34f,.055f,10,metal);
    }

    private static void Box(GameObject root,string name,Vector3 p,Vector3 s,Material m,float z=0f)=>Add(root,name,BoxMesh,p,s,Quaternion.Euler(0,0,z),m);
    private static void Cylinder(GameObject root,string name,Vector3 p,float radius,float height,int sides,Material m)
    {
        if(!Cylinders.TryGetValue(sides,out var mesh)){mesh=MakeCylinder(sides);Cylinders.Add(sides,mesh);} Add(root,name,mesh,p,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,m);
    }
    private static void Prism(GameObject root,string name,Vector3 p,float radius,float height,int sides,Material m)
    {
        if(!Prisms.TryGetValue(sides,out var mesh)){mesh=MakePrism(sides);Prisms.Add(sides,mesh);} Add(root,name,mesh,p,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,m);
    }
    private static void Add(GameObject root,string name,Mesh mesh,Vector3 p,Vector3 s,Quaternion r,Material m)
    {
        var part=new GameObject(name){layer=root.layer};part.transform.SetParent(root.transform,false);part.transform.localPosition=p;part.transform.localScale=s;part.transform.localRotation=r;part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=m;
    }
    private static Material Mat(Material source,string suffix,Color c,float metallic,float gloss,float emission=0f)
    {
        var m=new Material(source){name="magenheim.storm-staff."+suffix};GeneratedSurfaceTextures.Apply(m,suffix);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",gloss);if(m.HasProperty("_BumpMap"))m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");if(m.HasProperty("_EmissionColor")&&emission>0f){m.SetColor("_EmissionColor",c*emission);m.EnableKeyword("_EMISSION");}else m.DisableKeyword("_EMISSION");m.SetOverrideTag("RenderType","Opaque");if(m.HasProperty("_ZWrite"))m.SetFloat("_ZWrite",1f);m.renderQueue=2000;return m;
    }
    private static Mesh CreateBoxMesh(){var m=new Mesh{name="magenheim.storm-staff.box"};m.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};m.triangles=new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2};m.RecalculateNormals();m.RecalculateBounds();return m;}
    private static Mesh MakeCylinder(int sides){var v=new List<Vector3>();var t=new List<int>();for(var r=0;r<2;r++){var y=r==0?-.5f:.5f;for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),y,.5f*Mathf.Sin(a)));}}var b=v.Count;v.Add(new Vector3(0,-.5f,0));var top=v.Count;v.Add(new Vector3(0,.5f,0));for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{i,sides+i,n,n,sides+i,sides+n,b,i,n,top,sides+n,sides+i});}var m=new Mesh{name="magenheim.storm-staff.cylinder."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
    private static Mesh MakePrism(int sides){var v=new List<Vector3>();var t=new List<int>();for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.36f*Mathf.Cos(a),-.45f,.36f*Mathf.Sin(a)));}for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),.20f,.5f*Mathf.Sin(a)));}var top=v.Count;v.Add(new Vector3(0,.68f,0));var bottom=v.Count;v.Add(new Vector3(0,-.50f,0));for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{bottom,i,n,i,sides+i,n,n,sides+i,sides+n,top,sides+n,sides+i});}var m=new Mesh{name="magenheim.storm-staff.prism."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
}
