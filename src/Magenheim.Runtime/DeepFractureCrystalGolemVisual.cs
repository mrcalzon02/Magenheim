using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal static class DeepFractureCrystalGolemVisual
    {
        private static readonly Dictionary<int, Mesh> PrismMeshes = new Dictionary<int, Mesh>();

        internal static void Apply(GameObject prefab, ElementalAlignment alignment)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var original = prefab.GetComponentsInChildren<Renderer>(true);
            var source = original.Select(x => x.sharedMaterial).FirstOrDefault(x => x != null)
                ?? throw new InvalidOperationException("No creature material source: " + prefab.name);
            var tint = ElementVisualPalette.Tint(alignment);
            var shell = Material(source, Color.Lerp(tint, new Color(.22f, .24f, .28f), .55f), .32f);
            var core = Material(source, Color.Lerp(tint, Color.white, .72f), 1.55f);
            var shard = Material(source, tint, .95f);
            var root = new GameObject("magenheim.fracture.creature.crystal-golem.visual") { layer = prefab.layer };
            root.transform.SetParent(prefab.transform, false);
            root.transform.localPosition = new Vector3(0f, 2.25f, 0f);

            Prism(root, "regeneration-core", new Vector3(0f, .82f, -.48f), .48f, 1.05f, 8, core);
            Prism(root, "resistance-core", new Vector3(0f, .92f, .42f), .36f, .78f, 7, core, new Vector3(82f, 0f, 0f));
            Prism(root, "crystal-torso", new Vector3(0f, .72f, 0f), 1.08f, 1.92f, 8, shell);
            Prism(root, "crystal-crown", new Vector3(0f, 2.12f, .05f), .66f, .92f, 7, shard);
            Prism(root, "armor-core-left", new Vector3(-1.08f, 1.02f, 0f), .34f, .8f, 7, core, new Vector3(0f, 0f, -28f));
            Prism(root, "armor-core-right", new Vector3(1.08f, 1.02f, 0f), .34f, .8f, 7, core, new Vector3(0f, 0f, 28f));
            Prism(root, "arm-left", new Vector3(-1.28f, -.08f, 0f), .35f, 2.05f, 8, shell, new Vector3(0f, 0f, -5f));
            Prism(root, "arm-right", new Vector3(1.28f, -.08f, 0f), .35f, 2.05f, 8, shell, new Vector3(0f, 0f, 5f));
            Prism(root, "leg-left", new Vector3(-.48f, -1.38f, 0f), .43f, 2.25f, 8, shell);
            Prism(root, "leg-right", new Vector3(.48f, -1.38f, 0f), .43f, 2.25f, 8, shell);
            for (var i = -3; i <= 3; i++)
                Prism(root, "dorsal-spire-" + (i + 3), new Vector3(i * .25f, 1.45f, -.72f), .11f, .78f + .08f * (3 - Mathf.Abs(i)), 5, shard, new Vector3(-48f, 0f, i * 6f));

            var light = root.AddComponent<Light>();
            light.color = tint;
            light.range = 8.2f;
            light.intensity = 1.55f;
            foreach (var renderer in original) if (!renderer.transform.IsChildOf(root.transform)) renderer.enabled = false;
            foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        }

        private static Material Material(Material source, Color color, float emission)
        {
            var material = new Material(source);
            material.mainTexture = Texture2D.whiteTexture;
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor")) { material.SetColor("_EmissionColor", color * emission); material.EnableKeyword("_EMISSION"); }
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
            material.DisableKeyword("_NORMALMAP");
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = 2000;
            return material;
        }

        private static void Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material, Vector3? rotation = null)
        {
            Mesh mesh;
            if (!PrismMeshes.TryGetValue(sides, out mesh)) { mesh = CreatePrism(sides); PrismMeshes.Add(sides, mesh); }
            var part = new GameObject(name) { layer = root.layer };
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero);
            part.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh CreatePrism(int sides)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var i = 0; i < sides; i++) { var a = 2f * Mathf.PI * i / sides; vertices.Add(new Vector3(.36f * Mathf.Cos(a), -.45f, .36f * Mathf.Sin(a))); }
            for (var i = 0; i < sides; i++) { var a = 2f * Mathf.PI * i / sides; vertices.Add(new Vector3(.5f * Mathf.Cos(a), .2f, .5f * Mathf.Sin(a))); }
            var top = vertices.Count; vertices.Add(new Vector3(0f, .68f, 0f));
            var bottom = vertices.Count; vertices.Add(new Vector3(0f, -.5f, 0f));
            for (var i = 0; i < sides; i++) { var n = (i + 1) % sides; triangles.AddRange(new[] { bottom, n, i, i, n, sides + i, n, sides + n, sides + i, sides + i, sides + n, top }); }
            var mesh = new Mesh { name = "magenheim.fracture.crystal-golem.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
