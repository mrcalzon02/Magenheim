using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Valheim-style owned silhouettes for the four Venom staff tiers.</summary>
internal static class VenomStaffVisuals
{
    internal static void Apply(GameObject prefab, string assetName)
    {
        var context=ValheimStaffVisualBuilder.Begin(prefab,assetName);
        var source=context.SourceMaterial;
        var wood=ValheimStaffVisualBuilder.Surface(source,"venom-staff","wood",new Color(.25f,.17f,.10f,1f),0f,.10f);
        var elder=ValheimStaffVisualBuilder.Surface(source,"venom-staff","elder-wood",new Color(.20f,.14f,.11f,1f),0f,.11f);
        var leather=ValheimStaffVisualBuilder.Surface(source,"venom-staff","leather-wrap",new Color(.13f,.10f,.07f,1f),.01f,.08f);
        var iron=ValheimStaffVisualBuilder.Surface(source,"venom-staff","iron",new Color(.29f,.34f,.30f,1f),.42f,.20f);
        var black=ValheimStaffVisualBuilder.Surface(source,"venom-staff","blackmetal",new Color(.12f,.16f,.13f,1f),.72f,.30f);
        var venom=ValheimStaffVisualBuilder.Surface(source,"venom-staff","venom-crystal",new Color(.37f,.80f,.20f,1f),.04f,.55f,.20f);
        var bright=ValheimStaffVisualBuilder.Surface(source,"venom-staff","venom-bright-crystal",new Color(.63f,.94f,.35f,1f),.03f,.65f,.32f);
        var deep=ValheimStaffVisualBuilder.Surface(source,"venom-staff","venom-deep-crystal",new Color(.16f,.43f,.13f,1f),.05f,.35f,.10f);

        switch(assetName)
        {
            case "staff-venom-simple": BuildSimple(context.Root,wood,leather,iron,venom,deep); break;
            case "staff-venom-crystal": BuildCrystal(context.Root,elder,leather,iron,bright,deep); break;
            case "staff-venom-advanced": BuildAdvanced(context.Root,elder,leather,black,venom,deep); break;
            case "staff-venom-master": BuildMaster(context.Root,elder,leather,black,bright,deep); break;
            default: throw new InvalidOperationException($"Unknown Venom staff geometry '{assetName}'.");
        }
        ValheimStaffVisualBuilder.Finish(context);
    }

    private static void BuildSimple(GameObject root,Material wood,Material leather,Material iron,Material venom,Material deep)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"venom",wood,leather,iron,.039f,1.25f,false);
        var hub=new Vector3(.018f,.52f,.006f);
        var left=new Vector3(-.18f,.84f,.045f);
        var right=new Vector3(.15f,.88f,-.025f);
        ValheimStaffVisualBuilder.Segment(root,"thorn-hook-left",hub,left,.027f,wood,8);
        ValheimStaffVisualBuilder.Segment(root,"thorn-hook-right",hub,right,.025f,wood,8);
        ValheimStaffVisualBuilder.Segment(root,"left-fang",left,new Vector3(-.10f,1.03f,.055f),.018f,iron,6);
        ValheimStaffVisualBuilder.Segment(root,"right-fang",right,new Vector3(.24f,1.00f,-.04f),.016f,iron,6);
        ValheimStaffVisualBuilder.Shard(root,"venom-seed",new Vector3(.020f,.88f,.005f),new Vector3(.17f,.35f,.17f),Quaternion.Euler(6f,17f,-7f),venom,6);
        ValheimStaffVisualBuilder.Shard(root,"toxin-barb",new Vector3(-.16f,.72f,.07f),new Vector3(.042f,.14f,.042f),Quaternion.Euler(18f,5f,24f),deep,5);
    }

    private static void BuildCrystal(GameObject root,Material wood,Material leather,Material iron,Material bright,Material deep)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"venom",wood,leather,iron,.043f,1.35f,true);
        var centre=new Vector3(.018f,.90f,.005f);
        for(var i=0;i<4;i++)
        {
            var a=i*Mathf.PI*.5f+.20f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.10f+Vector3.down*.27f;
            var upper=centre+radial*(i==2?.30f:.25f)+Vector3.up*(i%2==0?.19f:.12f);
            ValheimStaffVisualBuilder.Segment(root,"fang-cage-a-"+i,lower,Vector3.Lerp(lower,upper,.55f),.019f,iron,8);
            ValheimStaffVisualBuilder.Segment(root,"fang-cage-b-"+i,Vector3.Lerp(lower,upper,.55f),upper,.013f,iron,6);
            ValheimStaffVisualBuilder.Shard(root,"fang-tip-"+i,upper,new Vector3(.038f,.13f,.038f),Quaternion.Euler(16f*Mathf.Sin(a),a*Mathf.Rad2Deg,18f*Mathf.Cos(a)),deep,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"venom-heart",centre,new Vector3(.21f,.47f,.21f),Quaternion.Euler(-4f,21f,3f),bright,7);
        ValheimStaffVisualBuilder.Shard(root,"drip-crystal",new Vector3(.22f,.77f,-.045f),new Vector3(.040f,.16f,.040f),Quaternion.Euler(170f,4f,-11f),deep,5);
        ValheimStaffVisualBuilder.FocusLight(root,"venom-light",centre+Vector3.up*.04f,new Color(.53f,.92f,.27f),1.55f,.28f);
    }

    private static void BuildAdvanced(GameObject root,Material wood,Material leather,Material black,Material venom,Material deep)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"venom",wood,leather,black,.046f,1.45f,true);
        var centre=new Vector3(.018f,.91f,.005f);
        for(var i=0;i<7;i++)
        {
            var a=i*Mathf.PI*2f/7f+.26f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.11f+Vector3.down*.26f;
            var elbow=centre+radial*.20f+Vector3.down*.02f;
            var upper=centre+radial*(i==4?.33f:.27f)+Vector3.up*(i%2==0?.22f:.16f);
            ValheimStaffVisualBuilder.Segment(root,"thorn-rib-a-"+i,lower,elbow,.019f,black,7);
            ValheimStaffVisualBuilder.Segment(root,"thorn-rib-b-"+i,elbow,upper,.012f,black,6);
            ValheimStaffVisualBuilder.Shard(root,"thorn-tip-"+i,upper,new Vector3(.038f,.14f,.038f),Quaternion.Euler(14f*Mathf.Sin(a),a*Mathf.Rad2Deg,20f*Mathf.Cos(a)),deep,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"advanced-venom-core",centre,new Vector3(.25f,.52f,.25f),Quaternion.Euler(-3f,24f,2f),venom,7);
        ValheimStaffVisualBuilder.Segment(root,"crooked-side-thorn",new Vector3(-.15f,.73f,.05f),new Vector3(-.34f,.91f,.09f),.015f,wood,7);
        ValheimStaffVisualBuilder.Shard(root,"side-toxin",new Vector3(-.35f,.95f,.09f),new Vector3(.040f,.14f,.040f),Quaternion.Euler(14f,0f,-16f),deep,5);
    }

    private static void BuildMaster(GameObject root,Material wood,Material leather,Material black,Material bright,Material deep)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"venom",wood,leather,black,.050f,1.55f,true);
        foreach(var y in new[]{-.34f,-.08f,.20f,.45f})
            ValheimStaffVisualBuilder.Band(root,"master-band-"+y.ToString("0.00"),new Vector3(.007f*y,y,.004f),.076f,.045f,black,10);
        var basePoint=new Vector3(.018f,.52f,.005f);
        var left=new Vector3(-.20f,.73f,.05f);
        var right=new Vector3(.18f,.70f,-.035f);
        ValheimStaffVisualBuilder.Segment(root,"fang-left",basePoint,left,.029f,black,8);
        ValheimStaffVisualBuilder.Segment(root,"fang-right",basePoint,right,.028f,black,8);
        ValheimStaffVisualBuilder.Segment(root,"fang-left-spire",left,new Vector3(-.35f,1.16f,.075f),.017f,black,7);
        ValheimStaffVisualBuilder.Segment(root,"fang-right-spire",right,new Vector3(.30f,1.10f,-.05f),.016f,black,7);
        var centre=new Vector3(.018f,.95f,.005f);
        for(var i=0;i<8;i++)
        {
            var a=i*Mathf.PI*2f/8f+.29f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.15f+Vector3.down*.22f;
            var upper=centre+radial*(i%3==0?.32f:.27f)+Vector3.up*(i%2==0?.17f:.22f);
            ValheimStaffVisualBuilder.Segment(root,"master-thorn-"+i,lower,upper,.012f,black,6);
            if(i%2==0) ValheimStaffVisualBuilder.Shard(root,"master-toxin-node-"+i,upper,new Vector3(.042f,.14f,.042f),Quaternion.Euler(13f*Mathf.Sin(a),a*Mathf.Rad2Deg,18f*Mathf.Cos(a)),deep,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"master-venom-core",centre,new Vector3(.29f,.61f,.29f),Quaternion.Euler(-4f,27f,3f),bright,8);
        ValheimStaffVisualBuilder.Shard(root,"master-dark-heart",centre+new Vector3(.03f,.12f,-.02f),new Vector3(.10f,.25f,.10f),Quaternion.Euler(8f,-14f,-5f),deep,6);
        ValheimStaffVisualBuilder.Segment(root,"hanging-fang",new Vector3(.24f,.88f,-.07f),new Vector3(.32f,.70f,-.10f),.009f,black,6);
        ValheimStaffVisualBuilder.Shard(root,"hanging-drop",new Vector3(.325f,.66f,-.10f),new Vector3(.038f,.13f,.038f),Quaternion.Euler(170f,0f,-8f),deep,5);
        ValheimStaffVisualBuilder.FocusLight(root,"master-venom-light",centre+Vector3.up*.06f,new Color(.65f,.96f,.38f),1.85f,.36f);
    }
}
