using System;
using System.Collections.Generic;

namespace Magenheim.Core.Socketing;

/// <summary>
/// Identifies the storage/UI backend that owns equipment sockets. Crystal identity,
/// Crystal Shaping, refinement, architecture, furniture and production consumption
/// remain Magenheim-owned regardless of the selected equipment backend.
/// </summary>
public enum EquipmentSocketBackend
{
    Magenheim = 0,
    Jewelcrafting = 1,
}

public sealed record EquipmentSocketProviderContext(
    bool JewelcraftingPresent,
    bool JewelcraftingCompatible,
    bool JewelcraftingIntegrationEnabled,
    bool PreferJewelcrafting,
    bool FailClosedOnInteropError);

public sealed record EquipmentSocketProviderResolution(
    EquipmentSocketBackend Backend,
    bool MayMutateEquipmentSockets,
    bool UsesMagenheimMetadata,
    bool UsesExternalStorage,
    string Diagnostic);

/// <summary>
/// Pure routing authority for equipment socket ownership. This intentionally does not
/// own crystals or crystal effects: it only chooses the equipment storage backend.
/// </summary>
public static class EquipmentSocketProviderRouter
{
    public static EquipmentSocketProviderResolution Resolve(EquipmentSocketProviderContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (!context.JewelcraftingIntegrationEnabled || !context.JewelcraftingPresent)
            return Magenheim("Jewelcrafting integration is inactive; Magenheim fallback socketing owns equipment storage.");

        if (!context.JewelcraftingCompatible)
        {
            if (context.FailClosedOnInteropError)
            {
                return new EquipmentSocketProviderResolution(
                    EquipmentSocketBackend.Magenheim,
                    false,
                    true,
                    false,
                    "Jewelcrafting is present but its compatibility boundary is unavailable; equipment socket mutation is blocked to prevent competing metadata.");
            }

            return Magenheim("Jewelcrafting compatibility is unavailable; configuration permits the Magenheim fallback backend.");
        }

        if (!context.PreferJewelcrafting)
            return Magenheim("Both socket providers are available; configuration explicitly prefers the Magenheim fallback backend.");

        return new EquipmentSocketProviderResolution(
            EquipmentSocketBackend.Jewelcrafting,
            true,
            false,
            true,
            "Jewelcrafting owns equipment socket storage/UI; Magenheim retains crystal identity, shaping, refinement and non-equipment consumption.");
    }

    private static EquipmentSocketProviderResolution Magenheim(string diagnostic) => new(
        EquipmentSocketBackend.Magenheim,
        true,
        true,
        false,
        diagnostic);
}

/// <summary>
/// Provider-neutral snapshot used by migration and runtime adapters. External provider
/// metadata that Magenheim does not understand must remain opaque and be preserved by
/// the adapter rather than normalized into this model.
/// </summary>
public sealed record EquipmentSocketSnapshot(
    int Capacity,
    IReadOnlyList<Crystal> MagenheimCrystals,
    string ProviderStateToken)
{
    public void Validate()
    {
        if (Capacity < 0 || Capacity > SocketState.MaximumSupportedSlots)
            throw new InvalidOperationException($"Socket capacity must be between 0 and {SocketState.MaximumSupportedSlots}.");
        if (MagenheimCrystals is null)
            throw new InvalidOperationException("Magenheim crystal collection is required.");
        if (MagenheimCrystals.Count > Capacity)
            throw new InvalidOperationException("Installed Magenheim crystal count exceeds provider capacity.");
        if (ProviderStateToken is null)
            throw new InvalidOperationException("Provider state token is required for optimistic migration validation.");
    }
}