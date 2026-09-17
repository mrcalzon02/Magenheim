using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;
using UnityEngine;
namespace Magenheim.Runtime;
internal static class GeodeVisuals {
    private const int IconSize = 256;
    private const int IconDesignSize = 128;
    private const int IconScale = IconSize / IconDesignSize;
    private static readonly Dictionary<string,Sprite> Icons=new(StringComparer.Ordinal);
    internal static void Apply(GameObject prefab,Color interiorTint,float scale=1f,bool worldObject=false) {
        var root=ModelAssets.Load(prefab,"geode-sample",item:!worldObject,scale:scale);
        if(worldObject){root.transform.localPosition=new Vector3(0,.5f*scale,0);foreach(var c in prefab.GetComponentsInChildren<Collider>(true))if(!c.isTrigger)c.enabled=false;var collider=prefab.AddComponent<SphereCollider>();collider.center=new Vector3(0,.5f*scale,0);collider.radius=.63f*scale;}
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)) {
            var source=renderer.sharedMaterial;if(source.name.IndexOf(".interior-",StringComparison.Ordinal)<0)continue;
            var bright=source.name.IndexOf("interior-bright-",StringComparison.Ordinal)>=0;var tint=bright?Color.Lerp(interiorTint,Color.white,.28f):interiorTint;
            var material=new Material(source);material.color=tint;if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",tint*(bright?.34f:.22f));renderer.sharedMaterial=material;
        }
    }
internal static Sprite Icon(string biomeKey, Color interiorTint)
    {
        var key = biomeKey + "|" + ColorUtility.ToHtmlStringRGB(interiorTint);
        if (Icons.TryGetValue(key, out var cached)) return cached;
        var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false) { name = "magenheim.geode." + biomeKey + ".icon", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = Enumerable.Repeat(Color.clear, IconSize * IconSize).ToArray(); var center = new Vector2(63.5f, 62f);
        for (var y = 0; y < IconSize; y++) for (var x = 0; x < IconSize; x++)
        {
            var designX = (x + .5f) / IconScale; var designY = (y + .5f) / IconScale;
            var dx = (designX - center.x) / 52f; var dy = (designY - center.y) / 48f; var angle = Mathf.Atan2(dy, dx); var radius = Mathf.Sqrt(dx * dx + dy * dy);
            var boundary = 0.94f + 0.035f * Mathf.Cos(angle * 12f) + 0.018f * Mathf.Cos(angle * 5f + 0.8f); if (radius > boundary) continue;
            var light = Mathf.Clamp01(0.50f + (designX / IconDesignSize) * 0.22f + (designY / IconDesignSize) * 0.10f);
            pixels[y * IconSize + x] = Color.Lerp(new Color(0.13f,0.12f,0.115f,1f), new Color(0.40f,0.37f,0.34f,1f), light);
        }
        var cutawayOuter = new[] { new Vector2(69,30), new Vector2(101,38), new Vector2(111,62), new Vector2(96,88), new Vector2(70,84), new Vector2(57,57) };
        FillPolygon(pixels, IconSize, cutawayOuter, new Color(0.045f,0.04f,0.038f,1f));
        var cutawayInner = new[] { new Vector2(72,36), new Vector2(96,42), new Vector2(103,61), new Vector2(91,80), new Vector2(73,77), new Vector2(63,57) };
        FillPolygon(pixels, IconSize, cutawayInner, interiorTint);
        FillPolygon(pixels, IconSize, new[] { new Vector2(71,58), new Vector2(80,40), new Vector2(86,61), new Vector2(77,76) }, Color.Lerp(interiorTint, Color.white, .24f));
        FillPolygon(pixels, IconSize, new[] { new Vector2(84,59), new Vector2(94,45), new Vector2(99,62), new Vector2(90,77) }, Color.Lerp(interiorTint, Color.white, .10f));
        FillPolygon(pixels, IconSize, new[] { new Vector2(66,59), new Vector2(74,47), new Vector2(77,62), new Vector2(72,73) }, Color.Lerp(interiorTint, Color.black, .14f));
        var crackColor = new Color(0.035f,0.032f,0.03f,1f);
        DrawCrack(pixels,IconSize,19,60,47,57,crackColor,3); DrawCrack(pixels,IconSize,47,57,59,44,crackColor,3); DrawCrack(pixels,IconSize,47,57,54,83,crackColor,3);
        DrawCrack(pixels,IconSize,54,83,37,101,crackColor,3); DrawCrack(pixels,IconSize,27,35,47,57,crackColor,3); DrawCrack(pixels,IconSize,30,86,54,83,crackColor,3); DrawCrack(pixels,IconSize,53,21,59,44,crackColor,3);
        texture.SetPixels(pixels); texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0,0,IconSize,IconSize), new Vector2(.5f,.5f), IconSize); sprite.name = "magenheim.geode." + biomeKey + ".icon"; Icons.Add(key, sprite); return sprite;
    }
private static void FillPolygon(Color[] pixels, int size, Vector2[] polygon, Color color) { var minX = Mathf.Clamp(Mathf.FloorToInt(polygon.Min(p => p.x) * IconScale), 0, size - 1); var maxX = Mathf.Clamp(Mathf.CeilToInt(polygon.Max(p => p.x) * IconScale), 0, size - 1); var minY = Mathf.Clamp(Mathf.FloorToInt(polygon.Min(p => p.y) * IconScale), 0, size - 1); var maxY = Mathf.Clamp(Mathf.CeilToInt(polygon.Max(p => p.y) * IconScale), 0, size - 1); for (var y = minY; y <= maxY; y++) for (var x = minX; x <= maxX; x++) if (InsidePolygon(new Vector2((x + .5f) / IconScale, (y + .5f) / IconScale), polygon)) pixels[y * size + x] = color; }
private static void DrawCrack(Color[] pixels, int size, int x0, int y0, int x1, int y1, Color color, int thickness) { x0*=IconScale;y0*=IconScale;x1*=IconScale;y1*=IconScale;thickness*=IconScale;var dx = Math.Abs(x1-x0); var sx=x0<x1?1:-1; var dy=-Math.Abs(y1-y0); var sy=y0<y1?1:-1; var err=dx+dy; while(true) { for(var oy=-thickness/2;oy<=thickness/2;oy++) for(var ox=-thickness/2;ox<=thickness/2;ox++) { var x=x0+ox; var y=y0+oy; if(x>=0&&x<size&&y>=0&&y<size) pixels[y*size+x]=color; } if(x0==x1&&y0==y1) break; var e2=2*err; if(e2>=dy){err+=dy;x0+=sx;} if(e2<=dx){err+=dx;y0+=sy;} } }
    private static bool InsidePolygon(Vector2 point, Vector2[] polygon) { var inside = false; for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++) { var a = polygon[i]; var b = polygon[j]; if (((a.y > point.y) != (b.y > point.y)) && point.x < (b.x - a.x) * (point.y - a.y) / ((b.y - a.y) + .00001f) + a.x) inside = !inside; } return inside; }
}
