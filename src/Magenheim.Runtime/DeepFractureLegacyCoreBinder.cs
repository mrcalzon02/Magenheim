using System;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal static class DeepFractureLegacyCoreBinder
    {
        internal static void BindStoneGuardian(GameObject prefab)
        {
            BindDefensiveTriad(prefab, "Stone Guardian", "magenheim.fracture.creature.stone-guardian.visual", "resistance-core", "armor-core-left", "armor-core-right", 260f, 220f);
        }

        internal static void BindStoneSentinel(GameObject prefab)
        {
            BindDefensiveTriad(prefab, "Stone Sentinel", "magenheim.fracture.creature.stone-sentinel.visual", "resistance-core", "armor-core-left", "armor-core-right", 190f, 165f);
        }

        internal static void BindCrystalRevenant(GameObject prefab)
        {
            BindDefensiveTriad(prefab, "Crystal Revenant", "magenheim.fracture.creature.crystal-revenant.visual", "resistance-core", "armor-core-l", "armor-core-r", 115f, 90f);
        }

        private static void BindDefensiveTriad(GameObject prefab, string displayName, string rootName, string resistanceName, string leftArmorName, string rightArmorName, float resistanceHealth, float armorHealth)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var owner = prefab.GetComponent<Character>() ?? throw new InvalidOperationException(displayName + " host has no Character.");
            var view = prefab.GetComponent<ZNetView>() ?? throw new InvalidOperationException(displayName + " host has no ZNetView.");
            var root = Find(prefab.transform, rootName);
            Attach(root, resistanceName, owner, view, "resistance", DeepFractureCrystalFunction.Resistance, resistanceHealth);
            Attach(root, leftArmorName, owner, view, "armor_left", DeepFractureCrystalFunction.Armor, armorHealth);
            Attach(root, rightArmorName, owner, view, "armor_right", DeepFractureCrystalFunction.Armor, armorHealth);
        }

        private static void Attach(Transform root, string name, Character owner, ZNetView view, string id, DeepFractureCrystalFunction function, float health)
        {
            var part = Find(root, name);
            if (part.GetComponent<DeepFractureCrystalComponent>() != null) throw new InvalidOperationException("Duplicate Deep Fracture crystal binding on '" + name + "'.");
            DeepFractureCrystalComponent.Attach(part.gameObject, owner, view, id, function, health);
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var match = Find(root.GetChild(i), name);
                if (match != null) return match;
            }
            throw new InvalidOperationException("Required Deep Fracture visual part '" + name + "' is missing.");
        }
    }
}
