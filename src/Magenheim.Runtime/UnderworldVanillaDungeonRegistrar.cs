using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Builds the five ordinary Underworld dungeons from private clones of vanilla dungeon assets.
/// It never mutates vanilla DungeonDB rooms, locations, generators or door prefabs.
/// Deep Fracture deliberately remains outside this registrar.
/// </summary>
internal sealed class UnderworldVanillaDungeonRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly Dictionary<string, Dictionary<string, string>> _roomNames =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, DungeonGenerator> _generators =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _donorRequiredRooms =
        new(StringComparer.Ordinal);
    private readonly List<string> _registeredRooms = new();
    private readonly List<string> _registeredLocations = new();
    private readonly List<string> _registeredDoorPrefabs = new();
    private readonly List<GameObject> _themeCarriers = new();
    private bool _roomsSubscribed;
    private bool _locationsSubscribed;

    internal UnderworldVanillaDungeonRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        UnderworldVanillaDungeonReuseCatalog.Validate();
        var active = ActiveProfiles().ToArray();
        if (active.Length != UnderworldVanillaDungeonReuseCatalog.All.Count)
            throw new InvalidOperationException(
                $"Expanded vanilla Underworld dungeon admission is incomplete: {active.Length}/" +
                $"{UnderworldVanillaDungeonReuseCatalog.All.Count} ordinary donor families are RuntimeReady.");

        DungeonManager.OnVanillaRoomsAvailable += RegisterRooms;
        ZoneManager.OnVanillaLocationsAvailable += RegisterLocations;
        _roomsSubscribed = true;
        _locationsSubscribed = true;
    }

    private IEnumerable<UnderworldVanillaDungeonReuseDefinition> ActiveProfiles()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in UnderworldVanillaDungeonReuseCatalog.All)
        {
            if (Definition(profile).Status != UnderworldDungeonStatus.RuntimeReady) continue;
            seen.Add(profile.DungeonId);
            yield return profile;
        }


    }

    private void RegisterRooms()
    {
        try
        {
            foreach (var profile in ActiveProfiles())
                RegisterRoomFamily(profile);
        }
        catch (Exception exception)
        {
            _log.LogError("Expanded vanilla Underworld room registration failed: " + exception);
            throw;
        }
        finally
        {
            if (_roomsSubscribed)
            {
                DungeonManager.OnVanillaRoomsAvailable -= RegisterRooms;
                _roomsSubscribed = false;
            }
        }
    }

    private void RegisterRoomFamily(UnderworldVanillaDungeonReuseDefinition profile)
    {
        if (!Enum.TryParse(profile.DonorThemeName, ignoreCase: false, out Room.Theme donorTheme) ||
            donorTheme == Room.Theme.None)
            throw new InvalidOperationException(
                $"Vanilla dungeon donor theme '{profile.DonorThemeName}' is unavailable.");

        var themeName = ThemeName(profile);
        var carrier = new GameObject(themeName + "_ThemeCarrier");
        carrier.SetActive(false);
        carrier.AddComponent<DungeonGenerator>();
        if (!DungeonManager.Instance.RegisterDungeonTheme(carrier, themeName))
        {
            UnityEngine.Object.Destroy(carrier);
            throw new InvalidOperationException(
                $"Jotunn refused expanded vanilla dungeon theme '{themeName}'.");
        }
        _themeCarriers.Add(carrier);

        var sourceRooms = DungeonDB.GetRooms()
            .Where(data => data is not null && data.m_enabled &&
                           (data.m_theme & donorTheme) != Room.Theme.None)
            .ToArray();
        if (sourceRooms.Length == 0)
            throw new InvalidOperationException(
                $"Vanilla dungeon donor '{profile.DonorDisplayName}' resolved zero loaded room definitions.");

        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var population = PopulationRebindStats.Empty;
        var scale = (float)profile.LinearRoomScale;
        for (var index = 0; index < sourceRooms.Length; index++)
        {
            var data = sourceRooms[index];
            data.m_prefab.Load();
            try
            {
                var source = data.m_prefab.Asset;
                if (!source)
                    throw new InvalidOperationException(
                        $"Vanilla dungeon room soft reference '{data.m_prefab.Name}' did not load.");

                var clone = UnityEngine.Object.Instantiate(source);
                var sourceName = data.m_prefab.Name;
                clone.name = RoomPrefabName(profile, sourceName, index);

                var room = clone.GetComponent<Room>()
                    ?? throw new InvalidOperationException(
                        $"Cloned donor room '{sourceName}' has no Room component.");

                var auditMetadata = clone.AddComponent<UnderworldVanillaDungeonRoomAuditMetadata>();
                auditMetadata.DonorRoomName = sourceName;
                auditMetadata.DonorRoomSize = room.m_size;
                auditMetadata.DonorConnectionLocalPositions = room.GetConnections()
                    .Where(value => value)
                    .Select(value => value.transform.localPosition)
                    .ToArray();
                auditMetadata.LinearScale = scale;

                // Do NOT scale the Room root. DungeonGenerator.CalculateRoomPosRot reads raw
                // RoomConnection.localPosition and does not multiply it by the Room root scale.
                // Root scaling therefore makes the rendered socket disagree with placement math.
                // Expand the donor's direct-child construction envelope instead: connection
                // positions move 1.5x, structural child geometry grows 1.5x, while networked
                // gameplay props keep their normal physical size at the expanded positions.
                ScaleRoomHierarchy(clone, room, scale, sourceName);
                room.m_size = new Vector3Int(
                    Mathf.CeilToInt(room.m_size.x * scale),
                    Mathf.CeilToInt(room.m_size.y * scale),
                    Mathf.CeilToInt(room.m_size.z * scale));

                population += RebindPopulation(clone, profile, sourceName, index);

                var custom = new CustomRoom(
                    clone,
                    fixReference: false,
                    new RoomConfig(themeName)
                    {
                        Enabled = true,
                    });
                if (!DungeonManager.Instance.AddCustomRoom(custom))
                    throw new InvalidOperationException(
                        $"Jotunn refused cloned donor room '{clone.name}'.");

                _registeredRooms.Add(clone.name);
                names[sourceName] = clone.name;
            }
            finally
            {
                data.m_prefab.Release();
            }
        }

        _roomNames[profile.DungeonId] = names;
        if (_generators.TryGetValue(profile.DungeonId, out var existingGenerator))
            RemapRequiredRooms(profile, existingGenerator);
        _log.LogInfo(
            $"DDE rooms {profile.Biome}: donor={profile.DonorDisplayName} theme={profile.DonorThemeName} " +
            $"enabled-donor-rooms={sourceRooms.Length} clones={names.Count} scale={profile.LinearRoomScale:0.##}x " +
            $"creature-spawners={population.Ecology.ActiveCreatureSpawners}/{population.Ecology.CreatureSpawnerSockets} " +
            $"spawn-areas={population.Ecology.ActiveSpawnAreas}/{population.Ecology.SpawnAreaSockets} " +
            $"roles=swarm:{population.Ecology.SwarmBindings},skirmisher:{population.Ecology.SkirmisherBindings}," +
            $"hunter:{population.Ecology.HunterBindings},bruiser:{population.Ecology.BruiserBindings}," +
            $"heavy:{population.Ecology.HeavyBindings},apex:{population.Ecology.ApexBindings} " +
            $"containers={population.Rewards.Containers} pickables={population.Rewards.Pickables} " +
            $"bottleneck-pickables={population.Rewards.BottleneckPickables} " +
            $"mineables={population.Rewards.Mineables} destructible-drops={population.Rewards.DestructibleDrops} " +
            $"dressing=materials:{population.Dressing.ArchitectureMaterials}," +
            $"structural:{population.Dressing.StructuralOverlays},major:{population.Dressing.MajorProps}," +
            $"ground:{population.Dressing.GroundProps},lights:{population.Dressing.LocalLights}," +
            $"atmosphere:{population.Dressing.AtmosphereVolumes},thermal:{population.Dressing.ThermalVolumes}," +
            $"water:{population.Dressing.WaterVolumes},frozen-exposure:{population.Dressing.FrozenExposureVolumes}," +
            $"decay-contamination:{population.Dressing.DecayContaminationVolumes} " +
            $"donor-mechanics=doors:{population.Mechanics.DoorsPreserved},teleports:{population.Mechanics.TeleportsPreserved}," +
            $"random-spawns:{population.Mechanics.RandomSpawnsPreserved},destructibles:{population.Mechanics.DestructiblesPreserved} " +
            $"lore-markers-removed={population.Mechanics.LoreMarkersRemoved}.");
    }

    private void RegisterLocations()
    {
        try
        {
            foreach (var profile in ActiveProfiles())
                RegisterLocation(profile);
        }
        catch (Exception exception)
        {
            _log.LogError("Expanded vanilla Underworld location registration failed: " + exception);
            throw;
        }
        finally
        {
            if (_locationsSubscribed)
            {
                ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
                _locationsSubscribed = false;
            }
        }
    }

    private void RegisterLocation(UnderworldVanillaDungeonReuseDefinition profile)
    {
        var definition = Definition(profile);
        if (ZoneManager.Instance.GetZoneLocation(definition.PrefabName) is not null ||
            CustomLocation.IsCustomLocation(definition.PrefabName))
            throw new InvalidOperationException(
                $"Occupied expanded vanilla dungeon identity '{definition.PrefabName}'.");

        var donorEntrance = profile.DonorEntrancePrefabs
            .FirstOrDefault(name => ZoneManager.Instance.GetZoneLocation(name) is not null)
            ?? throw new InvalidOperationException(
                $"{definition.DisplayName} found none of its vanilla entrance donors: " +
                string.Join(", ", profile.DonorEntrancePrefabs));

        var custom = ZoneManager.Instance.CreateClonedLocation(definition.PrefabName, donorEntrance)
            ?? throw new InvalidOperationException(
                $"Jotunn could not clone vanilla dungeon entrance '{donorEntrance}'.");
        _registeredLocations.Add(definition.PrefabName);

        // The entrance is deliberately vanilla-derived. Preserve its proven placement envelope
        // instead of imposing the retired bespoke-dungeon 18m/36m terrain settings.
        var donorZone = ZoneManager.Instance.GetZoneLocation(donorEntrance)
            ?? throw new InvalidOperationException(
                $"Resolved donor entrance '{donorEntrance}' disappeared before placement-policy capture.");
        var donorExteriorRadius = donorZone.m_exteriorRadius;
        var donorMinTerrainDelta = donorZone.m_minTerrainDelta;
        var donorMaxTerrainDelta = donorZone.m_maxTerrainDelta;
        var donorMinAltitude = donorZone.m_minAltitude;
        var donorMaxAltitude = donorZone.m_maxAltitude;
        var donorForestThresholdMin = donorZone.m_forestTresholdMin;
        var donorForestThresholdMax = donorZone.m_forestTresholdMax;
        var donorSlopeRotation = donorZone.m_slopeRotation;
        var donorSnapToWater = donorZone.m_snapToWater;

        var generator = custom.Prefab.GetComponentInChildren<DungeonGenerator>(true)
            ?? throw new InvalidOperationException(
                $"Cloned vanilla entrance '{donorEntrance}' has no DungeonGenerator.");
        if (!string.Equals(generator.gameObject.name, profile.DonorGeneratorPrefab, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Vanilla entrance '{donorEntrance}' resolved generator '{generator.gameObject.name}', " +
                $"expected '{profile.DonorGeneratorPrefab}'.");

        ValidateInteriorTransport(profile, donorZone, custom.Prefab, generator);
        var entranceDressing = UnderworldVanillaDungeonBiomeDressingPolicy.ApplyEntrance(
            custom.Prefab,
            profile);

        // A private theme is mandatory. Leaving the donor bitmask active would mix ordinary
        // vanilla-size rooms back into the 1.5x Magenheim room family.
        if (!DungeonManager.Instance.RegisterDungeonTheme(custom.Prefab, ThemeName(profile)))
            throw new InvalidOperationException(
                $"Jotunn refused private dungeon theme on '{definition.PrefabName}'.");
        generator.m_themes = Room.Theme.None;

        var vanillaMin = generator.m_minRooms;
        var vanillaMax = generator.m_maxRooms;
        var donorZoneSize = generator.m_zoneSize;
        var donorTileWidth = generator.m_tileWidth;
        var donorGridSize = generator.m_gridSize;
        var donorRequired = generator.m_requiredRooms?.ToArray() ?? Array.Empty<string>();
        var donorMinRequired = generator.m_minRequiredRooms;
        if (vanillaMin < 1 || vanillaMax < vanillaMin)
            throw new InvalidOperationException(
                $"Vanilla generator '{profile.DonorGeneratorPrefab}' has invalid room bounds {vanillaMin}-{vanillaMax}.");

        generator.m_minRooms = profile.ExpandedMinimumRooms(vanillaMin);
        generator.m_maxRooms = profile.ExpandedMaximumRooms(vanillaMax);

        // More rooms plus larger room footprints require a larger legal packing volume. Use a
        // derived linear scale rather than a magic fixed zone size so donor updates remain valid.
        var zoneScale = (float)(profile.LinearRoomScale * Math.Sqrt(profile.RoomCountMultiplier));
        generator.m_zoneSize *= zoneScale;
        generator.m_tileWidth *= (float)profile.LinearRoomScale;

        _generators[profile.DungeonId] = generator;
        _donorRequiredRooms[profile.DungeonId] = donorRequired;
        RemapRequiredRooms(profile, generator);

        var clonedDoors = CloneAndScaleDoors(generator, profile);

        var zone = custom.ZoneLocation;
        zone.m_biome = UnderworldTerrainRuntime.ToNativeBiome(profile.Biome);
        zone.m_biomeArea = JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All);
        zone.m_quantity = definition.Quantity;
        zone.m_prioritized = false;
        zone.m_minDistanceFromSimilar = (float)definition.MinDistanceFromSimilarMeters;
        zone.m_group = definition.Id;
        zone.m_unique = false;

        // DDE-09: terrain placement stays donor-native. Explicit assignments make this invariant
        // visible in diagnostics and protect it from future generic-location refactors.
        zone.m_exteriorRadius = donorExteriorRadius;
        zone.m_minTerrainDelta = donorMinTerrainDelta;
        zone.m_maxTerrainDelta = donorMaxTerrainDelta;
        zone.m_minAltitude = donorMinAltitude;
        zone.m_maxAltitude = donorMaxAltitude;
        zone.m_forestTresholdMin = donorForestThresholdMin;
        zone.m_forestTresholdMax = donorForestThresholdMax;
        zone.m_slopeRotation = donorSlopeRotation;
        zone.m_snapToWater = donorSnapToWater;

        _log.LogInfo(
            $"DDE location {definition.DisplayName}: entrance={donorEntrance} generator={profile.DonorGeneratorPrefab} " +
            $"theme={profile.DonorThemeName} donor-rooms={vanillaMin}-{vanillaMax} " +
            $"expanded-rooms={generator.m_minRooms}-{generator.m_maxRooms} room-scale={profile.LinearRoomScale:0.##}x " +
            $"zone={donorZoneSize}->{generator.m_zoneSize} zone-scale={zoneScale:0.##}x " +
            $"tile-width={donorTileWidth:0.###}->{generator.m_tileWidth:0.###} grid={donorGridSize} " +
            $"required=[{string.Join(",", donorRequired)}] min-required={donorMinRequired} " +
            $"remapped=[{string.Join(",", generator.m_requiredRooms ?? new List<string>())}] " +
            $"doors-cloned={clonedDoors} custom-interior:{generator.m_useCustomInteriorTransform} " +
            $"teleports:{CollectDungeonTeleports(custom.Prefab, custom.Prefab.GetComponent<Location>()).Count} " +
            $"terrain=radius:{zone.m_exteriorRadius:0.##}," +
            $"delta:{zone.m_minTerrainDelta:0.##}-{zone.m_maxTerrainDelta:0.##}," +
            $"altitude:{zone.m_minAltitude:0.##}-{zone.m_maxAltitude:0.##}," +
            $"slope-rotation:{zone.m_slopeRotation},snap-water:{zone.m_snapToWater} " +
            $"entrance-dressing=materials:{entranceDressing.ArchitectureMaterials}," +
            $"structural:{entranceDressing.StructuralOverlays},major:{entranceDressing.MajorProps}," +
            $"ground:{entranceDressing.GroundProps}," +
            $"lights:{entranceDressing.LocalLights}.");
    }

    private static void ValidateInteriorTransport(
        UnderworldVanillaDungeonReuseDefinition profile,
        ZoneSystem.ZoneLocation donorZone,
        GameObject clonePrefab,
        DungeonGenerator cloneGenerator)
    {
        donorZone.m_prefab.Load();
        try
        {
            var donorPrefab = donorZone.m_prefab.Asset
                ?? throw new InvalidOperationException(
                    $"Donor location '{profile.DonorDisplayName}' prefab could not be loaded.");
            var donorLocation = donorPrefab.GetComponent<Location>()
                ?? throw new InvalidOperationException(
                    $"Donor location '{profile.DonorDisplayName}' has no Location component.");
            var cloneLocation = clonePrefab.GetComponent<Location>()
                ?? throw new InvalidOperationException(
                    $"Cloned location '{profile.DungeonId}' has no Location component.");
            var donorGenerator = donorPrefab.GetComponentInChildren<DungeonGenerator>(true)
                ?? throw new InvalidOperationException(
                    $"Donor location '{profile.DonorDisplayName}' has no DungeonGenerator.");

            if (!donorLocation.m_hasInterior || !cloneLocation.m_hasInterior)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} derivative must preserve the donor's real dungeon interior.");
            if (cloneLocation.m_useCustomInteriorTransform != donorLocation.m_useCustomInteriorTransform)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} clone changed Location.m_useCustomInteriorTransform.");
            if (cloneGenerator.m_useCustomInteriorTransform != donorGenerator.m_useCustomInteriorTransform)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} clone changed DungeonGenerator.m_useCustomInteriorTransform.");
            if (cloneLocation.m_useCustomInteriorTransform != cloneGenerator.m_useCustomInteriorTransform)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} clone has mismatched Location/DungeonGenerator custom-interior flags.");
            if (cloneLocation.m_useCustomInteriorTransform && !cloneLocation.m_interiorTransform)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} requires a custom interior transform but the clone lost it.");
            if (!cloneLocation.m_interiorPrefab)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} clone lost its interior prefab.");
            if (cloneLocation.m_generator != cloneGenerator)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} clone Location.m_generator no longer points at the cloned generator.");

            var donorTeleports = CollectDungeonTeleports(donorPrefab, donorLocation);
            var cloneTeleports = CollectDungeonTeleports(clonePrefab, cloneLocation);
            if (donorTeleports.Count < 2)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} donor exposes only {donorTeleports.Count} dungeon Teleport endpoint(s); expected a paired entrance/exit.");
            if (cloneTeleports.Count != donorTeleports.Count)
                throw new InvalidOperationException(
                    $"{profile.DonorDisplayName} clone changed dungeon Teleport endpoint count " +
                    $"from {donorTeleports.Count} to {cloneTeleports.Count}.");

            foreach (var teleport in cloneTeleports)
            {
                if (!teleport || !teleport.m_targetPoint)
                    throw new InvalidOperationException(
                        $"{profile.DonorDisplayName} clone contains an unpaired Teleport endpoint.");
                if (!cloneTeleports.Contains(teleport.m_targetPoint))
                    throw new InvalidOperationException(
                        $"{profile.DonorDisplayName} clone Teleport targets an endpoint outside its cloned location/interior graph.");
            }
        }
        finally
        {
            donorZone.m_prefab.Release();
        }
    }

    private static HashSet<Teleport> CollectDungeonTeleports(GameObject root, Location location)
    {
        var result = new HashSet<Teleport>();
        foreach (var teleport in root.GetComponentsInChildren<Teleport>(true))
            if (teleport) result.Add(teleport);
        if (location.m_interiorPrefab)
            foreach (var teleport in location.m_interiorPrefab.GetComponentsInChildren<Teleport>(true))
                if (teleport) result.Add(teleport);
        return result;
    }

    private void RemapRequiredRooms(
        UnderworldVanillaDungeonReuseDefinition profile,
        DungeonGenerator generator)
    {
        if (!_donorRequiredRooms.TryGetValue(profile.DungeonId, out var donorRequired))
            donorRequired = generator.m_requiredRooms?.ToArray() ?? Array.Empty<string>();
        if (!_roomNames.TryGetValue(profile.DungeonId, out var roomNames))
        {
            // Location and DungeonDB callbacks are independent. Preserve donor names until the
            // room-clone callback arrives, then remap them atomically to private room identities.
            generator.m_requiredRooms = donorRequired.ToList();
            return;
        }

        var mapped = new List<string>();
        var missing = new List<string>();
        foreach (var required in donorRequired)
        {
            if (roomNames.TryGetValue(required, out var replacement))
                mapped.Add(replacement);
            else
                missing.Add(required);
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"{profile.DonorDisplayName} required room(s) did not resolve to private clones: " +
                string.Join(", ", missing));
        if (generator.m_minRequiredRooms > mapped.Count)
            throw new InvalidOperationException(
                $"{profile.DonorDisplayName} requires at least {generator.m_minRequiredRooms} required rooms " +
                $"but only {mapped.Count} donor required-room identities exist.");

        generator.m_requiredRooms = mapped;
    }

    private int CloneAndScaleDoors(
        DungeonGenerator generator,
        UnderworldVanillaDungeonReuseDefinition profile)
    {
        if (generator.m_doorTypes is null) return 0;
        var cloned = 0;

        for (var index = 0; index < generator.m_doorTypes.Count; index++)
        {
            var definition = generator.m_doorTypes[index];
            if (definition?.m_prefab is null) continue;

            var name =
                "Magenheim_Underworld_DungeonDoor_" +
                Safe(profile.Biome.ToString()) + "_" + index.ToString("00");
            if (PrefabManager.Instance.GetPrefab(name) is not null)
                throw new InvalidOperationException(
                    $"Occupied expanded dungeon door identity '{name}'.");

            var clone = PrefabManager.Instance.CreateClonedPrefab(name, definition.m_prefab)
                ?? throw new InvalidOperationException(
                    $"Unable to clone dungeon door '{definition.m_prefab.name}'.");
            clone.transform.localScale *= (float)profile.LinearRoomScale;
            UnderworldVanillaDungeonBiomeDressingPolicy.ApplyDoor(clone, profile, index);
            PrefabManager.Instance.AddPrefab(clone);
            definition.m_prefab = clone;
            _registeredDoorPrefabs.Add(name);
            cloned++;
        }
        return cloned;
    }

    private static void ScaleRoomHierarchy(
        GameObject clone,
        Room room,
        float scale,
        string donorRoomName)
    {
        if (Mathf.Approximately(scale, 1f)) return;
        if (scale < 1f)
            throw new InvalidOperationException(
                $"DDE room '{donorRoomName}' requested shrink scale {scale:0.###}.");

        // Root-attached render/collision geometry cannot be isolated from Room/connection metadata
        // without cloning component-specific data. Fail closed instead of making placement bounds
        // claim a size the visible shell does not actually have.
        if (clone.GetComponent<Renderer>() ||
            clone.GetComponent<Collider>() ||
            clone.GetComponent<MeshFilter>())
            throw new InvalidOperationException(
                $"Donor room '{donorRoomName}' carries render/collision geometry on the Room root. " +
                "DDE requires child-authored geometry for connection-safe scaling.");

        var connections = room.GetConnections();
        foreach (var connection in connections)
        {
            if (!connection || connection.transform.parent != clone.transform)
                throw new InvalidOperationException(
                    $"Donor room '{donorRoomName}' contains a RoomConnection below a nested parent. " +
                    "DungeonGenerator expects connections to remain direct Room children.");
        }

        var originalChildren = new List<Transform>();
        for (var index = 0; index < clone.transform.childCount; index++)
        {
            var child = clone.transform.GetChild(index);
            if (!child.GetComponent<RoomConnection>())
                originalChildren.Add(child);
        }

        var scaleRootObject = new GameObject("Magenheim_DDE_ScaleRoot");
        var scaleRoot = scaleRootObject.transform;
        scaleRoot.SetParent(clone.transform, false);
        scaleRoot.localPosition = Vector3.zero;
        scaleRoot.localRotation = Quaternion.identity;
        scaleRoot.localScale = Vector3.one * scale;

        // Reparent all ordinary room content under one uniform scale. Nested ZNetViews inherit the
        // enlarged source-space position used by DungeonGenerator.PlaceRoom, but when Valheim
        // instantiates those networked children separately their own local scale remains intact.
        foreach (var child in originalChildren)
            child.SetParent(scaleRoot, false);

        // Connections cannot live under the scale root: PlaceRoom reads their raw localPosition.
        // Move those raw positions outward by exactly the same factor so generator math and visible
        // openings agree.
        foreach (var connection in connections)
            connection.transform.localPosition *= scale;
    }

    private static PopulationRebindStats RebindPopulation(
        GameObject room,
        UnderworldVanillaDungeonReuseDefinition profile,
        string donorRoomName,
        int roomIndex)
    {
        var mechanics = UnderworldVanillaDungeonMechanicsPolicy.Apply(
            room, profile, donorRoomName);
        var ecology = UnderworldVanillaDungeonEcologyPolicy.Rebind(
            room, profile, donorRoomName, roomIndex);
        var rewards = UnderworldVanillaDungeonRewardPolicy.Rebind(
            room, profile, donorRoomName, roomIndex, ecology);
        var dressing = UnderworldVanillaDungeonBiomeDressingPolicy.Apply(
            room, profile, donorRoomName, roomIndex);
        return new PopulationRebindStats(mechanics, ecology, rewards, dressing);
    }

    private static UnderworldDungeonDefinition Definition(
        UnderworldVanillaDungeonReuseDefinition profile) =>
        UnderworldDungeonCatalog.All.Single(value =>
            string.Equals(value.Id, profile.DungeonId, StringComparison.Ordinal));

    internal static string ThemeName(UnderworldVanillaDungeonReuseDefinition profile) =>
        "MagenheimUnderworldVanilla" + Safe(profile.Biome.ToString());

    private static string RoomPrefabName(
        UnderworldVanillaDungeonReuseDefinition profile,
        string donorName,
        int index) =>
        "Magenheim_Underworld_VanillaRoom_" +
        Safe(profile.Biome.ToString()) + "_" + index.ToString("000") + "_" + Safe(donorName);

    private readonly record struct PopulationRebindStats(
        UnderworldVanillaDungeonMechanicsPolicy.Stats Mechanics,
        UnderworldVanillaDungeonEcologyPolicy.Stats Ecology,
        UnderworldVanillaDungeonRewardPolicy.Stats Rewards,
        UnderworldVanillaDungeonBiomeDressingPolicy.Stats Dressing)
    {
        internal static PopulationRebindStats Empty =>
            new(
                UnderworldVanillaDungeonMechanicsPolicy.Stats.Empty,
                UnderworldVanillaDungeonEcologyPolicy.Stats.Empty,
                UnderworldVanillaDungeonRewardPolicy.Stats.Empty,
                UnderworldVanillaDungeonBiomeDressingPolicy.Stats.Empty);

        public static PopulationRebindStats operator +(
            PopulationRebindStats left,
            PopulationRebindStats right) =>
            new(
                left.Mechanics + right.Mechanics,
                left.Ecology + right.Ecology,
                left.Rewards + right.Rewards,
                left.Dressing + right.Dressing);
    }

    private static string Safe(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray());

    public void Dispose()
    {
        if (_roomsSubscribed)
        {
            DungeonManager.OnVanillaRoomsAvailable -= RegisterRooms;
            _roomsSubscribed = false;
        }
        if (_locationsSubscribed)
        {
            ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
            _locationsSubscribed = false;
        }

        foreach (var room in _registeredRooms)
            DungeonManager.Instance.RemoveRoom(room);
        _registeredRooms.Clear();

        foreach (var location in _registeredLocations)
            ZoneManager.Instance.RemoveCustomLocation(location);
        _registeredLocations.Clear();

        foreach (var carrier in _themeCarriers)
            if (carrier) UnityEngine.Object.Destroy(carrier);
        _themeCarriers.Clear();

        // PrefabManager does not expose a symmetric removal API for registered network prefabs.
        // Door clones are lifetime-scoped to the plugin session and use unique Magenheim identities.
        _registeredDoorPrefabs.Clear();
        _roomNames.Clear();
        _generators.Clear();
        _donorRequiredRooms.Clear();
    }
}
