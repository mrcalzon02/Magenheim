using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers three elemental crystal banner families across all eight alignments.</summary>
internal sealed class CrystalBannerRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal CrystalBannerRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterBanners;
        _subscribed = true;
    }

    private void RegisterBanners()
    {
        if (_registered) return;
        try
        {
            if (PrefabManager.Instance.GetPrefab("piece_banner01") is null)
                throw new InvalidOperationException("Vanilla banner source 'piece_banner01' is unavailable.");

            var count = 0;
            foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
            {
                RegisterBanner(element, CrystalBannerVisuals.BannerStyle.Standard);
                RegisterBanner(element, CrystalBannerVisuals.BannerStyle.Swallowtail);
                RegisterBanner(element, CrystalBannerVisuals.BannerStyle.Pennant);
                count += 3;
            }

            _registered = true;
            _log.LogInfo($"Registered {count} Magenheim crystal banners: Standard, Swallowtail, and Pennant across all eight elemental palettes.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Crystal banner registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterBanner(ElementalAlignment element, CrystalBannerVisuals.BannerStyle style)
    {
        var styleName = style.ToString();
        var prefabName = $"Magenheim_Banner_{styleName}_{element}";
        if (PrefabManager.Instance.GetPrefab(prefabName) is not null)
            throw new InvalidOperationException($"Cannot replace occupied banner identity '{prefabName}'.");

        var config = new PieceConfig
        {
            Name = $"{element} Crystal {styleName}",
            Description = $"A {styleName.ToLowerInvariant()} banner carrying {element}-aligned crystal colors and a faceted mineral crest.",
            PieceTable = "Hammer",
            Category = "Furniture",
            CraftingStation = "piece_workbench",
            Icon = EarthAssets.Icon(CrystalBannerVisuals.ModelId(style, element)),
            Requirements = new[]
            {
                Cost("FineWood", 2),
                Cost("LinenThread", style == CrystalBannerVisuals.BannerStyle.Pennant ? 3 : 4),
                Cost($"Magenheim_Crystal_{element}_Simple", 1),
            }
        };

        var custom = new CustomPiece(prefabName, "piece_banner01", config);
        custom.Piece.m_dlc = string.Empty;
        var visual = CrystalBannerVisuals.Apply(custom.PiecePrefab, style, element);
        ConfigureCollider(custom.PiecePrefab, visual);

        if (!PieceManager.Instance.AddPiece(custom))
            throw new InvalidOperationException($"Jotunn refused crystal banner piece '{prefabName}'.");
    }

    /// <summary>
    /// Replace the donor banner's collision with collision that matches the banner actually
    /// shown. ModelAssets.Load disables the donor's Renderers and LODGroups but never touches
    /// its colliders, so without this the piece collides with piece_banner01's shape while
    /// displaying Magenheim geometry.
    ///
    /// The box is measured from the loaded meshes rather than hand-written per style, because
    /// hand-written collider constants go stale silently the next time the model library is
    /// rebuilt, and nothing would catch it.
    /// </summary>
    private static void ConfigureCollider(GameObject prefab, GameObject visual)
    {
        var existing = prefab.GetComponentsInChildren<Collider>(true)
            .Where(collider => !collider.isTrigger)
            .ToArray();
        var layer = existing.Length > 0 ? existing[0].gameObject.layer : prefab.layer;
        foreach (var collider in existing) collider.enabled = false;

        var worldToPrefab = prefab.transform.worldToLocalMatrix;
        var minimum = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var maximum = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        var measured = false;
        foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = filter.sharedMesh;
            if (!mesh) continue;
            var bounds = mesh.bounds;
            var localToPrefab = worldToPrefab * filter.transform.localToWorldMatrix;
            for (var corner = 0; corner < 8; corner++)
            {
                var point = localToPrefab.MultiplyPoint3x4(bounds.center + Vector3.Scale(
                    bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));
                minimum = Vector3.Min(minimum, point);
                maximum = Vector3.Max(maximum, point);
                measured = true;
            }
        }

        if (!measured)
            throw new InvalidOperationException($"Crystal banner '{prefab.name}' has no mesh to measure collision from.");

        var root = new GameObject("magenheim.crystal-banner.collision") { layer = layer };
        root.transform.SetParent(prefab.transform, false);
        var box = root.AddComponent<BoxCollider>();
        box.center = (minimum + maximum) * .5f;
        // A banner is a pole with cloth hanging off it. Collide with the pole, not the cloth,
        // so the player is not blocked by a banner they are standing beside.
        var size = maximum - minimum;
        box.size = new Vector3(Mathf.Min(size.x, .32f), size.y, Mathf.Min(size.z, .32f));
    }

    private static RequirementConfig Cost(string item, int amount) =>
        new RequirementConfig(item, amount, 0, true);

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterBanners;
        _subscribed = false;
    }
}
