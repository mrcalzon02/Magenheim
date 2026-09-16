using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Original geometry for late-game Boss Resonance and Fate artifacts.</summary>
internal static class CapstoneArtifactVisuals
{
    internal const string EikthyrStormheart = "boss-eikthyr-stormheart";
    internal const string ElderrootHeart = "boss-elder-rootheart";
    internal const string BonemassRotheart = "boss-bonemass-rotheart";
    internal const string ModerRimeheart = "boss-moder-rimeheart";
    internal const string YagluthSunheart = "boss-yagluth-sunheart";
    internal const string QueenVeilheart = "boss-queen-veilheart";
    internal const string FaderAshheart = "boss-fader-ashheart";
    internal const string KallWinterheart = "boss-kall-winterheart";
    internal const string FateShard = "fate-shard";
    internal const string FateCrystal = "fate-crystal";
    internal const string NornSpindle = "norn-spindle";

    private static readonly Mesh BoxMesh = RuntimeMeshPrimitives.Box("magenheim.capstone.box");
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, string modelId, bool itemModel = true, float scale = 1f)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Model id is required.", nameof(modelId));
        if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(scale));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on capstone model host '{prefab.name}'.");
        var parent = itemModel ? prefab.transform.Find("attach") ?? prefab.transform : prefab.transform;
        var root = new GameObject("magenheim." + modelId + ".visual") { layer = prefab.layer };
        root.transform.SetParent(parent, false);
        root.transform.localScale = Vector3.one * scale;

        var black = Mat(source, modelId + ".black", new Color(.09f,.09f,.095f,1f), .50f, .20f);
        var iron = Mat(source, modelId + ".iron", new Color(.30f,.33f,.34f,1f), .62f, .30f);
        var bone = Mat(source, modelId + ".bone", new Color(.70f,.67f,.57f,1f), .02f, .16f);
        var wood = Mat(source, modelId + ".wood", new Color(.21f,.13f,.07f,1f), 0f, .09f);
        var storm = Mat(source, modelId + ".storm", new Color(.36f,.72f,1f,1f), .02f, .82f, .75f);
        var earth = Mat(source, modelId + ".earth", new Color(.51f,.34f,.15f,1f), .05f, .28f, .12f);
        var venom = Mat(source, modelId + ".venom", new Color(.34f,.82f,.20f,1f), .02f, .72f, .48f);
        var frost = Mat(source, modelId + ".frost", new Color(.55f,.90f,1f,1f), .01f, .86f, .68f);
        var sun = Mat(source, modelId + ".sun", new Color(1f,.67f,.18f,1f), .04f, .85f, .78f);
        var radiance = Mat(source, modelId + ".radiance", new Color(1f,.93f,.56f,1f), .02f, .92f, .90f);
        var seidr = Mat(source, modelId + ".seidr", new Color(.66f,.30f,.96f,1f), .02f, .82f, .70f);
        var fire = Mat(source, modelId + ".fire", new Color(1f,.24f,.08f,1f), .02f, .78f, .75f);
        var spirit = Mat(source, modelId + ".spirit", new Color(.43f,1f,.86f,1f), .01f, .88f, .78f);
        var fate = Mat(source, modelId + ".fate", new Color(.92f,.62f,1f,1f), .02f, .92f, .88f);
        var fatePale = Mat(source, modelId + ".fate-pale", new Color(.98f,.91f,1f,1f), .01f, .95f, 1f);

        switch (modelId)
        {
            case EikthyrStormheart: BuildStormheart(root, bone, iron, storm); break;
            case ElderrootHeart: BuildRootheart(root, wood, earth, bone); break;
            case BonemassRotheart: BuildRotheart(root, bone, black, venom); break;
            case ModerRimeheart: BuildRimeheart(root, iron, frost); break;
            case YagluthSunheart: BuildSunheart(root, black, sun, radiance); break;
            case QueenVeilheart: BuildVeilheart(root, black, iron, seidr); break;
            case FaderAshheart: BuildAshheart(root, black, iron, fire); break;
            case KallWinterheart: BuildWinterheart(root, bone, iron, frost, spirit); break;
            case FateShard: BuildFateShard(root, fate, fatePale); break;
            case FateCrystal: BuildFateCrystal(root, iron, fate, fatePale); break;
            case NornSpindle: BuildNornSpindle(root, wood, iron, fate, fatePale); break;
            default: throw new InvalidOperationException($"Unknown Magenheim capstone model '{modelId}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static void BuildStormheart(GameObject root, Material bone, Material iron, Material storm)
    {
        Prism(root,"heart",new Vector3(0f,.18f,0f),new Vector3(.34f,.68f,.34f),6,storm,new Vector3(0f,0f,180f));
        Cylinder(root,"heart-band",new Vector3(0f,.16f,0f),new Vector3(.42f,.06f,.42f),10,iron);
        Box(root,"left-antler",new Vector3(-.22f,.58f,0f),new Vector3(.07f,.55f,.07f),bone,new Vector3(0f,0f,-23f));
        Box(root,"right-antler",new Vector3(.22f,.58f,0f),new Vector3(.07f,.55f,.07f),bone,new Vector3(0f,0f,23f));
        foreach (var side in new[] { -1f, 1f })
        {
            Box(root,"antler-branch-a-"+side,new Vector3(.34f*side,.72f,0f),new Vector3(.06f,.28f,.06f),bone,new Vector3(0f,0f,28f*side));
            Box(root,"antler-branch-b-"+side,new Vector3(.27f*side,.88f,0f),new Vector3(.05f,.24f,.05f),bone,new Vector3(0f,0f,48f*side));
        }
    }

    private static void BuildRootheart(GameObject root, Material wood, Material earth, Material bone)
    {
        Prism(root,"earth-core",new Vector3(0f,.28f,0f),new Vector3(.31f,.66f,.31f),7,earth);
        for (var i=0;i<6;i++)
        {
            var a=i*Mathf.PI*2f/6f; var x=.23f*Mathf.Cos(a); var z=.23f*Mathf.Sin(a);
            Box(root,"root-"+i,new Vector3(x,.28f,z),new Vector3(.075f,.72f,.075f),wood,new Vector3(10f*Mathf.Sin(a),0f,-14f*Mathf.Cos(a)));
            Box(root,"root-foot-"+i,new Vector3(x*1.35f,-.06f,z*1.35f),new Vector3(.06f,.28f,.06f),wood,new Vector3(18f*Mathf.Sin(a),0f,-25f*Mathf.Cos(a)));
        }
        Cylinder(root,"root-collar",new Vector3(0f,.03f,0f),new Vector3(.48f,.08f,.48f),8,bone);
    }

    private static void BuildRotheart(GameObject root, Material bone, Material black, Material venom)
    {
        Prism(root,"rot-core",new Vector3(0f,.20f,0f),new Vector3(.36f,.58f,.36f),8,venom,new Vector3(0f,12f,178f));
        for(var i=0;i<7;i++)
        {
            var a=i*Mathf.PI*2f/7f; var x=.25f*Mathf.Cos(a); var z=.25f*Mathf.Sin(a);
            Prism(root,"rot-nodule-"+i,new Vector3(x,.17f,z),new Vector3(.12f,.24f,.12f),5,venom,new Vector3(i*5f,0f,-i*4f));
            Box(root,"thorn-"+i,new Vector3(x*1.25f,.45f,z*1.25f),new Vector3(.045f,.31f,.045f),bone,new Vector3(12f*Mathf.Sin(a),0f,-20f*Mathf.Cos(a)));
        }
        Cylinder(root,"rot-band",new Vector3(0f,.00f,0f),new Vector3(.48f,.07f,.48f),9,black);
    }

    private static void BuildRimeheart(GameObject root, Material iron, Material frost)
    {
        Prism(root,"rime-core",new Vector3(0f,.24f,0f),new Vector3(.30f,.80f,.30f),6,frost);
        for(var i=0;i<6;i++)
        {
            var a=i*Mathf.PI*2f/6f; var x=.27f*Mathf.Cos(a); var z=.27f*Mathf.Sin(a);
            Prism(root,"rime-spike-"+i,new Vector3(x,.38f,z),new Vector3(.09f,.46f,.09f),5,frost,new Vector3(20f*Mathf.Sin(a),0f,-20f*Mathf.Cos(a)));
        }
        Cylinder(root,"rime-band",new Vector3(0f,.05f,0f),new Vector3(.46f,.07f,.46f),10,iron);
    }

    private static void BuildSunheart(GameObject root, Material black, Material sun, Material radiance)
    {
        Cylinder(root,"sun-disk",new Vector3(0f,.30f,0f),new Vector3(.72f,.10f,.72f),16,sun,new Vector3(90f,0f,0f));
        Cylinder(root,"black-back",new Vector3(0f,.30f,-.07f),new Vector3(.56f,.12f,.56f),14,black,new Vector3(90f,0f,0f));
        Prism(root,"solar-core",new Vector3(0f,.30f,.08f),new Vector3(.28f,.66f,.28f),6,radiance,new Vector3(90f,0f,0f));
        for(var i=0;i<8;i++)
        {
            var angle=i*45f; var r=angle*Mathf.Deg2Rad;
            Box(root,"ray-"+i,new Vector3(.48f*Mathf.Cos(r),.30f+.48f*Mathf.Sin(r),0f),new Vector3(.08f,.34f,.08f),sun,new Vector3(0f,0f,angle-90f));
        }
    }

    private static void BuildVeilheart(GameObject root, Material black, Material iron, Material seidr)
    {
        Prism(root,"veil-core",new Vector3(0f,.33f,0f),new Vector3(.25f,.72f,.25f),6,seidr);
        Arc(root,"left-veil",new Vector3(0f,.34f,0f),.42f,7,iron,-120f,120f,-.08f);
        Arc(root,"right-veil",new Vector3(0f,.34f,0f),.52f,7,black,60f,300f,.08f);
        Cylinder(root,"veil-collar",new Vector3(0f,.00f,0f),new Vector3(.36f,.08f,.36f),10,iron);
    }

    private static void BuildAshheart(GameObject root, Material black, Material iron, Material fire)
    {
        Prism(root,"ash-core",new Vector3(0f,.24f,0f),new Vector3(.31f,.70f,.31f),6,fire);
        for(var i=0;i<6;i++)
        {
            var a=i*Mathf.PI*2f/6f; var x=.28f*Mathf.Cos(a); var z=.28f*Mathf.Sin(a);
            Box(root,"cage-rib-"+i,new Vector3(x,.26f,z),new Vector3(.055f,.75f,.055f),black,new Vector3(8f*Mathf.Sin(a),0f,-8f*Mathf.Cos(a)));
            Prism(root,"ember-tip-"+i,new Vector3(x,.70f,z),new Vector3(.06f,.18f,.06f),4,fire);
        }
        Cylinder(root,"ash-band-low",new Vector3(0f,-.02f,0f),new Vector3(.48f,.07f,.48f),10,iron);
        Cylinder(root,"ash-band-high",new Vector3(0f,.58f,0f),new Vector3(.45f,.06f,.45f),10,iron);
    }

    private static void BuildWinterheart(GameObject root, Material bone, Material iron, Material frost, Material spirit)
    {
        Prism(root,"left-core",new Vector3(-.08f,.25f,0f),new Vector3(.23f,.72f,.23f),6,frost,new Vector3(0f,0f,-8f));
        Prism(root,"right-core",new Vector3(.08f,.25f,0f),new Vector3(.21f,.66f,.21f),6,spirit,new Vector3(0f,0f,8f));
        Arc(root,"winter-halo",new Vector3(0f,.28f,0f),.44f,10,iron,-20f,200f,0f);
        Box(root,"left-horn",new Vector3(-.31f,.60f,0f),new Vector3(.06f,.42f,.06f),bone,new Vector3(0f,0f,-34f));
        Box(root,"right-horn",new Vector3(.31f,.60f,0f),new Vector3(.06f,.42f,.06f),bone,new Vector3(0f,0f,34f));
    }

    private static void BuildFateShard(GameObject root, Material fate, Material pale)
    {
        Prism(root,"thread-a",new Vector3(-.08f,.20f,0f),new Vector3(.14f,.72f,.14f),5,fate,new Vector3(0f,0f,-12f));
        Prism(root,"thread-b",new Vector3(.08f,.20f,0f),new Vector3(.11f,.62f,.11f),5,pale,new Vector3(0f,0f,14f));
        Prism(root,"thread-c",new Vector3(0f,.05f,.07f),new Vector3(.08f,.48f,.08f),4,fate,new Vector3(9f,21f,0f));
    }

    private static void BuildFateCrystal(GameObject root, Material iron, Material fate, Material pale)
    {
        Prism(root,"fate-core-a",new Vector3(-.07f,.28f,0f),new Vector3(.20f,.80f,.20f),6,fate,new Vector3(0f,0f,-9f));
        Prism(root,"fate-core-b",new Vector3(.07f,.28f,0f),new Vector3(.18f,.72f,.18f),6,pale,new Vector3(0f,0f,9f));
        Arc(root,"thread-ring-a",new Vector3(0f,.30f,0f),.40f,12,iron,-40f,210f,0f);
        Arc(root,"thread-ring-b",new Vector3(0f,.30f,0f),.48f,12,iron,140f,390f,.04f);
    }

    private static void BuildNornSpindle(GameObject root, Material wood, Material iron, Material fate, Material pale)
    {
        Cylinder(root,"spindle-shaft",new Vector3(0f,.62f,0f),new Vector3(.12f,1.24f,.12f),8,wood);
        Cylinder(root,"lower-whorl",new Vector3(0f,.18f,0f),new Vector3(.62f,.09f,.62f),12,iron);
        Cylinder(root,"upper-whorl",new Vector3(0f,.90f,0f),new Vector3(.46f,.07f,.46f),12,iron);
        Prism(root,"thread-crystal",new Vector3(0f,1.25f,0f),new Vector3(.18f,.48f,.18f),6,pale);
        for(var i=0;i<3;i++)
        {
            var a=i*Mathf.PI*2f/3f; var x=.26f*Mathf.Cos(a); var z=.26f*Mathf.Sin(a);
            Prism(root,"norn-thread-"+i,new Vector3(x,.63f,z),new Vector3(.065f,.58f,.065f),4,fate,new Vector3(9f*Mathf.Sin(a),0f,-9f*Mathf.Cos(a)));
        }
    }

    private static void Arc(GameObject root,string name,Vector3 center,float radius,int segments,Material material,float startDegrees,float endDegrees,float zOffset)
    {
        var span=endDegrees-startDegrees;
        for(var i=0;i<segments;i++)
        {
            var a=startDegrees+span*(i+.5f)/segments; var rad=a*Mathf.Deg2Rad;
            var segmentLength=radius*Mathf.Abs(span)*Mathf.Deg2Rad/segments*.96f;
            Box(root,name+"-"+i,new Vector3(center.x+radius*Mathf.Cos(rad),center.y+radius*Mathf.Sin(rad),center.z+zOffset),new Vector3(segmentLength,.045f,.055f),material,new Vector3(0f,0f,a+90f));
        }
    }

    private static void Box(GameObject root,string name,Vector3 position,Vector3 size,Material material,Vector3? euler=null) =>
        Add(root,name,BoxMesh,position,size,Quaternion.Euler(euler ?? Vector3.zero),material);

    private static void Cylinder(GameObject root,string name,Vector3 position,Vector3 size,int sides,Material material,Vector3? euler=null)
    {
        if(!CylinderMeshes.TryGetValue(sides,out var mesh))
        {
            mesh=RuntimeMeshPrimitives.Cylinder(sides,"magenheim.capstone.cylinder."+sides);
            CylinderMeshes.Add(sides,mesh);
        }
        Add(root,name,mesh,position,size,Quaternion.Euler(euler ?? Vector3.zero),material);
    }

    private static void Prism(GameObject root,string name,Vector3 position,Vector3 size,int sides,Material material,Vector3? euler=null)
    {
        if(!PrismMeshes.TryGetValue(sides,out var mesh))
        {
            mesh=RuntimeMeshPrimitives.Prism(
                sides,
                "magenheim.capstone.prism."+sides,
                lowerRadius:.40f,
                lowerY:-.50f,
                upperRadius:.50f,
                upperY:.18f,
                apexY:.70f,
                baseY:-.50f);
            PrismMeshes.Add(sides,mesh);
        }
        Add(root,name,mesh,position,size,Quaternion.Euler(euler ?? Vector3.zero),material);
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

    private static Material Mat(Material source,string suffix,Color color,float metallic,float gloss,float emission=0f)
    {
        var material=new Material(source){name="magenheim.capstone."+suffix};
        GeneratedSurfaceTextures.Apply(material, suffix);
        if(material.HasProperty("_Color")) material.SetColor("_Color",color);
        if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
        if(material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",gloss);
        if(material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap",null);
        material.DisableKeyword("_NORMALMAP");
        if(material.HasProperty("_EmissionColor") && emission>0f){material.SetColor("_EmissionColor",color*emission);material.EnableKeyword("_EMISSION");}
        else material.DisableKeyword("_EMISSION");
        material.SetOverrideTag("RenderType","Opaque");
        if(material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite",1f);
        material.renderQueue=2000;
        return material;
    }
}
