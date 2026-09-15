using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Magenheim.Core;
using Magenheim.Core.Socketing;

namespace Magenheim.Runtime.Compatibility;

/// <summary>
/// Read-only adapter over Jewelcrafting's public GetGems(ItemData) API. It recognizes only
/// authoritative Magenheim crystal prefab identities and ignores every unrelated Jewelcrafting
/// or third-party gem. Jewelcrafting remains the socket persistence authority.
/// </summary>
internal static class JewelcraftingSocketReader
{
    private const string CrystalPrefix = "Magenheim_Crystal_";

    internal static bool TryReadMagenheimCrystals(
        ItemDrop.ItemData item,
        ManualLogSource log,
        out IReadOnlyList<Crystal> crystals)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        if (log is null) throw new ArgumentNullException(nameof(log));

        crystals = Array.Empty<Crystal>();

        try
        {
            Type apiType = JewelcraftingCompatibility.RequireApiType();
            MethodInfo getGems = apiType.GetMethod(
                "GetGems",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(ItemDrop.ItemData) },
                null)
                ?? throw new MissingMethodException(apiType.FullName, "GetGems(ItemDrop.ItemData)");

            if (getGems.Invoke(null, new object[] { item }) is not IEnumerable gems)
                throw new InvalidOperationException("Jewelcrafting GetGems returned a non-enumerable result.");

            var resolved = new List<Crystal>();
            foreach (object? gem in gems)
            {
                if (gem is null)
                    continue;

                FieldInfo? prefabField = gem.GetType().GetField("gemPrefab", BindingFlags.Public | BindingFlags.Instance);
                if (prefabField?.GetValue(gem) is not string prefabName || string.IsNullOrWhiteSpace(prefabName))
                    continue;

                if (!prefabName.StartsWith(CrystalPrefix, StringComparison.Ordinal))
                    continue;

                if (!TryParseCrystal(prefabName, out var crystal))
                {
                    log.LogWarning($"Ignoring unrecognized Magenheim crystal identity returned by Jewelcrafting: '{prefabName}'.");
                    continue;
                }

                // Rough crystals are valid Magenheim production items but are intentionally not
                // valid equipment payloads. Refuse their effects even if another mod inserts one.
                if (crystal.Tier == CrystalTier.Rough)
                {
                    log.LogWarning($"Ignoring Rough crystal '{prefabName}' found in a Jewelcrafting equipment socket.");
                    continue;
                }

                resolved.Add(crystal);
            }

            crystals = resolved;
            return true;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            log.LogWarning($"Unable to read Jewelcrafting socket state: {exception.InnerException.Message}");
            return false;
        }
        catch (Exception exception)
        {
            log.LogWarning($"Unable to read Jewelcrafting socket state: {exception.Message}");
            return false;
        }
    }

    internal static bool TryParseCrystal(string prefabName, out Crystal crystal)
    {
        crystal = default!;
        if (string.IsNullOrWhiteSpace(prefabName) || !prefabName.StartsWith(CrystalPrefix, StringComparison.Ordinal))
            return false;

        string identity = prefabName.Substring(CrystalPrefix.Length);
        int separator = identity.LastIndexOf('_');
        if (separator <= 0 || separator >= identity.Length - 1)
            return false;

        string elementToken = identity.Substring(0, separator);
        string tierToken = identity.Substring(separator + 1);
        if (!Enum.TryParse(elementToken, false, out ElementalAlignment element)
            || !Enum.IsDefined(typeof(ElementalAlignment), element)
            || !Enum.TryParse(tierToken, false, out CrystalTier tier)
            || !Enum.IsDefined(typeof(CrystalTier), tier))
            return false;

        crystal = new Crystal(element, tier);
        return true;
    }
}
