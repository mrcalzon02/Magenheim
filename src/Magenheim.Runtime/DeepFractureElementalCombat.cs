using System;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal sealed class DeepFractureElementalCombat : MonoBehaviour
    {
        private Character _owner = null!;
        private ElementalAlignment _alignment;
        private DeepFractureCrystalComponent? _focus;

        internal static void Attach(GameObject prefab, ElementalAlignment alignment)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var owner = prefab.GetComponent<Character>() ?? throw new InvalidOperationException("Aligned Deep Fracture creature has no Character.");
            var view = prefab.GetComponent<ZNetView>() ?? throw new InvalidOperationException("Aligned Deep Fracture creature has no ZNetView.");
            var runtime = prefab.GetComponent<DeepFractureElementalCombat>() ?? prefab.AddComponent<DeepFractureElementalCombat>();
            runtime._owner = owner;
            runtime._alignment = alignment;
            runtime._focus = CreateFocus(prefab, owner, view, alignment);
        }

        internal void ModifyOutgoingHit(HitData hit)
        {
            if (hit == null || _owner == null || _focus == null || !_focus.IsFunctionIntact()) return;
            var bonus = Mathf.Max(1f, hit.GetTotalDamage() * 0.22f);
            switch (_alignment)
            {
                case ElementalAlignment.Fire: hit.m_damage.m_fire += bonus; break;
                case ElementalAlignment.Frost: hit.m_damage.m_frost += bonus; break;
                case ElementalAlignment.Storm: hit.m_damage.m_lightning += bonus; break;
                case ElementalAlignment.Earth: hit.m_damage.m_blunt += bonus; break;
                case ElementalAlignment.Venom: hit.m_damage.m_poison += bonus; break;
                case ElementalAlignment.Radiance: hit.m_damage.m_spirit += bonus; break;
                case ElementalAlignment.Seidr: hit.m_damage.m_spirit += bonus * .7f; hit.m_damage.m_poison += bonus * .3f; break;
                case ElementalAlignment.Spirit: hit.m_damage.m_spirit += bonus; break;
            }
        }

        private static DeepFractureCrystalComponent CreateFocus(GameObject prefab, Character owner, ZNetView view, ElementalAlignment alignment)
        {
            var focus = new GameObject("elemental-focus") { layer = prefab.layer };
            focus.transform.SetParent(prefab.transform, false);
            focus.transform.localPosition = new Vector3(0f, 1.05f, -.22f);
            focus.transform.localScale = new Vector3(.24f, .38f, .24f);
            var meshFilter = focus.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = BuildFocusMesh();
            var renderer = focus.AddComponent<MeshRenderer>();
            var source = prefab.GetComponentInChildren<Renderer>(true);
            if (source != null && source.sharedMaterial != null)
            {
                var material = new Material(source.sharedMaterial) { name = "Magenheim_" + alignment + "_ElementalFocus" };
                var tint = ElementVisualPalette.Tint(alignment);
                if (material.HasProperty("_Color")) material.color = tint;
                if (material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", tint * 1.8f); }
                renderer.sharedMaterial = material;
            }
            return DeepFractureCrystalComponent.Attach(focus, owner, view, "elemental_focus", DeepFractureCrystalFunction.ElementalFocus, 95f);
        }

        private static Mesh BuildFocusMesh()
        {
            var mesh = new Mesh { name = "Magenheim_ElementalFocus" };
            mesh.vertices = new[] { new Vector3(0,.5f,0),new Vector3(.42f,0,0),new Vector3(0,0,.42f),new Vector3(-.42f,0,0),new Vector3(0,0,-.42f),new Vector3(0,-.5f,0) };
            mesh.triangles = new[] { 0,1,2,0,2,3,0,3,4,0,4,1,5,2,1,5,3,2,5,4,3,5,1,4 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
