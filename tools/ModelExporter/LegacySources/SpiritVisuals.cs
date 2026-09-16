using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Magenheim.Runtime;
internal static class SpiritVisuals
{
    internal static void Apply(GameObject prefab, string assetName)
    {
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on Spirit staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + assetName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);

        var bone = MakeMaterial(source, "bone", new Color(.66f, .70f, .65f, 1f), .02f, .18f);
        var dark = MakeMaterial(source, "dark-wood", new Color(.08f, .12f, .13f, 1f), .18f, .16f);
        var silver = MakeMaterial(source, "silver", new Color(.42f, .55f, .55f, 1f), .56f, .38f);
        var spirit = MakeMaterial(source, "spirit-crystal", new Color(.24f, .88f, .76f, 1f), .01f, .74f, .58f);
        var pale = MakeMaterial(source, "spirit-pale-crystal", new Color(.72f, 1f, .92f, 1f), .01f, .84f, .78f);

        switch (assetName)
        {
            case "staff-spirit-simple":
                Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), new Vector3(.065f, 1.48f, .065f), dark);
                Cylinder(root, "bone-cap", new Vector3(0f, .57f, 0f), new Vector3(.10f, .08f, .10f), bone);
                Cube(root, "hook", new Vector3(.07f, .73f, 0f), new Vector3(.035f, .34f, .035f), Quaternion.Euler(0f, 0f, 24f), bone);
                Sphere(root, "whisper", new Vector3(.13f, .90f, 0f), new Vector3(.13f, .18f, .13f), spirit);
                break;

            case "staff-spirit-crystal":
                Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), new Vector3(.072f, 1.48f, .072f), dark);
                Cylinder(root, "collar", new Vector3(0f, .55f, 0f), new Vector3(.13f, .07f, .13f), silver);
                for (var i = 0; i < 4; i++)
                {
                    var angle = i * Mathf.PI / 2f;
                    var x = .14f * Mathf.Cos(angle);
                    var z = .14f * Mathf.Sin(angle);
                    Cube(root, "cage-" + i, new Vector3(x, .82f, z), new Vector3(.030f, .46f, .030f), Quaternion.Euler(10f * Mathf.Sin(angle), 0f, 16f * Mathf.Cos(angle)), silver);
                }
                Sphere(root, "lantern", new Vector3(0f, .90f, 0f), new Vector3(.20f, .28f, .20f), spirit);
                Sphere(root, "heart", new Vector3(0f, .97f, 0f), new Vector3(.095f, .13f, .095f), pale);
                break;

            case "staff-spirit-advanced":
                Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), new Vector3(.078f, 1.48f, .078f), dark);
                Cylinder(root, "collar", new Vector3(0f, .54f, 0f), new Vector3(.15f, .08f, .15f), silver);
                for (var i = 0; i < 6; i++)
                {
                    var angle = i * Mathf.PI / 3f;
                    var x = .19f * Mathf.Cos(angle);
                    var z = .19f * Mathf.Sin(angle);
                    Sphere(root, "voice-" + i, new Vector3(x, .88f, z), new Vector3(.075f, .10f, .075f), i % 2 == 0 ? spirit : pale);
                    Cube(root, "rib-" + i, new Vector3(x * .70f, .80f, z * .70f), new Vector3(.026f, .42f, .026f), Quaternion.Euler(14f * Mathf.Sin(angle), 0f, 18f * Mathf.Cos(angle)), bone);
                }
                Sphere(root, "chorus-core", new Vector3(0f, .91f, 0f), new Vector3(.22f, .30f, .22f), spirit);
                break;

            case "staff-spirit-master":
                Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), new Vector3(.084f, 1.48f, .084f), dark);
                foreach (var y in new[] { -.44f, -.12f, .24f, .51f })
                    Cylinder(root, "band-" + y, new Vector3(0f, y, 0f), new Vector3(.13f, .045f, .13f), silver);
                for (var ring = 0; ring < 2; ring++)
                {
                    var radius = ring == 0 ? .21f : .29f;
                    var y = ring == 0 ? .88f : .94f;
                    var count = ring == 0 ? 6 : 8;
                    for (var i = 0; i < count; i++)
                    {
                        var angle = i * Mathf.PI * 2f / count + (ring == 0 ? 0f : Mathf.PI / 8f);
                        var x = radius * Mathf.Cos(angle);
                        var z = radius * Mathf.Sin(angle);
                        Sphere(root, "echo-" + ring + "-" + i, new Vector3(x, y, z), new Vector3(.065f, .085f, .065f), ring == 0 ? spirit : pale);
                    }
                }
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Mathf.PI / 4f;
                    var x = .20f * Mathf.Cos(angle);
                    var z = .20f * Mathf.Sin(angle);
                    Cube(root, "reliquary-rib-" + i, new Vector3(x * .70f, .83f, z * .70f), new Vector3(.025f, .48f, .025f), Quaternion.Euler(17f * Mathf.Sin(angle), 0f, 20f * Mathf.Cos(angle)), silver);
                }
                Sphere(root, "reliquary", new Vector3(0f, .95f, 0f), new Vector3(.25f, .38f, .25f), spirit);
                Sphere(root, "soul-heart", new Vector3(0f, 1.07f, 0f), new Vector3(.11f, .16f, .11f), pale);
                break;

            default:
                throw new InvalidOperationException($"Unknown Spirit geometry '{assetName}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void Cube(GameObject root, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material) =>
        Primitive(root, name, PrimitiveType.Cube, position, scale, rotation, material);

    private static void Cylinder(GameObject root, string name, Vector3 position, Vector3 scale, Material material) =>
        Primitive(root, name, PrimitiveType.Cylinder, position, scale, Quaternion.identity, material);

    private static void Sphere(GameObject root, string name, Vector3 position, Vector3 scale, Material material) =>
        Primitive(root, name, PrimitiveType.Sphere, position, scale, Quaternion.identity, material);

    private static void Primitive(GameObject root, string name, PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        var gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.layer = root.layer;
        gameObject.transform.SetParent(root.transform, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localRotation = rotation;
        gameObject.transform.localScale = scale;
        var collider = gameObject.GetComponent<Collider>();
        if (collider) collider.enabled = false;
        var renderer = gameObject.GetComponent<Renderer>();
        if (!renderer) throw new InvalidOperationException($"Spirit visual primitive '{name}' has no renderer.");
        renderer.sharedMaterial = material;
    }

    private static Material MakeMaterial(Material source, string name, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.spirit." + name };
        GeneratedSurfaceTextures.Apply(material, name);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
        material.DisableKeyword("_NORMALMAP");
        if (material.HasProperty("_EmissionColor") && emission > 0f)
        {
            material.SetColor("_EmissionColor", color * emission);
            material.EnableKeyword("_EMISSION");
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }
        material.SetOverrideTag("RenderType", "Opaque");
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
        material.renderQueue = 2000;
        return material;
    }
}
