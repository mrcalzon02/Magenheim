using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Valheim-style owned silhouettes for the four Earth staff tiers.</summary>
internal static class EarthStaffVisuals
{
    internal static void Apply(GameObject prefab, string prefabName)
    {
        var context=ValheimStaffVisualBuilder.Begin(prefab,prefabName);
        var source=context.SourceMaterial;
        var coreWood=ValheimStaffVisualBuilder.Surface(source,"earth-staff","core-wood",new Color(.26f,.18f,.10f,1f),0f,.10f);
        var elder=ValheimStaffVisualBuilder.Surface(source,"earth-staff","elder-wood",new Color(.18f,.13f,.10f,1f),0f,.11f);
        var leather=ValheimStaffVisualBuilder.Surface(source,"earth-staff","leather-wrap",new Color(.16f,.11f,.08f,1f),.01f,.09f);
        var iron=ValheimStaffVisualBuilder.Surface(source,"earth-staff","iron",new Color(.34f,.35f,.34f,1f),.56f,.23f);
        var black=ValheimStaffVisualBuilder.Surface(source,"earth-staff","blackmetal",new Color(.12f,.13f,.12f,1f),.76f,.30f);
        var stone=ValheimStaffVisualBuilder.Surface(source,"earth-staff","stone",new Color(.39f,.34f,.27f,1f),.02f,.08f);
        var deep=ValheimStaffVisualBuilder.Surface(source,"earth-staff","deep-stone",new Color(.20f,.18f,.16f,1f),.02f,.07f);
        var earth=ValheimStaffVisualBuilder.Surface(source,"earth-staff","earth-crystal",new Color(.72f,.52f,.25f,1f),.03f,.62f,.22f);
        var bright=ValheimStaffVisualBuilder.Surface(source,"earth-staff","earth-bright-crystal",new Color(.92f,.72f,.34f,1f),.02f,.72f,.34f);

        switch(prefabName)
        {
            case "Magenheim_Staff_Earth_Simple": BuildSimple(context.Root,coreWood,leather,iron,stone,earth); break;
            case "Magenheim_Staff_Earth_Crystal": BuildCrystal(context.Root,elder,leather,iron,stone,bright); break;
            case "Magenheim_Staff_Earth_Advanced": BuildAdvanced(context.Root,elder,leather,black,stone,deep,earth); break;
            case "Magenheim_Staff_Earth_Master": BuildMaster(context.Root,elder,leather,black,deep,bright); break;
            default: throw new InvalidOperationException($"Unknown Earth staff model '{prefabName}'.");
        }
        ValheimStaffVisualBuilder.Finish(context);
    }

    private static void BuildSimple(GameObject root,Material wood,Material leather,Material iron,Material stone,Material crystal)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"earth",wood,leather,iron,.045f,.75f,false);
        var hub=new Vector3(.015f,.52f,.005f);
        var left=new Vector3(-.16f,.79f,.04f);
        var right=new Vector3(.18f,.75f,-.02f);
        ValheimStaffVisualBuilder.Segment(root,"stone-yoke-left",hub,left,.038f,stone,7);
        ValheimStaffVisualBuilder.Segment(root,"stone-yoke-right",hub,right,.036f,stone,7);
        ValheimStaffVisualBuilder.Segment(root,"stone-left-cap",left,new Vector3(-.13f,.98f,.05f),.028f,stone,6);
        ValheimStaffVisualBuilder.Segment(root,"stone-right-cap",right,new Vector3(.24f,.93f,-.03f),.026f,stone,6);
        ValheimStaffVisualBuilder.Shard(root,"earth-focus",new Vector3(.02f,.84f,.005f),new Vector3(.20f,.37f,.20f),Quaternion.Euler(6f,16f,-5f),crystal,6);
        ValheimStaffVisualBuilder.Plate(root,"fault-plate",new Vector3(-.09f,.68f,.055f),new Vector3(.10f,.05f,.18f),Quaternion.Euler(12f,28f,17f),stone);
    }

    private static void BuildCrystal(GameObject root,Material wood,Material leather,Material iron,Material stone,Material crystal)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"earth",wood,leather,iron,.048f,.82f,true);
        var centre=new Vector3(.018f,.88f,.005f);
        for(var i=0;i<4;i++)
        {
            var a=i*Mathf.PI*.5f+.18f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.10f+Vector3.down*.23f;
            var upper=centre+radial*(i==1?.29f:.25f)+Vector3.up*(i%2==0?.19f:.14f);
            ValheimStaffVisualBuilder.Segment(root,"fault-brace-"+i,lower,upper,.025f,i%2==0?stone:iron,7);
            ValheimStaffVisualBuilder.Plate(root,"fault-plate-"+i,upper,new Vector3(.09f,.045f,.16f),Quaternion.Euler(11f*i,35f*i,17f*Mathf.Cos(a)),stone);
        }
        ValheimStaffVisualBuilder.Shard(root,"fault-focus",centre,new Vector3(.24f,.48f,.24f),Quaternion.Euler(-5f,22f,3f),crystal,7);
        ValheimStaffVisualBuilder.Shard(root,"fault-chip",new Vector3(-.22f,.77f,.06f),new Vector3(.050f,.14f,.050f),Quaternion.Euler(18f,4f,22f),crystal,5);
    }

    private static void BuildAdvanced(GameObject root,Material wood,Material leather,Material black,Material stone,Material deep,Material crystal)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"earth",wood,leather,black,.051f,.90f,true);
        foreach(var y in new[]{.02f,.27f,.48f})
            ValheimStaffVisualBuilder.Band(root,"deepstone-band-"+y.ToString("0.00"),new Vector3(.008f*y,y,.003f),.080f,.055f,deep,8);
        var centre=new Vector3(.018f,.92f,.005f);
        for(var i=0;i<6;i++)
        {
            var a=i*Mathf.PI*2f/6f+.21f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.12f+Vector3.down*.25f;
            var elbow=centre+radial*.22f+Vector3.down*.03f;
            var upper=centre+radial*(i%3==0?.32f:.27f)+Vector3.up*(i%2==0?.20f:.12f);
            ValheimStaffVisualBuilder.Segment(root,"seismic-rib-a-"+i,lower,elbow,.025f,i%2==0?stone:deep,7);
            ValheimStaffVisualBuilder.Segment(root,"seismic-rib-b-"+i,elbow,upper,.019f,deep,6);
            ValheimStaffVisualBuilder.Plate(root,"seismic-slab-"+i,upper,new Vector3(.10f,.05f,.18f),Quaternion.Euler(10f*i,a*Mathf.Rad2Deg,16f*Mathf.Cos(a)),i%2==0?stone:deep);
        }
        ValheimStaffVisualBuilder.Shard(root,"seismic-core",centre,new Vector3(.28f,.54f,.28f),Quaternion.Euler(-4f,24f,2f),crystal,7);
        ValheimStaffVisualBuilder.Plate(root,"broken-side-slab",new Vector3(-.30f,.85f,.08f),new Vector3(.12f,.06f,.23f),Quaternion.Euler(18f,20f,31f),stone);
    }

    private static void BuildMaster(GameObject root,Material wood,Material leather,Material black,Material deep,Material crystal)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"earth",wood,leather,black,.055f,1.0f,true);
        foreach(var y in new[]{-.34f,-.08f,.20f,.45f})
            ValheimStaffVisualBuilder.Band(root,"master-band-"+y.ToString("0.00"),new Vector3(.006f*y,y,.004f),.082f,.052f,black,10);
        var basePoint=new Vector3(.018f,.52f,.005f);
        var left=new Vector3(-.19f,.72f,.055f);
        var right=new Vector3(.20f,.69f,-.035f);
        ValheimStaffVisualBuilder.Segment(root,"world-left-yoke",basePoint,left,.036f,deep,8);
        ValheimStaffVisualBuilder.Segment(root,"world-right-yoke",basePoint,right,.034f,deep,8);
        ValheimStaffVisualBuilder.Segment(root,"world-left-spine",left,new Vector3(-.34f,1.14f,.08f),.025f,deep,7);
        ValheimStaffVisualBuilder.Segment(root,"world-right-spine",right,new Vector3(.31f,1.08f,-.05f),.023f,deep,7);
        var centre=new Vector3(.018f,.96f,.005f);
        for(var i=0;i<8;i++)
        {
            var a=i*Mathf.PI*2f/8f+.16f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.15f+Vector3.down*.23f;
            var upper=centre+radial*(i%3==0?.33f:.28f)+Vector3.up*(i%2==0?.19f:.12f);
            ValheimStaffVisualBuilder.Segment(root,"world-rib-"+i,lower,upper,.018f,deep,6);
            if(i%2==0) ValheimStaffVisualBuilder.Plate(root,"world-slab-"+i,upper,new Vector3(.11f,.055f,.20f),Quaternion.Euler(13f*Mathf.Sin(a),a*Mathf.Rad2Deg,19f*Mathf.Cos(a)),deep);
        }
        ValheimStaffVisualBuilder.Shard(root,"world-core",centre,new Vector3(.33f,.66f,.33f),Quaternion.Euler(-3f,26f,3f),crystal,8);
        ValheimStaffVisualBuilder.Shard(root,"world-heart",centre+new Vector3(.035f,.12f,-.018f),new Vector3(.11f,.27f,.11f),Quaternion.Euler(9f,-15f,-4f),crystal,6);
        ValheimStaffVisualBuilder.Plate(root,"hanging-fault-stone",new Vector3(-.31f,.74f,.10f),new Vector3(.10f,.16f,.08f),Quaternion.Euler(15f,12f,22f),deep);
    }
}
