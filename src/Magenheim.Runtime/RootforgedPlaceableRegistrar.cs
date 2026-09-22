using System;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Authored Rootforged meshes on native Valheim construction and wear components.</summary>
internal sealed class RootforgedPlaceableRegistrar : IDisposable
{
    private readonly UnderworldArchitectureDefinitionSet _definitions;
    private readonly ManualLogSource _log;
    internal RootforgedPlaceableRegistrar(UnderworldArchitectureDefinitionSet definitions, ManualLogSource log)
    { _definitions = definitions; _log = log; }
    internal void Register() => PrefabManager.OnVanillaPrefabsAvailable += RegisterPieces;
    internal static string ModelId(UnderworldBuildPieceDefinition piece) =>
        "rootforged-" + piece.Id.Substring(UnderworldArchitectureValidator.BuildIdPrefix.Length).Replace('_', '-');
    private static string Resource(string id) => id switch
    {
        UnderworldArchitectureValidator.WorldrootTimberResourceId => "RoundLog",
        UnderworldArchitectureValidator.UnderstoneResourceId => "Stone",
        UnderworldArchitectureValidator.IronResourceId => "Iron",
        _ => throw new InvalidOperationException($"No live construction resource binding for {id}."),
    };
    private void RegisterPieces()
    {
        var count = 0;
        try
        {
            foreach (var definition in _definitions.Pieces)
            {
                try { RegisterPiece(definition); count++; }
                catch (Exception exception) { _log.LogError($"Rootforged piece {definition.PrefabName} failed: {exception}"); }
            }
            _log.LogInfo($"Registered {count}/{_definitions.Pieces.Count} Rootforged pieces in Hammer > Rootforged. Interim materials: core wood, stone and iron.");
        }
        finally { Dispose(); }
    }
    private static void RegisterPiece(UnderworldBuildPieceDefinition definition)
    {
        var stone = definition.Kind == UnderworldBuildPieceKind.Foundation || definition.Kind == UnderworldBuildPieceKind.Plinth;
        var reinforced = definition.Tier == UnderworldBuildTier.RootforgedIron;
        var donor = stone ? "stone_floor_2x2" : reinforced ? "woodiron_beam" : "wood_pole_log";
        var source = PrefabManager.Instance.GetPrefab(donor);
        if (!source || !source.GetComponent<Piece>() || !source.GetComponent<WearNTear>())
            throw new InvalidOperationException($"Construction donor {donor} unavailable.");
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName))
            throw new InvalidOperationException($"Occupied Rootforged identity {definition.PrefabName}.");
        var config = new PieceConfig
        {
            Name = definition.DisplayName,
            Description = "Grown root and stone construction. Uses core wood/stone until Underworld harvesting is available.",
            PieceTable = "Hammer", Category = "Rootforged",
            CraftingStation = definition.CraftingStationPrefabName,
            Icon = EarthAssets.Icon(ModelId(definition)),
            Requirements = definition.Costs.Select(cost => new RequirementConfig(Resource(cost.ResourceId), cost.Amount, 0, true)).ToArray(),
        };
        var custom = new CustomPiece(definition.PrefabName, donor, config);
        var prefab = custom.PiecePrefab;
        var donorColliders = prefab.GetComponentsInChildren<Collider>(true);
        var collisionLayer = donorColliders.FirstOrDefault(c => !c.isTrigger)?.gameObject.layer ?? prefab.layer;
        foreach (var collider in donorColliders) collider.enabled = false;
        var visual = ModelAssets.Load(prefab, ModelId(definition), materialSource: CrystalArchitectureVisuals.SurfaceDonorMaterial());
        var collisionRoot = new GameObject("rootforged-collision") { layer = collisionLayer };
        collisionRoot.transform.SetParent(prefab.transform, false);
        // Collision is independent of the native wear visual's active state.
        foreach (var collider in visual.GetComponentsInChildren<MeshCollider>(true))
        {
            collisionRoot.AddComponent<MeshCollider>().sharedMesh = collider.sharedMesh;
            UnityEngine.Object.DestroyImmediate(collider);
        }
        var wear = prefab.GetComponent<WearNTear>();
        wear.m_materialType = stone ? WearNTear.MaterialType.Stone : reinforced ? WearNTear.MaterialType.Iron : WearNTear.MaterialType.HardWood;
        wear.m_new = visual;
        wear.m_worn = UnityEngine.Object.Instantiate(visual, prefab.transform);
        wear.m_broken = UnityEngine.Object.Instantiate(visual, prefab.transform);
        wear.m_worn.name = "rootforged-worn"; wear.m_broken.name = "rootforged-weathered";
        Darken(wear.m_worn, .8f); Darken(wear.m_broken, .6f);
        wear.m_worn.SetActive(false); wear.m_broken.SetActive(false);
        wear.m_fragmentRoots = new[] { wear.m_new, wear.m_worn, wear.m_broken };
        ConfigureSnaps(prefab, definition);
        if (!PieceManager.Instance.AddPiece(custom)) throw new InvalidOperationException("Jotunn rejected " + definition.PrefabName);
    }
    private static void Darken(GameObject root, float amount)
    {
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(amount, amount, amount, 1f));
            renderer.SetPropertyBlock(block);
        }
    }
    private static void ConfigureSnaps(GameObject prefab, UnderworldBuildPieceDefinition definition)
    {
        // Installed Piece.GetSnapPoints reads tagged DIRECT children, not nested transforms.
        foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
            if (child != prefab.transform && child.CompareTag("snappoint")) child.tag = "Untagged";
        var width = definition.Dimensions.WidthMeters;
        var height = definition.Dimensions.HeightMeters;
        var depth = definition.Dimensions.DepthMeters;
        var number = 0;
        void Snap(float x, float y, float z)
        {
            var node = new GameObject("root-snap-" + number++) { tag = "snappoint" };
            node.transform.SetParent(prefab.transform, false); node.transform.localPosition = new Vector3(x,y,z);
        }
        switch (definition.Kind)
        {
            case UnderworldBuildPieceKind.Foundation:
            case UnderworldBuildPieceKind.Plinth:
                foreach (var y in new[] {0f, (float)height})
                    foreach (var x in new[] {-width*.5f,0f,width*.5f})
                        foreach (var z in new[] {-depth*.5f,0f,depth*.5f}) Snap(x,y,z);
                break;
            case UnderworldBuildPieceKind.Pillar:
                for (var y = 0; y <= height; y += 2)
                { Snap(0,y,0); Snap(-.5f,y,0); Snap(.5f,y,0); Snap(0,y,-.5f); Snap(0,y,.5f); }
                break;
            case UnderworldBuildPieceKind.ArchRib:
                Snap(-width*.5f+.4f,0,0); Snap(width*.5f-.4f,0,0);
                Snap(0,height,0); Snap(0,height-.5f,0);
                Snap(-width*.25f,height*.7f,0); Snap(width*.25f,height*.7f,0);
                break;
            default:
                for (var x = -width*.5f; x <= width*.5f; x += 2f)
                { Snap(x,.5f,0); Snap(x,0,0); Snap(x,1,0); }
                break;
        }
    }
    public void Dispose() => PrefabManager.OnVanillaPrefabsAvailable -= RegisterPieces;
}
