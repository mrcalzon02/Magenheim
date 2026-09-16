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
            var focus = ModelAssets.Load(prefab,"elemental-focus",hideOriginal:false);
            focus.name="elemental-focus";
            focus.transform.localPosition = new Vector3(0f, 1.05f, -.22f);
            focus.transform.localScale = new Vector3(.24f, .38f, .24f);
            var tint=ElementVisualPalette.Tint(alignment);
            foreach(var renderer in focus.GetComponentsInChildren<Renderer>(true)) {
                var material=new Material(renderer.sharedMaterial);material.color=tint;
                if(material.HasProperty("_EmissionColor")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",tint*1.8f);}renderer.sharedMaterial=material;
            }
            return DeepFractureCrystalComponent.Attach(focus, owner, view, "elemental_focus", DeepFractureCrystalFunction.ElementalFocus, 95f);
        }

    }
}
