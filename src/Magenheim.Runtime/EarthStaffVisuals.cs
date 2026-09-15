using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Owned Magenheim geometry for the four Earth staff tiers.</summary>
internal static class EarthStaffVisuals
{
    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> Cylinders = new();
    private static readonly Dictionary<int, Mesh> Prisms = new();

    internal static void Apply(GameObject prefab, string prefabName)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(r => r.sharedMaterial).FirstOrDefault(m => m)
            ?? throw new InvalidOperationException($"No material source exists on Earth staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + prefabName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var coreWood = Mat(source,"corewood",new Color(.26f,.18f,.10f,1f),0f,.10f);
        var elder = Mat(source,"elder",new Color(.18f,.13f,.10f,1f),0f,.11f);
        var iron = Mat(source,"iron",new Color(.34f,.35f,.34f,1f),.56f,.23f);
        var blackMetal = Mat(source,"blackmetal",new Color(.12f,.13f,.12f,1f),.76f,.30f);
        var stone = Mat(source,"stone",new Color(.39f,.34f,.27f,1f),.02f,.08f);
        var deepStone = Mat(source,"deepstone",new Color(.20f,.18f,.16f,1f),.02f,.07f);
        var earth = Mat(source,"earth-crystal",new Color(.72f,.52f,.25f,1f),.03f,.62f,.22f);
        var earthBright = Mat(source,"earth-bright",new Color(.92f,.72f,.34f,1f),.02f,.72f,.34f);

        switch(prefabName)
        {
            case "Magenheim_Staff_Earth_Simple": BuildSimple(root,coreWood,iron,stone,earth); break;
            case "Magenheim_Staff_Earth_Crystal": BuildCrystal(root,elder,iron,stone,earthBright); break;
            case "Magenheim_Staff_Earth_Advanced": BuildAdvanced(root,elder,blackMetal,stone,deepStone,earth); break;
            case "Magenheim_Staff_Earth_Master": BuildMaster(root,elder,blackMetal,deepStone,earthBright); break;
            default: throw new InvalidOperationException($"Unknown Earth staff model '{prefabName}'.");
        }

        foreach(var renderer in original) renderer.enabled=false;
        foreach(var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled=false;
    }

    private static void BuildSimple(GameObject root,Material wood,Material metal,Material stone,Material crystal)
    {
        Shaft(root,wood,metal,.040f);
        Cylinder(root,"stone-collar",new Vector3(0,.58f,0),.085f,.085f,8,stone);
        Box(root,"left-brace",new Vector3(-.10f,.75f,0),new Vector3(.050f,.34f,.060f),stone,-12f);
        Box(root,"right-brace",new Vector3(.10f,.75f,0),new Vector3(.050f,.34f,.060f),stone,12f);
        Prism(root,"earth-focus",new Vector3(0,.84f,0),.105f,.33f,6,crystal);
    }

    private static void BuildCrystal(GameObject root,Material wood,Material metal,Material stone,Material crystal)
    {
        Shaft(root,wood,metal,.043f);
        Cylinder(root,"lower-stone-band",new Vector3(0,.35f,0),.072f,.060f,8,stone);
        Cylinder(root,"upper-stone-band",new Vector3(0,.60f,0),.090f,.080f,8,stone);
        for(var i=0;i<4;i++)
        {
            var a=Mathf.PI*.5f*i;
            Box(root,"brace-"+i,new Vector3(.14f*Mathf.Cos(a),.80f,.14f*Mathf.Sin(a)),new Vector3(.055f,.34f,.055f),metal,10f*Mathf.Cos(a));
        }
        Prism(root,"fault-focus",new Vector3(0,.89f,0),.125f,.45f,6,crystal);
    }

    private static void BuildAdvanced(GameObject root,Material wood,Material metal,Material stone,Material deepStone,Material crystal)
    {
        Shaft(root,wood,metal,.046f);
        foreach(var y in new[]{.22f,.48f,.64f}) Cylinder(root,"reinforcement-"+y,new Vector3(0,y,0),.074f,.050f,10,deepStone);
        Cylinder(root,"seismic-ring",new Vector3(0,.75f,0),.205f,.060f,10,stone);
        for(var i=0;i<6;i++)
        {
            var a=Mathf.PI*2f*i/6f;
            var x=.17f*Mathf.Cos(a);var z=.17f*Mathf.Sin(a);
            Box(root,"stone-rib-"+i,new Vector3(x,.88f,z),new Vector3(.055f,.38f,.055f),i%2==0?stone:deepStone,9f*Mathf.Cos(a));
        }
        Prism(root,"seismic-core",new Vector3(0,.93f,0),.135f,.50f,6,crystal);
    }

    private static void BuildMaster(GameObject root,Material wood,Material metal,Material stone,Material crystal)
    {
        Shaft(root,wood,metal,.049f);
        foreach(var y in new[]{-.48f,-.12f,.22f,.51f}) Cylinder(root,"shaft-band-"+y,new Vector3(0,y,0),.072f,.050f,12,metal);
        Cylinder(root,"world-ring",new Vector3(0,.75f,0),.245f,.070f,12,stone);
        for(var i=0;i<8;i++)
        {
            var a=Mathf.PI*2f*i/8f;
            var x=.20f*Mathf.Cos(a);var z=.20f*Mathf.Sin(a);
            Box(root,"world-rib-"+i,new Vector3(x,.91f,z),new Vector3(.050f,.45f,.050f),metal,12f*Mathf.Cos(a));
            Prism(root,"world-chip-"+i,new Vector3(x,1.13f,z),.032f,.14f,5,crystal);
        }
        Prism(root,"world-core",new Vector3(0,.99f,0),.155f,.62f,8,crystal);
    }

    private static void Shaft(GameObject root,Material wood,Material metal,float radius)
    {
        Cylinder(root,"shaft",new Vector3(0,-.08f,0),radius,1.48f,10,wood);
        Cylinder(root,"pommel",new Vector3(0,-.77f,0),radius*1.45f,.11f,10,metal);
        Cylinder(root,"neck",new Vector3(0,.50f,0),radius*1.38f,.060f,10,metal);
    }

    private static void Box(GameObject root,string name,Vector3 p,Vector3 s,Material m,float z=0f)=>Add(root,name,BoxMesh,p,s,Quaternion.Euler(0,0,z),m);
    private static void Cylinder(GameObject root,string name,Vector3 p,float radius,float height,int sides,Material m){if(!Cylinders.TryGetValue(sides,out var mesh)){mesh=MakeCylinder(sides);Cylinders.Add(sides,mesh);}Add(root,name,mesh,p,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,m);}
    private static void Prism(GameObject root,string name,Vector3 p,float radius,float height,int sides,Material m){if(!Prisms.TryGetValue(sides,out var mesh)){mesh=MakePrism(sides);Prisms.Add(sides,mesh);}Add(root,name,mesh,p,new Vector3(radius*2f,height,radius*2f),Quaternion.identity,m);}
    private static void Add(GameObject root,string name,Mesh mesh,Vector3 p,Vector3 s,Quaternion r,Material m){var go=new GameObject(name){layer=root.layer};go.transform.SetParent(root.transform,false);go.transform.localPosition=p;go.transform.localScale=s;go.transform.localRotation=r;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=m;}
    private static Material Mat(Material source,string suffix,Color c,float metallic,float gloss,float emission=0f){var m=new Material(source){name="magenheim.earth-staff."+suffix};m.mainTexture=Texture2D.whiteTexture;m.mainTextureScale=Vector2.one;m.mainTextureOffset=Vector2.zero;if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",gloss);if(m.HasProperty("_BumpMap"))m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");if(m.HasProperty("_EmissionColor")&&emission>0f){m.SetColor("_EmissionColor",c*emission);m.EnableKeyword("_EMISSION");}else m.DisableKeyword("_EMISSION");m.SetOverrideTag("RenderType","Opaque");if(m.HasProperty("_ZWrite"))m.SetFloat("_ZWrite",1f);m.renderQueue=2000;return m;}
    private static Mesh CreateBoxMesh(){var m=new Mesh{name="magenheim.earth-staff.box"};m.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};m.triangles=new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2};m.RecalculateNormals();m.RecalculateBounds();return m;}
    private static Mesh MakeCylinder(int sides){var v=new List<Vector3>();var t=new List<int>();for(var r=0;r<2;r++){var y=r==0?-.5f:.5f;for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),y,.5f*Mathf.Sin(a)));}}var b=v.Count;v.Add(new Vector3(0,-.5f,0));var top=v.Count;v.Add(new Vector3(0,.5f,0));for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{i,sides+i,n,n,sides+i,sides+n,b,i,n,top,sides+n,sides+i});}var m=new Mesh{name="magenheim.earth-staff.cylinder."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
    private static Mesh MakePrism(int sides){var v=new List<Vector3>();var t=new List<int>();for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.38f*Mathf.Cos(a),-.46f,.38f*Mathf.Sin(a)));}for(var i=0;i<sides;i++){var a=2f*Mathf.PI*i/sides;v.Add(new Vector3(.5f*Mathf.Cos(a),.22f,.5f*Mathf.Sin(a)));}var top=v.Count;v.Add(new Vector3(0,.66f,0));var bottom=v.Count;v.Add(new Vector3(0,-.50f,0));for(var i=0;i<sides;i++){var n=(i+1)%sides;t.AddRange(new[]{bottom,i,n,i,sides+i,n,n,sides+i,sides+n,top,sides+n,sides+i});}var m=new Mesh{name="magenheim.earth-staff.prism."+sides,vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
}
