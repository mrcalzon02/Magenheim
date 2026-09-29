using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldDungeonRoomRole
{
    Entrance,
    MainRoute,
    Junction,
    Vertical,
    Hazard,
    Resource,
    Encounter,
    Landmark,
}

public sealed record UnderworldDungeonRoomDefinition(
    string Id,
    string ModelId,
    string DisplayName,
    UnderworldDungeonRoomRole Role,
    double WidthMeters,
    double DepthMeters,
    double HeightMeters)
{
    public void Validate(string requiredPrefix, string requiredModelPrefix)
    {
        if (string.IsNullOrWhiteSpace(Id) ||
            !Id.StartsWith(requiredPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Dungeon room id '{Id}' must use prefix '{requiredPrefix}'.");
        if (string.IsNullOrWhiteSpace(ModelId) ||
            !ModelId.StartsWith(requiredModelPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Dungeon room model '{ModelId}' must use prefix '{requiredModelPrefix}'.");
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new InvalidOperationException("Dungeon room display name is required.");
        if (!Enum.IsDefined(typeof(UnderworldDungeonRoomRole), Role))
            throw new InvalidOperationException($"Unknown dungeon room role '{Role}'.");
        Positive(WidthMeters, nameof(WidthMeters));
        Positive(DepthMeters, nameof(DepthMeters));
        Positive(HeightMeters, nameof(HeightMeters));
    }

    private static void Positive(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            throw new InvalidOperationException(name + " must be finite and greater than zero.");
    }
}

/// <summary>
/// First ordinary biome-dungeon production kit. These are asset/runtime contracts, not permission
/// to spawn the Rootwarren before the sixteen authored room payloads and interior binder exist.
/// </summary>
public static class UnderworldFungalRootwarrenCatalog
{
    public const string RoomIdPrefix = "magenheim.underworld.dungeon.fungal_forest.room.";
    public const string ModelIdPrefix = "underworld-dungeon-fungal-rootwarren-";

    public static IReadOnlyList<UnderworldDungeonRoomDefinition> Rooms { get; } =
        Array.AsReadOnly(new[]
        {
            Room("fracture-mouth", "Fracture Mouth", UnderworldDungeonRoomRole.Entrance, 28, 32, 18),
            Room("mycelial-gallery", "Mycelial Gallery", UnderworldDungeonRoomRole.MainRoute, 34, 46, 20),
            Room("glowcap-vault", "Glowcap Vault", UnderworldDungeonRoomRole.Landmark, 42, 42, 28),
            Room("spore-basin", "Spore Basin", UnderworldDungeonRoomRole.Hazard, 38, 44, 18),
            Room("root-bridge", "Root Bridge", UnderworldDungeonRoomRole.MainRoute, 24, 52, 24),
            Room("sunken-nursery", "Sunken Nursery", UnderworldDungeonRoomRole.Resource, 40, 38, 16),
            Room("tangle-junction", "Tangle Junction", UnderworldDungeonRoomRole.Junction, 38, 38, 22),
            Room("shelf-drop", "Shelf Drop", UnderworldDungeonRoomRole.Vertical, 30, 34, 38),
            Room("amber-grotto", "Amber Grotto", UnderworldDungeonRoomRole.Resource, 32, 36, 20),
            Room("worldroot-hollow", "Worldroot Hollow", UnderworldDungeonRoomRole.Landmark, 46, 48, 34),
            Room("crawler-nest", "Capcrawler Nest", UnderworldDungeonRoomRole.Encounter, 34, 36, 16),
            Room("stalker-den", "Mycelial Stalker Den", UnderworldDungeonRoomRole.Encounter, 36, 42, 20),
            Room("puffback-graze", "Puffback Graze", UnderworldDungeonRoomRole.Encounter, 44, 46, 18),
            Room("buried-archway", "Buried Archway", UnderworldDungeonRoomRole.MainRoute, 30, 40, 22),
            Room("root-squeeze", "Root Squeeze", UnderworldDungeonRoomRole.Hazard, 22, 38, 14),
            Room("heartcap-sanctum", "Heartcap Sanctum", UnderworldDungeonRoomRole.Landmark, 48, 50, 30),
        });

    public static void Validate()
    {
        if (Rooms.Count < UnderworldDungeonCatalog.FungalForest.TargetRoomFamilyMinimum ||
            Rooms.Count > UnderworldDungeonCatalog.FungalForest.TargetRoomFamilyMaximum)
            throw new InvalidOperationException(
                "Rootwarren room kit must stay inside the Fungal dungeon 15-20 family production target.");

        foreach (var room in Rooms)
            room.Validate(RoomIdPrefix, ModelIdPrefix);

        if (Rooms.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Rootwarren room ids must be unique.");
        if (Rooms.Select(value => value.ModelId).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Rootwarren model ids must be unique.");

        if (!Rooms.Any(value => value.Role == UnderworldDungeonRoomRole.Entrance) ||
            !Rooms.Any(value => value.Role == UnderworldDungeonRoomRole.Junction) ||
            !Rooms.Any(value => value.Role == UnderworldDungeonRoomRole.Vertical) ||
            !Rooms.Any(value => value.Role == UnderworldDungeonRoomRole.Hazard) ||
            !Rooms.Any(value => value.Role == UnderworldDungeonRoomRole.Resource) ||
            !Rooms.Any(value => value.Role == UnderworldDungeonRoomRole.Encounter) ||
            !Rooms.Any(value => value.Role == UnderworldDungeonRoomRole.Landmark))
            throw new InvalidOperationException(
                "Rootwarren room kit must cover entrance, junction, vertical, hazard, resource, encounter and landmark gameplay roles.");
    }

    public static IReadOnlyList<string> RoomFamilyIds() =>
        Array.AsReadOnly(Rooms.Select(value => value.Id).ToArray());

    private static UnderworldDungeonRoomDefinition Room(
        string suffix,
        string displayName,
        UnderworldDungeonRoomRole role,
        double width,
        double depth,
        double height) =>
        new(
            RoomIdPrefix + suffix,
            ModelIdPrefix + suffix,
            displayName,
            role,
            width,
            depth,
            height);
}
