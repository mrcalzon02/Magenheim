using System;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal sealed class DeepFractureElementalCombat : MonoBehaviour
    {
        private Character _owner = null!;
        private ElementalAlignment _alignment;

        internal static void Attach(GameObject prefab, ElementalAlignment alignment)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var owner = prefab.GetComponent<Character>() ?? throw new InvalidOperationException("Aligned Deep Fracture creature has no Character.");
            var runtime = prefab.GetComponent<DeepFractureElementalCombat>() ?? prefab.AddComponent<DeepFractureElementalCombat>();
            runtime._owner = owner;
            runtime._alignment = alignment;
        }

        internal void ModifyOutgoingHit(HitData hit)
        {
            if (hit == null || _owner == null) return;
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
    }
}
