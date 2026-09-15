using System;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal static class DeepFractureLegacyCoreBinder
    {
        internal static void BindStoneGuardian(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var owner = prefab.GetComponent<Character>() ?? throw new InvalidOperationException("Stone Guardian host has no Character.");
            var view = prefab.GetComponent<ZNetView>() ?? throw new InvalidOperationException("Stone Guardian host has no ZNetView.");
            var root = Find(prefab.transform, "magenheim.fracture.creature.stone-guardian.visual");
            Attach(root, "resistance-core", owner, view, "resistance", DeepFractureCrystalFunction.Resistance, 260f);
            Attach(root, "armor-core-left", owner, view, "armor_left", DeepFractureCrystalFunction.Armor, 220f);
            Attach(root, "armor-core-right", owner, view, "armor_right", DeepFractureCrystalFunction.Armor, 220f);
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
