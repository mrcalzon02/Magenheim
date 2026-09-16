using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Valheim-style owned silhouettes for the four Storm staff tiers.</summary>
internal static class StormStaffVisuals
{
    internal static void Apply(GameObject prefab, string prefabName)
    {
        var context=ValheimStaffVisualBuilder.Begin(prefab,prefabName);
        var source=context.SourceMaterial;
        var ash=ValheimStaffVisualBuilder.Surface(source,"storm-staff","ash-wood",new Color(.30f,.25f,.20f,1f),0f,.14f);
        var dark=ValheimStaffVisualBuilder.Surface(source,"storm-staff","dark-wood",new Color(.18f,.14f,.11f,1f),0f,.11f);
        var leather=ValheimStaffVisualBuilder.Surface(source,"storm-staff","leather-wrap",new Color(.13f,.10f,.08f,1f),.01f,.10f);
        var copper=ValheimStaffVisualBuilder.Surface(source,"storm-staff","copper-metal",new Color(.55f,.31f,.18f,1f),.52f,.31f);
        var silver=ValheimStaffVisualBuilder.Surface(source,"storm-staff","silver",new Color(.63f,.70f,.75f,1f),.70f,.38f);
        var black=ValheimStaffVisualBuilder.Surface(source,"storm-staff","blackmetal",new Color(.12f,.15f,.18f,1f),.78f,.34f);
        var storm=ValheimStaffVisualBuilder.Surface(source,"storm-staff","storm-crystal",new Color(.28f,.62f,1f,1f),.02f,.76f,.50f);
        var bright=ValheimStaffVisualBuilder.Surface(source,"storm-staff","storm-bright-crystal",new Color(.72f,.90f,1f,1f),.01f,.88f,.72f);

        switch(prefabName)
        {
            case "Magenheim_Staff_Storm_Simple": BuildSimple(context.Root,ash,leather,copper,storm); break;
            case "Magenheim_Staff_Storm_Crystal": BuildCrystal(context.Root,dark,leather,silver,storm,bright); break;
            case "Magenheim_Staff_Storm_Advanced": BuildAdvanced(context.Root,dark,leather,silver,black,storm,bright); break;
            case "Magenheim_Staff_Storm_Master": BuildMaster(context.Root,dark,leather,black,storm,bright); break;
            default: throw new InvalidOperationException($"Unknown Storm staff model '{prefabName}'.");
        }
        ValheimStaffVisualBuilder.Finish(context);
    }

    private static void BuildSimple(GameObject root,Material wood,Material leather,Material copper,Material storm)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"storm",wood,leather,copper,.040f,1.0f,false);
        var hub=new Vector3(.018f,.52f,.006f);
        var left=new Vector3(-.17f,.89f,.025f);
        var right=new Vector3(.21f,.82f,-.025f);
        ValheimStaffVisualBuilder.Segment(root,"left-conductor",hub,left,.024f,copper,8);
        ValheimStaffVisualBuilder.Segment(root,"right-conductor",hub,right,.022f,copper,8);
        ValheimStaffVisualBuilder.Segment(root,"left-needle",left,new Vector3(-.14f,1.08f,.035f),.013f,copper,6);
        ValheimStaffVisualBuilder.Segment(root,"right-needle",right,new Vector3(.29f,1.01f,-.04f),.012f,copper,6);
        ValheimStaffVisualBuilder.Shard(root,"static-focus",new Vector3(.028f,.90f,.002f),new Vector3(.15f,.35f,.15f),Quaternion.Euler(5f,13f,-5f),storm,6);
        ValheimStaffVisualBuilder.Segment(root,"grounding-spur",new Vector3(-.05f,.61f,.02f),new Vector3(-.18f,.68f,.06f),.014f,copper,6);
        ValheimStaffVisualBuilder.FocusLight(root,"static-light",new Vector3(.028f,.92f,.002f),new Color(.36f,.68f,1f),1.55f,.34f);
    }

    private static void BuildCrystal(GameObject root,Material wood,Material leather,Material silver,Material storm,Material bright)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"storm",wood,leather,silver,.043f,1.12f,true);
        var centre=new Vector3(.018f,.91f,.005f);
        var anchors=new[]
        {
            new Vector3(-.22f,1.05f,.035f),new Vector3(.21f,1.11f,-.025f),
            new Vector3(-.04f,.96f,-.21f),new Vector3(.08f,.90f,.20f)
        };
        for(var i=0;i<anchors.Length;i++)
        {
            var lower=new Vector3(.018f,.57f,.005f)+new Vector3((i-1.5f)*.008f,0f,0f);
            var elbow=Vector3.Lerp(lower,anchors[i],.58f)+Vector3.up*.018f;
            ValheimStaffVisualBuilder.Segment(root,"conductor-a-"+i,lower,elbow,.018f,silver,8);
            ValheimStaffVisualBuilder.Segment(root,"conductor-b-"+i,elbow,anchors[i],.012f,silver,6);
            ValheimStaffVisualBuilder.Shard(root,"arc-tip-"+i,anchors[i]+Vector3.up*.025f,new Vector3(.038f,.12f,.038f),Quaternion.Euler(12f*i,19f*i,-7f*i),bright,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"bolt-core",centre,new Vector3(.19f,.47f,.19f),Quaternion.Euler(-4f,21f,3f),storm,7);
        ValheimStaffVisualBuilder.Shard(root,"charged-heart",centre+new Vector3(.025f,.12f,-.015f),new Vector3(.075f,.19f,.075f),Quaternion.Euler(8f,-12f,-5f),bright,6);
        ValheimStaffVisualBuilder.FocusLight(root,"storm-crystal-light",centre+Vector3.up*.05f,new Color(.55f,.80f,1f),1.85f,.44f);
    }

    private static void BuildAdvanced(GameObject root,Material wood,Material leather,Material silver,Material black,Material storm,Material bright)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"storm",wood,leather,black,.046f,1.22f,true);
        ValheimStaffVisualBuilder.Band(root,"silver-neck",new Vector3(.019f,.52f,.006f),.078f,.055f,silver,10);
        var centre=new Vector3(.018f,.92f,.005f);
        for(var i=0;i<5;i++)
        {
            var a=i*Mathf.PI*2f/5f+.31f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.10f+Vector3.down*.27f;
            var elbow=centre+radial*.20f+Vector3.down*.02f;
            var tip=centre+radial*(i==2?.32f:.27f)+Vector3.up*(i%2==0?.29f:.22f);
            ValheimStaffVisualBuilder.Segment(root,"arc-arm-a-"+i,lower,elbow,.018f,i%2==0?silver:black,8);
            ValheimStaffVisualBuilder.Segment(root,"arc-arm-b-"+i,elbow,tip,.012f,silver,6);
            ValheimStaffVisualBuilder.Shard(root,"arc-node-"+i,tip+Vector3.up*.025f,new Vector3(.040f,.13f,.040f),Quaternion.Euler(13f*Mathf.Sin(a),a*Mathf.Rad2Deg,18f*Mathf.Cos(a)),bright,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"advanced-storm-core",centre,new Vector3(.24f,.52f,.24f),Quaternion.Euler(-3f,24f,2f),storm,7);
        ValheimStaffVisualBuilder.Segment(root,"offset-lightning-rod",new Vector3(-.15f,.73f,.04f),new Vector3(-.34f,1.02f,.07f),.013f,silver,6);
        ValheimStaffVisualBuilder.Shard(root,"rod-cap",new Vector3(-.345f,1.06f,.07f),new Vector3(.035f,.12f,.035f),Quaternion.Euler(4f,0f,-12f),bright,5);
        ValheimStaffVisualBuilder.FocusLight(root,"advanced-storm-light",centre+Vector3.up*.05f,new Color(.54f,.81f,1f),2.05f,.54f);
    }

    private static void BuildMaster(GameObject root,Material wood,Material leather,Material black,Material storm,Material bright)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root,"storm",wood,leather,black,.050f,1.34f,true);
        foreach(var y in new[]{-.34f,-.08f,.20f,.46f})
            ValheimStaffVisualBuilder.Band(root,"master-band-"+y.ToString("0.00"),new Vector3(.007f*y,y,.004f),.076f,.046f,black,10);
        var basePoint=new Vector3(.018f,.52f,.005f);
        var left=new Vector3(-.19f,.73f,.045f);
        var right=new Vector3(.19f,.70f,-.025f);
        ValheimStaffVisualBuilder.Segment(root,"thunderfork-left",basePoint,left,.029f,black,9);
        ValheimStaffVisualBuilder.Segment(root,"thunderfork-right",basePoint,right,.028f,black,9);
        ValheimStaffVisualBuilder.Segment(root,"left-spire",left,new Vector3(-.35f,1.20f,.075f),.017f,black,7);
        ValheimStaffVisualBuilder.Segment(root,"right-spire",right,new Vector3(.31f,1.13f,-.045f),.016f,black,7);
        ValheimStaffVisualBuilder.Segment(root,"rear-spire",new Vector3(.02f,.58f,-.025f),new Vector3(-.02f,1.24f,-.20f),.015f,black,7);
        var centre=new Vector3(.018f,.96f,.005f);
        for(var i=0;i<8;i++)
        {
            var a=i*Mathf.PI*2f/8f+.23f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.14f+Vector3.down*.21f;
            var upper=centre+radial*(i%3==0?.31f:.27f)+Vector3.up*(i%2==0?.20f:.15f);
            ValheimStaffVisualBuilder.Segment(root,"thunder-cage-"+i,lower,upper,.011f,black,6);
            if(i%2==0) ValheimStaffVisualBuilder.Shard(root,"thunder-node-"+i,upper+Vector3.up*.024f,new Vector3(.040f,.13f,.040f),Quaternion.Euler(12f*Mathf.Sin(a),a*Mathf.Rad2Deg,15f*Mathf.Cos(a)),bright,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"thunderhead-core",centre,new Vector3(.30f,.64f,.30f),Quaternion.Euler(-5f,27f,3f),bright,8);
        ValheimStaffVisualBuilder.Shard(root,"storm-heart",centre+new Vector3(.03f,.13f,-.02f),new Vector3(.10f,.26f,.10f),Quaternion.Euler(8f,-15f,-5f),storm,6);
        ValheimStaffVisualBuilder.Segment(root,"side-grounder",new Vector3(.23f,.89f,-.06f),new Vector3(.34f,.72f,-.09f),.009f,black,6);
        ValheimStaffVisualBuilder.Shard(root,"grounder-cap",new Vector3(.345f,.69f,-.09f),new Vector3(.035f,.11f,.035f),Quaternion.Euler(18f,0f,-8f),bright,5);
        ValheimStaffVisualBuilder.FocusLight(root,"master-storm-light",centre+Vector3.up*.07f,new Color(.70f,.90f,1f),2.45f,.66f);
    }
}
