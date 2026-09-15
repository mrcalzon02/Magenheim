using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Original Magenheim geometry for the four Venom staff tiers.</summary>
internal static class VenomStaffVisuals
{
    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static void Apply(GameObject prefab, string assetName)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        var originalRenderers = prefab.GetComponentsInChildren<Renderer>(true);
        var sourceMaterial = originalRenderers.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on Venom staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + assetName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var wood = MakeMaterial(sourceMaterial, "wood", new Color(.25f,.17f,.10f,1f), 0f, .10f);
        var elder = MakeMaterial(sourceMaterial, "elder", new Color(.20f,.14f,.11f,1f), 0f, .11f);
        var iron = MakeMaterial(sourceMaterial, "iron", new Color(.29f,.34f,.30f,1f), .42f, .20f);
        var black = MakeMaterial(sourceMaterial, "blackmetal", new Color(.12f,.16f,.13f,1f), .72f, .30f);
        var venom = MakeMaterial(sourceMaterial, "venom", new Color(.37f,.80f,.20f,1f), .04f, .55f, .20f);
        var venomBright = MakeMaterial(sourceMaterial, "venom-bright", new Color(.63f,.94f,.35f,1f), .03f, .65f, .32f);
        var venomDeep = MakeMaterial(sourceMaterial, "venom-deep", new Color(.16f,.43f,.13f,1f), .05f, .35f, .10f);

        switch (assetName)
        {
            case "staff-venom-simple": BuildSimple(root, wood, iron, venom); break;
            case "staff-venom-crystal": BuildCrystal(root, elder, iron, venomBright); break;
            case "staff-venom-advanced": BuildAdvanced(root, elder, black, venom, venomDeep); break;
            case "staff-venom-master": BuildMaster(root, elder, black, venomBright, venomDeep); break;
            default: throw new InvalidOperationException($"Unknown Venom staff geometry '{assetName}'.");
        }

        foreach (var renderer in originalRenderers) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void BuildSimple(GameObject root, Material wood, Material metal, Material venom)
    {
        Shaft(root, wood, metal, .035f);
        Box(root, "left-hook", new Vector3(-.07f,.70f,0), new Vector3(.052f,.34f,.052f), wood, -18f);
        Box(root, "right-hook", new Vector3(.07f,.70f,0), new Vector3(.052f,.34f,.052f), wood, 18f);
        Cylinder(root, "binding", new Vector3(0,.58f,0), .052f,.06f,10,metal);
        Prism(root, "venom-seed", new Vector3(0,.82f,0), .075f,.28f,6,venom);
    }

    private static void BuildCrystal(GameObject root, Material wood, Material metal, Material venom)
    {
        Shaft(root, wood, metal, .039f);
        foreach (var y in new[] { .30f,.48f,.64f }) Cylinder(root,"coil-"+y,new Vector3(0,y,0),.070f,.035f,12,metal);
        Box(root,"left-tine",new Vector3(-.12f,.76f,0),new Vector3(.035f,.29f,.035f),metal,-23f);
        Box(root,"right-tine",new Vector3(.12f,.76f,0),new Vector3(.035f,.29f,.035f),metal,23f);
        Prism(root,"venom-heart",new Vector3(0,.84f,0),.10f,.40f,6,venom);
    }

    private static void BuildAdvanced(GameObject root, Material wood, Material metal, Material venom, Material deep)
    {
        Shaft(root, wood, metal, .042f);
        Cylinder(root,"crown-band",new Vector3(0,.61f,0),.082f,.07f,12,metal);
        for (var i=0;i<8;i++)
        {
            var angle=Mathf.PI*2f*i/8f;
            var x=.18f*Mathf.Cos(angle); var z=.18f*Mathf.Sin(angle);
            Box(root,"thorn-"+i,new Vector3(x,.78f,z),new Vector3(.035f,.30f,.035f),metal,8f*Mathf.Cos(angle));
            Prism(root,"thorn-tip-"+i,new Vector3(x,.96f,z),.025f,.11f,5,deep);
        }
        Prism(root,"advanced-core",new Vector3(0,.87f,0),.11f,.46f,6,venom);
    }

    private static void BuildMaster(GameObject root, Material wood, Material metal, Material venom, Material deep)
    {
        Shaft(root, wood, metal, .045f);
        foreach (var y in new[] { -.48f,-.10f,.28f,.58f }) Cylinder(root,"shaft-band-"+y,new Vector3(0,y,0),.065f,.045f,12,metal);
        Cylinder(root,"master-halo",new Vector3(0,.70f,0),.20f,.05f,14,metal);
        for (var i=0;i<6;i++)
        {
            var angle=Mathf.PI*2f*i/6f;
            var x=.17f*Mathf.Cos(angle); var z=.17f*Mathf.Sin(angle);
            Box(root,"rib-"+i,new Vector3(x,.84f,z),new Vector3(.038f,.42f,.038f),metal,10f*Mathf.Cos(angle));
            Prism(root,"rib-crystal-"+i,new Vector3(x,1.05f,z),.027f,.13f,5,deep);
        }
        Prism(root,"master-core",new Vector3(0,.91f,0),.13f,.55f,6,venom);
    }

    private static void Shaft(GameObject root, Material wood, Material metal, float radius)
    {
        Cylinder(root,"shaft",new Vector3(0,-.08f,0),radius,1.48f,10,wood);
        Cylinder(root,"pommel",new Vector3(0,-.77f,0),radius*1.35f,.10f,10,metal);
        Cylinder(root,"neck-band",new Vector3(0,.50f,0),radius*1.30f,.055f,10,metal);
    }

    private static void Box(GameObject root,string name,Vector3 position,Vector3 size,Material material,float zDegrees=0f) =>
        AddPart(root,name,BoxMesh,position,size,Quaternion.Euler(0,0,zDegrees),material);

    private static void Cylinder(GameObject root,string name,Vector3 position,float radius,float height,int sides,Material material)
    {
        if (!CylinderMeshes.TryGetValue(sides,out var mesh)) { mesh=CreateCylinderMesh(sides); CylinderMeshes.Add(sides,mesh); }
        AddPart(root,name,mesh,position,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,material);
    }

    private static void Prism(GameObject root,string name,Vector3 position,float radius,float height,int sides,Material material)
    {
        if (!PrismMeshes.TryGetValue(sides,out var mesh)) { mesh=CreatePrismMesh(sides); PrismMeshes.Add(sides,mesh); }
        AddPart(root,name,mesh,position,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,material);
    }

    private static void AddPart(GameObject root,string name,Mesh mesh,Vector3 position,Vector3 scale,Quaternion rotation,Material material)
    {
        var part=new GameObject(name){layer=root.layer};
        part.transform.SetParent(root.transform,false); part.transform.localPosition=position; part.transform.localScale=scale; part.transform.localRotation=rotation;
        part.AddComponent<MeshFilter>().sharedMesh=mesh; part.AddComponent<MeshRenderer>().sharedMaterial=material;
    }

    private static Material MakeMaterial(Material source,string suffix,Color color,float metallic,float glossiness,float emission=0f)
    {
        var material=new Material(source){name="magenheim.venom-staff."+suffix};
        material.mainTexture=Texture2D.whiteTexture;
        material.mainTextureScale=Vector2.one;
        material.mainTextureOffset=Vector2.zero;
        if(material.HasProperty("_Color")) material.SetColor("_Color",color);
        if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
        if(material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",glossiness);
        if(material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap",null);
        material.DisableKeyword("_NORMALMAP");
        if(material.HasProperty("_EmissionColor") && emission>0f){ material.SetColor("_EmissionColor",color*emission); material.EnableKeyword("_EMISSION"); }
        else material.DisableKeyword("_EMISSION");
        material.SetOverrideTag("RenderType","Opaque"); if(material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite",1f); material.renderQueue=2000;
        return material;
    }

    private static Mesh CreateBoxMesh()
    {
        var mesh=new Mesh{name="magenheim.venom.box"};
        mesh.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
        mesh.triangles=new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2};
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh CreateCylinderMesh(int sides)
    {
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(var ring=0;ring<2;ring++){var y=ring==0?-.5f:.5f; for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides; vertices.Add(new Vector3(.5f*Mathf.Cos(a),y,.5f*Mathf.Sin(a)));}}
        var bottom=vertices.Count; vertices.Add(new Vector3(0,-.5f,0)); var top=vertices.Count; vertices.Add(new Vector3(0,.5f,0));
        for(var i=0;i<sides;i++)
        {
            var n=(i+1)%sides;
            triangles.AddRange(new[]{i,sides+i,n, n,sides+i,sides+n, bottom,i,n, top,sides+n,sides+i});
        }
        var mesh=new Mesh{name="magenheim.venom.cylinder."+sides,vertices=vertices.ToArray(),triangles=triangles.ToArray()}; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh CreatePrismMesh(int sides)
    {
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides; vertices.Add(new Vector3(.36f*Mathf.Cos(a),-.45f,.36f*Mathf.Sin(a)));}
        for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides; vertices.Add(new Vector3(.5f*Mathf.Cos(a),.20f,.5f*Mathf.Sin(a)));}
        var top=vertices.Count; vertices.Add(new Vector3(0,.68f,0)); var bottom=vertices.Count; vertices.Add(new Vector3(0,-.50f,0));
        for(var i=0;i<sides;i++)
        {
            var n=(i+1)%sides;
            triangles.AddRange(new[]{bottom,i,n, i,sides+i,n, n,sides+i,sides+n, top,sides+n,sides+i});
        }
        var mesh=new Mesh{name="magenheim.venom.prism."+sides,vertices=vertices.ToArray(),triangles=triangles.ToArray()}; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
