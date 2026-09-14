using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;

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
            Icon = CrystalBannerIcons.Icon(style, element),
            Requirements = new[]
            {
                Cost("FineWood", 2),
                Cost("LinenThread", style == CrystalBannerVisuals.BannerStyle.Pennant ? 3 : 4),
                Cost($"Magenheim_Crystal_{element}_Simple", 1),
            }
        };

        var custom = new CustomPiece(prefabName, "piece_banner01", config);
        custom.Piece.m_dlc = string.Empty;
        CrystalBannerVisuals.Apply(custom.PiecePrefab, style, element);

        if (!PieceManager.Instance.AddPiece(custom))
            throw new InvalidOperationException($"Jotunn refused crystal banner piece '{prefabName}'.");
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
