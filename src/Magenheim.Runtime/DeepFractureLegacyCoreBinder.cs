using System;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal static class DeepFractureLegacyCoreBinder
    {
        internal static void BindAnnoyanceWisp(GameObject prefab){var c=Context(prefab,"Annoyance Wisp");Attach(c.Root,"motion-core",c.Owner,c.View,"movement",DeepFractureCrystalFunction.Movement,42f);}
        internal static void BindGeodeCrawler(GameObject prefab){var c=Context(prefab,"Geode Crawler");Attach(c.Root,"carapace-core",c.Owner,c.View,"armor",DeepFractureCrystalFunction.Armor,68f);}
        internal static void BindShardling(GameObject prefab){var c=Context(prefab,"Shardling");Attach(c.Root,"locomotion-core",c.Owner,c.View,"movement",DeepFractureCrystalFunction.Movement,52f);}
        internal static void BindStoneGuardian(GameObject prefab){BindDefensiveTriad(prefab,"Stone Guardian","resistance-core","armor-core-left","armor-core-right",260f,220f);}
        internal static void BindStoneSentinel(GameObject prefab){BindDefensiveTriad(prefab,"Stone Sentinel","resistance-core","armor-core-left","armor-core-right",190f,165f);}
        internal static void BindCrystalRevenant(GameObject prefab){BindDefensiveTriad(prefab,"Crystal Revenant","resistance-core","armor-core-l","armor-core-r",115f,90f);}
        internal static void BindCrystalHound(GameObject prefab){var c=Context(prefab,"Crystal Hound");Attach(c.Root,"motion-core",c.Owner,c.View,"movement",DeepFractureCrystalFunction.Movement,105f);Attach(c.Root,"resistance-core",c.Owner,c.View,"resistance",DeepFractureCrystalFunction.Resistance,85f);}
        internal static void BindBurrower(GameObject prefab){var c=Context(prefab,"Burrower");Attach(c.Root,"burrow-core",c.Owner,c.View,"burrow",DeepFractureCrystalFunction.Burrow,145f);Attach(c.Root,"armor-core",c.Owner,c.View,"armor",DeepFractureCrystalFunction.Armor,120f);}
        internal static void BindCrystalParasite(GameObject prefab){var c=Context(prefab,"Crystal Parasite");Attach(c.Root,"regrowth-core",c.Owner,c.View,"regeneration",DeepFractureCrystalFunction.Regeneration,70f);}
        internal static void BindFacetSentry(GameObject prefab){var c=Context(prefab,"Facet Sentry");Attach(c.Root,"control-core",c.Owner,c.View,"environmental_control",DeepFractureCrystalFunction.EnvironmentalControl,125f);}
        private static void BindDefensiveTriad(GameObject prefab,string displayName,string resistanceName,string leftArmorName,string rightArmorName,float resistanceHealth,float armorHealth){var c=Context(prefab,displayName);Attach(c.Root,resistanceName,c.Owner,c.View,"resistance",DeepFractureCrystalFunction.Resistance,resistanceHealth);Attach(c.Root,leftArmorName,c.Owner,c.View,"armor_left",DeepFractureCrystalFunction.Armor,armorHealth);Attach(c.Root,rightArmorName,c.Owner,c.View,"armor_right",DeepFractureCrystalFunction.Armor,armorHealth);}
        private sealed class BindingContext{internal readonly Transform Root;internal readonly Character Owner;internal readonly ZNetView View;internal BindingContext(Transform root,Character owner,ZNetView view){Root=root;Owner=owner;View=view;}}
        // The authored visual root is created by ModelAssets.Load as "magenheim.<model id>.visual",
        // and the model id carries the creature's elemental alignment, so it cannot be a constant
        // here. These binders previously looked for legacy "magenheim.fracture.creature.*.visual"
        // names that Load has never produced; the mismatch stayed invisible because the registrar
        // threw on its first chassis before any binder ran. Resolve by the naming convention and
        // require exactly one match, which fails just as loudly as the old constant did.
        private static BindingContext Context(GameObject prefab,string displayName){if(prefab==null)throw new ArgumentNullException(nameof(prefab));var owner=prefab.GetComponent<Character>()??throw new InvalidOperationException(displayName+" host has no Character.");var view=prefab.GetComponent<ZNetView>()??throw new InvalidOperationException(displayName+" host has no ZNetView.");return new BindingContext(FindVisualRoot(prefab.transform,displayName),owner,view);}
        private static Transform FindVisualRoot(Transform prefabRoot,string displayName){Transform? found=null;foreach(var candidate in prefabRoot.GetComponentsInChildren<Transform>(true)){var name=candidate.name;if(!name.StartsWith("magenheim.",StringComparison.Ordinal)||!name.EndsWith(".visual",StringComparison.Ordinal))continue;if(found!=null)throw new InvalidOperationException(displayName+" host carries more than one Magenheim visual root; binding target is ambiguous.");found=candidate;}return found??throw new InvalidOperationException(displayName+" host has no Magenheim visual root; the authored model was not applied.");}
        private static void Attach(Transform root,string name,Character owner,ZNetView view,string id,DeepFractureCrystalFunction function,float health){var part=Find(root,name);if(part.GetComponent<DeepFractureCrystalComponent>()!=null)throw new InvalidOperationException("Duplicate Deep Fracture crystal binding on '"+name+"'.");DeepFractureCrystalComponent.Attach(part.gameObject,owner,view,id,function,health);}
        private static Transform Find(Transform root,string name){if(root.name==name)return root;for(var i=0;i<root.childCount;i++){var match=TryFind(root.GetChild(i),name);if(match!=null)return match;}throw new InvalidOperationException("Required Deep Fracture visual part '"+name+"' is missing.");}
        private static Transform? TryFind(Transform root,string name){if(root.name==name)return root;for(var i=0;i<root.childCount;i++){var match=TryFind(root.GetChild(i),name);if(match!=null)return match;}return null;}
    }
}