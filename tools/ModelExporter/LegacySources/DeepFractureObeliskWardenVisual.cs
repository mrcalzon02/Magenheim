using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal static class DeepFractureObeliskWardenVisual
    {
        private static readonly Dictionary<int, Mesh> PrismMeshes = new Dictionary<int, Mesh>();

        internal static void Apply(GameObject prefab, ElementalAlignment alignment)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var character = prefab.GetComponent<Character>() ?? throw new InvalidOperationException("Obelisk Warden requires Character.");
            var view = prefab.GetComponent<ZNetView>() ?? throw new InvalidOperationException("Obelisk Warden requires ZNetView.");
            var original = prefab.GetComponentsInChildren<Renderer>(true);
            var source = original.Select(x => x.sharedMaterial).FirstOrDefault(x => x != null)
                ?? throw new InvalidOperationException("No creature material source: " + prefab.name);
            var tint = ElementVisualPalette.Tint(alignment);
            var stone = Material(source, "stone", Color.Lerp(tint, new Color(.18f, .19f, .22f), .76f), .18f);
            var core = Material(source, "crystal-core", Color.Lerp(tint, Color.white, .68f), 1.75f);
            var shard = Material(source, "crystal-shard", tint, 1.05f);
            var root = new GameObject("magenheim.fracture.creature.obelisk-warden.visual") { layer = prefab.layer };
            root.transform.SetParent(prefab.transform, false);
            root.transform.localPosition = new Vector3(0f, 2.65f, 0f);

            Prism(root, "central-obelisk", new Vector3(0f, .65f, 0f), .88f, 3.7f, 8, stone);
            var controlCore = Prism(root, "control-core", new Vector3(0f, 1.15f, .72f), .38f, .82f, 7, core, new Vector3(82f, 0f, 0f));
            var deathAnchor = Prism(root, "death-anchor", new Vector3(0f, -.15f, -.82f), .46f, 1.08f, 8, core, new Vector3(84f, 0f, 0f));
            DeepFractureCrystalComponent.Attach(controlCore, character, view, "warden_control", DeepFractureCrystalFunction.EnvironmentalControl, 180f);
            DeepFractureCrystalComponent.Attach(deathAnchor, character, view, "warden_death_anchor", DeepFractureCrystalFunction.DeathAnchor, 260f);
            Prism(root, "crown-spire", new Vector3(0f, 2.85f, 0f), .46f, 1.65f, 6, shard);
            Prism(root, "base-left", new Vector3(-.72f, -1.25f, 0f), .42f, 1.35f, 8, stone, new Vector3(0f, 0f, -16f));
            Prism(root, "base-right", new Vector3(.72f, -1.25f, 0f), .42f, 1.35f, 8, stone, new Vector3(0f, 0f, 16f));
            for (var i = 0; i < 4; i++)
            {
                var angle = Mathf.PI * .5f * i;
                Prism(root, "secondary-crystal-" + i, new Vector3(Mathf.Cos(angle) * 1.18f, .62f, Mathf.Sin(angle) * 1.18f), .19f, 1.28f, 6, shard, new Vector3(-18f, -angle * Mathf.Rad2Deg, 0f));
            }
            for (var i = 0; i < 8; i++)
            {
                var angle = Mathf.PI * .25f * i;
                Prism(root, "ground-prong-" + i, new Vector3(Mathf.Cos(angle) * 1.42f, -1.38f, Mathf.Sin(angle) * 1.42f), .12f, .72f, 5, shard, new Vector3(-48f, -angle * Mathf.Rad2Deg, 0f));
            }

            var light = root.AddComponent<Light>();
            light.color = tint;
            light.range = 10.5f;
            light.intensity = 1.85f;
            foreach (var renderer in original) if (!renderer.transform.IsChildOf(root.transform)) renderer.enabled = false;
            foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        }

        private static Material Material(Material source, string semantic, Color color, float emission)
        {
            var material = new Material(source) { name = "magenheim.fracture.obelisk-warden." + semantic };
            GeneratedSurfaceTextures.Apply(material, semantic);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
            material.DisableKeyword("_NORMALMAP");
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * emission);
                material.EnableKeyword("_EMISSION");
            }
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = 2000;
            return material;
        }

        private static GameObject Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material, Vector3? rotation = null)
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
            return part;
        }

        private static Mesh CreatePrism(int sides)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var i = 0; i < sides; i++)
            {
                var a = 2f * Mathf.PI * i / sides;
                vertices.Add(new Vector3(.36f * Mathf.Cos(a), -.45f, .36f * Mathf.Sin(a)));
            }
            for (var i = 0; i < sides; i++)
            {
                var a = 2f * Mathf.PI * i / sides;
                vertices.Add(new Vector3(.5f * Mathf.Cos(a), .2f, .5f * Mathf.Sin(a)));
            }
            var top = vertices.Count; vertices.Add(new Vector3(0f, .68f, 0f));
            var bottom = vertices.Count; vertices.Add(new Vector3(0f, -.5f, 0f));
            for (var i = 0; i < sides; i++)
            {
                var n = (i + 1) % sides;
                triangles.AddRange(new[]
                {
                    bottom, i, n,
                    i, sides + i, n,
                    n, sides + i, sides + n,
                    top, sides + n, sides + i,
                });
            }
            var mesh = new Mesh { name = "magenheim.fracture.obelisk-warden.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
