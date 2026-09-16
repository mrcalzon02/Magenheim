using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Valheim-style owned silhouettes for the four Fire staff tiers.</summary>
internal static class FireStaffVisuals
{
    internal static void Apply(GameObject prefab, string prefabName)
    {
        var context = ValheimStaffVisualBuilder.Begin(prefab, prefabName);
        var source = context.SourceMaterial;
        var darkWood = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "dark-wood", new Color(.24f,.13f,.075f,1f), 0f, .12f);
        var charred = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "charred-wood", new Color(.15f,.075f,.045f,1f), 0f, .08f);
        var leather = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "scorched-leather", new Color(.12f,.065f,.04f,1f), .01f, .08f);
        var bronze = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "bronze", new Color(.54f,.31f,.12f,1f), .48f, .28f);
        var iron = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "iron", new Color(.34f,.31f,.29f,1f), .52f, .24f);
        var blackMetal = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "blackmetal", new Color(.11f,.10f,.095f,1f), .76f, .32f);
        var ember = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "ember-crystal", new Color(1f,.30f,.035f,1f), .02f, .70f, .48f);
        var flame = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "flame-crystal", new Color(1f,.58f,.08f,1f), .02f, .78f, .62f);
        var hot = ValheimStaffVisualBuilder.Surface(source, "fire-staff", "white-hot-crystal", new Color(1f,.90f,.48f,1f), .01f, .86f, .78f);

        switch (prefabName)
        {
            case "Magenheim_Staff_Fire_Simple": BuildSimple(context.Root, darkWood, leather, bronze, ember); break;
            case "Magenheim_Staff_Fire_Crystal": BuildCrystal(context.Root, charred, leather, iron, flame, ember); break;
            case "Magenheim_Staff_Fire_Advanced": BuildAdvanced(context.Root, charred, leather, iron, blackMetal, flame, ember); break;
            case "Magenheim_Staff_Fire_Master": BuildMaster(context.Root, charred, leather, blackMetal, flame, hot, ember); break;
            default: throw new InvalidOperationException($"Unknown Fire staff model '{prefabName}'.");
        }

        ValheimStaffVisualBuilder.Finish(context);
    }

    private static void BuildSimple(GameObject root, Material wood, Material leather, Material bronze, Material ember)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root, "fire", wood, leather, bronze, .040f, 1.1f, false);
        var basePoint = new Vector3(.018f,.52f,.006f);
        var left = new Vector3(-.16f,.87f,.025f);
        var right = new Vector3(.19f,.82f,-.020f);
        ValheimStaffVisualBuilder.Segment(root,"ember-fork-left",basePoint,left,.030f,wood,8);
        ValheimStaffVisualBuilder.Segment(root,"ember-fork-right",basePoint,right,.027f,wood,8);
        ValheimStaffVisualBuilder.Segment(root,"ember-fork-left-tip",left,new Vector3(-.11f,1.04f,.045f),.020f,bronze,7);
        ValheimStaffVisualBuilder.Segment(root,"ember-fork-right-tip",right,new Vector3(.27f,.96f,-.035f),.018f,bronze,7);
        ValheimStaffVisualBuilder.Shard(root,"ember-focus",new Vector3(.025f,.87f,.005f),new Vector3(.17f,.34f,.17f),Quaternion.Euler(4f,13f,-7f),ember,6);
        ValheimStaffVisualBuilder.Shard(root,"ember-splinter",new Vector3(-.15f,.78f,.055f),new Vector3(.045f,.14f,.045f),Quaternion.Euler(15f,5f,18f),ember,5);
        ValheimStaffVisualBuilder.FocusLight(root,"ember-light",new Vector3(.025f,.90f,.005f),new Color(1f,.32f,.04f),1.55f,.42f);
    }

    private static void BuildCrystal(GameObject root, Material wood, Material leather, Material iron, Material flame, Material ember)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root, "fire", wood, leather, iron, .043f, 1.25f, true);
        var hub = new Vector3(.020f,.56f,.004f);
        var tips = new[]
        {
            new Vector3(-.22f,1.01f,.04f), new Vector3(.19f,1.08f,-.03f),
            new Vector3(-.05f,.94f,-.21f), new Vector3(.08f,.91f,.19f)
        };
        for (var i=0;i<tips.Length;i++)
        {
            var shoulder = Vector3.Lerp(hub,tips[i],.45f) + new Vector3(i==0?-.025f:i==1?.020f:0f,.02f,0f);
            ValheimStaffVisualBuilder.Segment(root,"flame-cage-a-"+i,hub,shoulder,.021f,iron,8);
            ValheimStaffVisualBuilder.Segment(root,"flame-cage-b-"+i,shoulder,tips[i],.016f,iron,7);
        }
        ValheimStaffVisualBuilder.Shard(root,"flame-focus",new Vector3(.015f,.91f,.005f),new Vector3(.21f,.45f,.21f),Quaternion.Euler(-3f,18f,4f),flame,7);
        ValheimStaffVisualBuilder.Shard(root,"flame-heart",new Vector3(.045f,1.00f,-.018f),new Vector3(.075f,.18f,.075f),Quaternion.Euler(12f,-7f,-6f),ember,6);
        ValheimStaffVisualBuilder.Shard(root,"ash-spark",new Vector3(.22f,.78f,-.05f),new Vector3(.040f,.13f,.040f),Quaternion.Euler(20f,4f,-15f),ember,5);
        ValheimStaffVisualBuilder.FocusLight(root,"flame-light",new Vector3(.02f,.93f,.005f),new Color(1f,.52f,.08f),1.85f,.48f);
    }

    private static void BuildAdvanced(GameObject root, Material wood, Material leather, Material iron, Material blackMetal, Material flame, Material ember)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root, "fire", wood, leather, blackMetal, .046f, 1.35f, true);
        ValheimStaffVisualBuilder.Band(root,"iron-neck",new Vector3(.019f,.52f,.006f),.080f,.060f,iron,10);
        var centre = new Vector3(.020f,.90f,.005f);
        for (var i=0;i<5;i++)
        {
            var a=i*Mathf.PI*2f/5f+.24f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.10f+Vector3.down*.27f;
            var elbow=centre+radial*.20f+Vector3.down*.02f;
            var tip=centre+radial*(i==1?.30f:.27f)+Vector3.up*(i==3?.24f:.30f);
            ValheimStaffVisualBuilder.Segment(root,"flame-rib-a-"+i,lower,elbow,.020f,i%2==0?iron:blackMetal,8);
            ValheimStaffVisualBuilder.Segment(root,"flame-rib-b-"+i,elbow,tip,.015f,blackMetal,7);
            ValheimStaffVisualBuilder.Shard(root,"flame-tip-"+i,tip+Vector3.up*.035f,new Vector3(.050f,.15f,.050f),Quaternion.Euler(13f*Mathf.Sin(a),a*Mathf.Rad2Deg,17f*Mathf.Cos(a)),ember,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"advanced-fire-core",centre,new Vector3(.25f,.52f,.25f),Quaternion.Euler(-4f,23f,2f),flame,7);
        ValheimStaffVisualBuilder.Segment(root,"broken-charred-hook",new Vector3(-.14f,.74f,.05f),new Vector3(-.31f,.91f,.08f),.019f,wood,7);
        ValheimStaffVisualBuilder.Shard(root,"hook-ember",new Vector3(-.32f,.95f,.08f),new Vector3(.042f,.14f,.042f),Quaternion.Euler(11f,0f,-18f),ember,5);
        ValheimStaffVisualBuilder.FocusLight(root,"advanced-fire-light",centre+Vector3.up*.04f,new Color(1f,.55f,.09f),2.10f,.56f);
    }

    private static void BuildMaster(GameObject root, Material wood, Material leather, Material blackMetal, Material flame, Material hot, Material ember)
    {
        ValheimStaffVisualBuilder.OrganicShaft(root, "fire", wood, leather, blackMetal, .050f, 1.45f, true);
        foreach(var y in new[]{-.34f,-.08f,.20f,.46f})
            ValheimStaffVisualBuilder.Band(root,"master-band-"+y.ToString("0.00"),new Vector3(.008f*y,.0f+y,.004f),.076f,.047f,blackMetal,10);
        var forkBase=new Vector3(.018f,.52f,.006f);
        var left=new Vector3(-.18f,.76f,.045f);
        var right=new Vector3(.19f,.73f,-.025f);
        ValheimStaffVisualBuilder.Segment(root,"master-main-left",forkBase,left,.031f,blackMetal,9);
        ValheimStaffVisualBuilder.Segment(root,"master-main-right",forkBase,right,.029f,blackMetal,9);
        ValheimStaffVisualBuilder.Segment(root,"master-left-spire",left,new Vector3(-.31f,1.20f,.07f),.020f,blackMetal,8);
        ValheimStaffVisualBuilder.Segment(root,"master-right-spire",right,new Vector3(.34f,1.12f,-.04f),.019f,blackMetal,8);
        var centre=new Vector3(.018f,.94f,.006f);
        for(var i=0;i<8;i++)
        {
            var a=i*Mathf.PI*2f/8f+.17f;
            var radial=new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a));
            var lower=centre+radial*.15f+Vector3.down*.20f;
            var upper=centre+radial*(i%3==0?.31f:.27f)+Vector3.up*(i%2==0?.18f:.24f);
            ValheimStaffVisualBuilder.Segment(root,"master-cage-"+i,lower,upper,.014f,blackMetal,7);
            if(i%2==0) ValheimStaffVisualBuilder.Shard(root,"master-ember-"+i,upper+Vector3.up*.035f,new Vector3(.045f,.14f,.045f),Quaternion.Euler(12f*Mathf.Sin(a),a*Mathf.Rad2Deg,16f*Mathf.Cos(a)),ember,5);
        }
        ValheimStaffVisualBuilder.Shard(root,"white-hot-core",centre,new Vector3(.29f,.62f,.29f),Quaternion.Euler(-3f,26f,4f),hot,8);
        ValheimStaffVisualBuilder.Shard(root,"inner-flame",centre+new Vector3(.035f,.12f,-.02f),new Vector3(.11f,.29f,.11f),Quaternion.Euler(8f,-14f,-5f),flame,6);
        ValheimStaffVisualBuilder.Segment(root,"hanging-chain",new Vector3(-.25f,.93f,.08f),new Vector3(-.32f,.73f,.10f),.010f,blackMetal,6);
        ValheimStaffVisualBuilder.Shard(root,"hanging-coal",new Vector3(-.325f,.69f,.10f),new Vector3(.042f,.13f,.042f),Quaternion.Euler(15f,0f,-9f),ember,5);
        ValheimStaffVisualBuilder.FocusLight(root,"master-fire-light",centre+Vector3.up*.07f,new Color(1f,.73f,.20f),2.45f,.66f);
    }
}
