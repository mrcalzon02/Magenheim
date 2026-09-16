using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Owned Magenheim geometry for the four Fire staff tiers.</summary>
internal static class FireStaffVisuals
{
    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static void Apply(GameObject prefab, string prefabName)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on Fire staff prefab '{prefab.name}'.");

        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + prefabName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var charred = Material(source, "charred-wood", new Color(.18f,.10f,.065f,1f), 0f, .10f);
        var darkWood = Material(source, "dark-wood", new Color(.24f,.13f,.075f,1f), 0f, .12f);
        var bronze = Material(source, "bronze", new Color(.54f,.31f,.12f,1f), .48f, .28f);
        var iron = Material(source, "iron", new Color(.34f,.31f,.29f,1f), .52f, .24f);
        var blackMetal = Material(source, "blackmetal", new Color(.11f,.10f,.095f,1f), .76f, .32f);
        var ember = Material(source, "ember-crystal", new Color(1f,.30f,.035f,1f), .02f, .70f, .48f);
        var flame = Material(source, "flame-crystal", new Color(1f,.58f,.08f,1f), .02f, .78f, .62f);
        var hot = Material(source, "white-hot-crystal", new Color(1f,.90f,.48f,1f), .01f, .86f, .78f);

        switch (prefabName)
        {
            case "Magenheim_Staff_Fire_Simple": BuildSimple(root, darkWood, bronze, ember); break;
            case "Magenheim_Staff_Fire_Crystal": BuildCrystal(root, charred, iron, flame); break;
            case "Magenheim_Staff_Fire_Advanced": BuildAdvanced(root, charred, iron, blackMetal, flame, ember); break;
            case "Magenheim_Staff_Fire_Master": BuildMaster(root, charred, blackMetal, flame, hot); break;
            default: throw new InvalidOperationException($"Unknown Fire staff model '{prefabName}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void BuildSimple(GameObject root, Material wood, Material metal, Material fire)
    {
        Shaft(root, wood, metal, .036f);
        Box(root, "left-fork", new Vector3(-.07f,.72f,0), new Vector3(.050f,.34f,.050f), wood, -16f);
        Box(root, "right-fork", new Vector3(.07f,.72f,0), new Vector3(.050f,.34f,.050f), wood, 16f);
        Cylinder(root, "fork-band", new Vector3(0,.59f,0), .056f,.060f,10,metal);
        Prism(root, "ember-focus", new Vector3(0,.82f,0), .076f,.30f,6,fire);
    }

    private static void BuildCrystal(GameObject root, Material wood, Material metal, Material fire)
    {
        Shaft(root, wood, metal, .039f);
        Cylinder(root, "neck-collar", new Vector3(0,.59f,0), .070f,.070f,12,metal);
        Box(root, "crossbar", new Vector3(0,.72f,0), new Vector3(.30f,.038f,.044f), metal);
        Box(root, "left-prong", new Vector3(-.12f,.82f,0), new Vector3(.038f,.30f,.038f), metal, -20f);
        Box(root, "right-prong", new Vector3(.12f,.82f,0), new Vector3(.038f,.30f,.038f), metal, 20f);
        Prism(root, "flame-focus", new Vector3(0,.88f,0), .098f,.42f,6,fire);
    }

    private static void BuildAdvanced(GameObject root, Material wood, Material iron, Material blackMetal, Material fire, Material ember)
    {
        Shaft(root, wood, blackMetal, .042f);
        Cylinder(root, "lower-band", new Vector3(0,.26f,0), .061f,.045f,12,iron);
        Cylinder(root, "crown-band", new Vector3(0,.61f,0), .085f,.070f,12,blackMetal);
        for (var i=0;i<5;i++)
        {
            var angle = Mathf.PI * 2f * i / 5f;
            var x = .17f * Mathf.Cos(angle);
            var z = .17f * Mathf.Sin(angle);
            Box(root, "flame-rib-" + i, new Vector3(x,.83f,z), new Vector3(.036f,.36f,.036f), iron, 11f*Mathf.Cos(angle));
            Prism(root, "flame-tip-" + i, new Vector3(x,.99f,z), .029f,.13f,5,ember);
        }
        Prism(root, "advanced-core", new Vector3(0,.90f,0), .115f,.49f,6,fire);
    }

    private static void BuildMaster(GameObject root, Material wood, Material metal, Material fire, Material hot)
    {
        Shaft(root, wood, metal, .045f);
        foreach (var y in new[] { -.48f,-.08f,.30f,.57f })
            Cylinder(root, "shaft-band-" + y, new Vector3(0,y,0), .066f,.046f,14,metal);
        Cylinder(root, "crown-halo", new Vector3(0,.72f,0), .22f,.052f,18,metal);
        for (var i=0;i<6;i++)
        {
            var angle=Mathf.PI*2f*i/6f;
            var x=.18f*Mathf.Cos(angle); var z=.18f*Mathf.Sin(angle);
            Box(root,"crown-rib-"+i,new Vector3(x,.88f,z),new Vector3(.038f,.43f,.038f),metal,12f*Mathf.Cos(angle));
            Prism(root,"crown-flame-"+i,new Vector3(x,1.09f,z),.030f,.15f,5,fire);
        }
        Prism(root,"master-core",new Vector3(0,.96f,0),.140f,.58f,7,hot);
    }

    private static void Shaft(GameObject root, Material wood, Material metal, float radius)
    {
        Cylinder(root,"shaft",new Vector3(0,-.08f,0),radius,1.48f,10,wood);
        Cylinder(root,"pommel",new Vector3(0,-.77f,0),radius*1.38f,.10f,10,metal);
        Cylinder(root,"neck-band",new Vector3(0,.50f,0),radius*1.32f,.055f,10,metal);
    }

    private static void Box(GameObject root,string name,Vector3 position,Vector3 size,Material material,float zDegrees=0f) =>
        Add(root,name,BoxMesh,position,size,Quaternion.Euler(0,0,zDegrees),material);

    private static void Cylinder(GameObject root,string name,Vector3 position,float radius,float height,int sides,Material material)
    {
        if(!CylinderMeshes.TryGetValue(sides,out var mesh)){mesh=CreateCylinderMesh(sides);CylinderMeshes.Add(sides,mesh);}
        Add(root,name,mesh,position,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,material);
    }

    private static void Prism(GameObject root,string name,Vector3 position,float radius,float height,int sides,Material material)
    {
        if(!PrismMeshes.TryGetValue(sides,out var mesh)){mesh=CreatePrismMesh(sides);PrismMeshes.Add(sides,mesh);}
        Add(root,name,mesh,position,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,material);
    }

    private static void Add(GameObject root,string name,Mesh mesh,Vector3 position,Vector3 scale,Quaternion rotation,Material material)
    {
        var part=new GameObject(name){layer=root.layer};
        part.transform.SetParent(root.transform,false);
        part.transform.localPosition=position;
        part.transform.localScale=scale;
        part.transform.localRotation=rotation;
        part.AddComponent<MeshFilter>().sharedMesh=mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial=material;
    }

    private static Material Material(Material source,string suffix,Color color,float metallic,float gloss,float emission=0f)
    {
        var material=new Material(source){name="magenheim.fire-staff."+suffix};
        GeneratedSurfaceTextures.Apply(material, suffix);
        if(material.HasProperty("_Color"))material.SetColor("_Color",color);
        if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",metallic);
        if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",gloss);
        if(material.HasProperty("_BumpMap"))material.SetTexture("_BumpMap",null);
        material.DisableKeyword("_NORMALMAP");
        if(material.HasProperty("_EmissionColor")&&emission>0f){material.SetColor("_EmissionColor",color*emission);material.EnableKeyword("_EMISSION");}
        else material.DisableKeyword("_EMISSION");
        material.SetOverrideTag("RenderType","Opaque");
        if(material.HasProperty("_ZWrite"))material.SetFloat("_ZWrite",1f);
        material.renderQueue=2000;
        return material;
    }

    private static Mesh CreateBoxMesh()
    {
        var m=new Mesh{name="magenheim.fire-staff.box"};
        m.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
        m.triangles=new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2};
        m.RecalculateNormals();m.RecalculateBounds();return m;
    }

    private static Mesh CreateCylinderMesh(int sides)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(var r=0;r<2;r++){var y=r==0?-.5f:.5f;for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),y,.5f*Mathf.Sin(a)));}}
        var bottom=v.Count;v.Add(new Vector3(0,-.5f,0));var top=v.Count;v.Add(new Vector3(0,.5f,0));
        for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{i,sides+i,n,n,sides+i,sides+n,bottom,i,n,top,sides+n,sides+i});}
        var m=new Mesh{name="magenheim.fire-staff.cylinder."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;
    }

    private static Mesh CreatePrismMesh(int sides)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.36f*Mathf.Cos(a),-.45f,.36f*Mathf.Sin(a)));}
        for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),.20f,.5f*Mathf.Sin(a)));}
        var top=v.Count;v.Add(new Vector3(0,.68f,0));var bottom=v.Count;v.Add(new Vector3(0,-.50f,0));
        for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{bottom,i,n,i,sides+i,n,n,sides+i,sides+n,top,sides+n,sides+i});}
        var m=new Mesh{name="magenheim.fire-staff.prism."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;
    }
}
