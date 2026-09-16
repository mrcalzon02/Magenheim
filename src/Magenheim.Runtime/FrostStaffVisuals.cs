using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Valheim-style owned silhouettes for the four Frost staff tiers.</summary>
internal static class FrostStaffVisuals
{
    internal static GameObject Apply(GameObject prefab, string assetName)
    {
        var context = ValheimStaffVisualBuilder.Begin(prefab, assetName);
        var source = context.SourceMaterial;
        var wood = ValheimStaffVisualBuilder.Surface(source,"frost-staff","wood",new Color(.29f,.21f,.16f,1f),0f,.14f);
        var paleWood = ValheimStaffVisualBuilder.Surface(source,"frost-staff","pale-wood",new Color(.46f,.36f,.27f,1f),0f,.16f);
        var leather = ValheimStaffVisualBuilder.Surface(source,"frost-staff","leather-wrap",new Color(.18f,.13f,.11f,1f),.01f,.10f);
        var iron = ValheimStaffVisualBuilder.Surface(source,"frost-staff","iron",new Color(.36f,.40f,.43f,1f),.42f,.22f);
        var silver = ValheimStaffVisualBuilder.Surface(source,"frost-staff","silver",new Color(.67f,.75f,.80f,1f),.62f,.34f);
        var frost = ValheimStaffVisualBuilder.Surface(source,"frost-staff","frost-crystal",new Color(.44f,.78f,.92f,1f),.08f,.58f,.22f);
        var bright = ValheimStaffVisualBuilder.Surface(source,"frost-staff","frost-bright-crystal",new Color(.73f,.93f,.98f,1f),.05f,.72f,.40f);
        var deep = ValheimStaffVisualBuilder.Surface(source,"frost-staff","frost-deep-crystal",new Color(.22f,.51f,.72f,1f),.10f,.48f,.14f);

        switch(assetName)
        {
            case "staff-frost-simple": BuildSimple(context.Root,paleWood,leather,iron,frost,deep); break;
            case "staff-frost-crystal": BuildCrystal(context.Root,paleWood,leather,silver,bright,deep); break;
            case "staff-frost-advanced": BuildAdvanced(context.Root,wood,leather,silver,bright,deep); break;
            case "staff-frost-master": BuildMaster(context.Root,wood,leather,silver,frost,bright,deep); break;
            default: throw new InvalidOperationException($"Unknown Frost staff geometry '{assetName}'.");
        }

        ValheimStaffVisualBuilder.Finish(context);
        return context.Root;
    }

    private static void BuildSimple(GameObject root, Material wood, Material leather, Material iron, Material frost, Material deep)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"frost",wood,leather,iron,.039f,.95f,false);
        var basePoint=new Vector3(.016f,.52f,.006f);
        var left=new Vector3(-.15f,.88f,.035f);
        var right=new Vector3(.17f,.83f,-.018f);
        ValheimStaffVisualBuilder.Segment(root,"fork-left",basePoint,left,.029f,wood,8);
        ValheimStaffVisualBuilder.Segment(root,"fork-right",basePoint,right,.027f,wood,8);
        ValheimStaffVisualBuilder.Segment(root,"fork-left-tip",left,new Vector3(-.12f,1.07f,.055f),.020f,iron,7);
        ValheimStaffVisualBuilder.Segment(root,"fork-right-tip",right,new Vector3(.24f,1.00f,-.035f),.018f,iron,7);
        ValheimStaffVisualBuilder.Shard(root,"ice-focus",new Vector3(.020f,.89f,.005f),new Vector3(.16f,.39f,.16f),Quaternion.Euler(-4f,10f,-6f),frost,6);
        ValheimStaffVisualBuilder.Shard(root,"ice-chip",new Vector3(-.14f,.77f,.065f),new Vector3(.043f,.15f,.043f),Quaternion.Euler(17f,6f,19f),deep,5);
        ValheimStaffVisualBuilder.FocusLight(root,"frost-light",new Vector3(.02f,.91f,.005f),new Color(.46f,.80f,1f),1.45f,.30f);
    }

    private static void BuildCrystal(GameObject root, Material wood, Material leather, Material silver, Material bright, Material deep)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"frost",wood,leather,silver,.042f,1.05f,true);
        var hub=new Vector3(.018f,.55f,.005f);
        var tips=new[]
        {
            new Vector3(-.20f,1.06f,.035f),new Vector3(.21f,1.00f,-.025f),
            new Vector3(-.04f,.94f,-.19f),new Vector3(.07f,.91f,.18f)
        };
        for(var i=0;i<tips.Length;i++)
        {
            var elbow=Vector3.Lerp(hub,tips[i],.48f)+Vector3.up*.015f;
            ValheimStaffVisualBuilder.Segment(root,"ice-cage-a-"+i,hub,elbow,.020f,silver,8);
            ValheimStaffVisualBuilder.Segment(root,"ice-cage-b-"+i,elbow,tips[i],.015f,silver,7);
        }
        ValheimStaffVisualBuilder.Shard(root,"frost-focus",new Vector3(.018f,.92f,.004f),new Vector3(.20f,.49f,.20f),Quaternion.Euler(-5f,16f,3f),bright,7);
        ValheimStaffVisualBuilder.Shard(root,"frost-side-shard",new Vector3(.20f,.79f,-.045f),new Vector3(.045f,.17f,.045f),Quaternion.Euler(18f,0f,-16f),deep,5);
        ValheimStaffVisualBuilder.Shard(root,"frost-lower-shard",new Vector3(-.12f,.72f,.055f),new Vector3(.040f,.13f,.040f),Quaternion.Euler(14f,7f,13f),deep,5);
        ValheimStaffVisualBuilder.FocusLight(root,"frost-crystal-light",new Vector3(.018f,.94f,.004f),new Color(.68f,.91f,1f),1.70f,.38f);
    }

    private static void BuildAdvanced(GameObject root, Material wood, Material leather, Material silver, Material bright, Material deep)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"frost",wood,leather,silver,.045f,1.15f,true);
        var centre=new Vector3(.015f,.91f,.005f);
        for(var i=0;i<6;i++)
        {
            var a=i*Mathf.PI*2f/6f+.12f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.10f+Vector3.down*.26f;
            var elbow=centre+radial*.19f+Vector3.down*.01f;
            var tip=centre+radial*(i==4?.30f:.26f)+Vector3.up*(i%2==0?.26f:.20f);
            ValheimStaffVisualBuilder.Segment(root,"rime-antler-a-"+i,lower,elbow,.018f,silver,8);
            ValheimStaffVisualBuilder.Segment(root,"rime-antler-b-"+i,elbow,tip,.014f,silver,7);
            ValheimStaffVisualBuilder.Shard(root,"rime-icicle-"+i,tip+Vector3.down*.015f,new Vector3(.042f,.17f,.042f),Quaternion.Euler(180f+10f*Mathf.Sin(a),a*Mathf.Rad2Deg,13f*Mathf.Cos(a)),i%2==0?deep:bright,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"advanced-rime-core",centre,new Vector3(.24f,.54f,.24f),Quaternion.Euler(-3f,20f,2f),bright,7);
        ValheimStaffVisualBuilder.Segment(root,"long-ice-hook",new Vector3(-.14f,.75f,.05f),new Vector3(-.31f,.96f,.08f),.017f,wood,7);
        ValheimStaffVisualBuilder.Shard(root,"hook-icicle",new Vector3(-.315f,.91f,.08f),new Vector3(.040f,.16f,.040f),Quaternion.Euler(172f,4f,-8f),deep,5);
        ValheimStaffVisualBuilder.FocusLight(root,"advanced-frost-light",centre+Vector3.up*.04f,new Color(.67f,.92f,1f),1.95f,.44f);
    }

    private static void BuildMaster(GameObject root, Material wood, Material leather, Material silver, Material frost, Material bright, Material deep)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"frost",wood,leather,silver,.049f,1.25f,true);
        foreach(var y in new[]{-.34f,-.08f,.20f,.46f})
            ValheimStaffVisualBuilder.Band(root,"master-band-"+y.ToString("0.00"),new Vector3(.007f*y,y,.004f),.073f,.045f,silver,10);
        var basePoint=new Vector3(.017f,.52f,.005f);
        var left=new Vector3(-.18f,.76f,.035f);
        var right=new Vector3(.19f,.73f,-.025f);
        ValheimStaffVisualBuilder.Segment(root,"master-left-fork",basePoint,left,.029f,silver,9);
        ValheimStaffVisualBuilder.Segment(root,"master-right-fork",basePoint,right,.028f,silver,9);
        ValheimStaffVisualBuilder.Segment(root,"master-left-spire",left,new Vector3(-.31f,1.18f,.06f),.019f,silver,8);
        ValheimStaffVisualBuilder.Segment(root,"master-right-spire",right,new Vector3(.30f,1.12f,-.04f),.018f,silver,8);
        var centre=new Vector3(.017f,.95f,.005f);
        for(var i=0;i<8;i++)
        {
            var a=i*Mathf.PI*2f/8f+.19f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.15f+Vector3.down*.21f;
            var upper=centre+radial*(i%3==0?.30f:.27f)+Vector3.up*(i%2==0?.18f:.23f);
            ValheimStaffVisualBuilder.Segment(root,"master-ice-cage-"+i,lower,upper,.013f,silver,7);
            if(i%2==0) ValheimStaffVisualBuilder.Shard(root,"master-ice-satellite-"+i,upper+Vector3.up*.02f,new Vector3(.045f,.16f,.045f),Quaternion.Euler(11f*Mathf.Sin(a),a*Mathf.Rad2Deg,15f*Mathf.Cos(a)),i%4==0?frost:deep,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"master-frost-core",centre,new Vector3(.29f,.64f,.29f),Quaternion.Euler(-4f,24f,3f),bright,8);
        ValheimStaffVisualBuilder.Shard(root,"master-deep-heart",centre+new Vector3(.03f,.12f,-.018f),new Vector3(.095f,.25f,.095f),Quaternion.Euler(7f,-13f,-4f),deep,6);
        ValheimStaffVisualBuilder.Shard(root,"master-lower-ice",new Vector3(.24f,.76f,-.04f),new Vector3(.045f,.18f,.045f),Quaternion.Euler(170f,0f,-10f),frost,5);
        ValheimStaffVisualBuilder.FocusLight(root,"master-frost-light",centre+Vector3.up*.07f,new Color(.78f,.96f,1f),2.25f,.52f);
    }
}
