using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Socketing;
using Magenheim.Core.Worldgen;
using Magenheim.Core.Underworld;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Magenheim.Runtime.Definitions;

internal static class MagenheimDefinitionLoader
{
    internal static MagenheimDefinitionSet LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Definition path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("Magenheim definition file was not found.", path);

        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    internal static MagenheimDefinitionSet LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("Magenheim definition JSON cannot be empty.");

        try
        {
            var loadSettings = new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore,
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                LineInfoHandling = LineInfoHandling.Load,
            };

            var root = JObject.Parse(json, loadSettings);
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Include,
                ContractResolver = new UnderworldRequiredFieldsResolver(),
            });

            var document = root.ToObject<DefinitionFileDocument>(serializer)
                ?? throw new InvalidDataException("Magenheim definition document deserialized to null.");

            var compatibility = ToWorldgenCompatibility(document.WorldgenCompatibility);
            var refinementRules = document.RefinementRules.Select(ToRefinementRule).ToArray();
            var geodes = document.Geodes
                .Select(geode => ToGeodeDefinition(geode, compatibility.InvalidAreaBehavior))
                .ToArray();
            var socketEffects = new SocketEffectDefinitionSet(
                document.SocketEffects.Select(ToSocketEffectRule).ToArray());

            return MagenheimDefinitionValidator.ValidateAndFreeze(
                document.SchemaVersion,
                refinementRules,
                geodes,
                compatibility,
                socketEffects,
                document.Underworld is null ? null : UnderworldDefinitionValidator.ValidateAndFreeze(
                    document.Underworld.SchemaVersion, document.Underworld.Biomes,
                    document.Underworld.Bosses, document.Underworld.Deepstones),
                document.UnderworldArchitecture is null ? null : UnderworldArchitectureValidator.ValidateAndFreeze(
                    document.UnderworldArchitecture.SchemaVersion, document.UnderworldArchitecture.Pieces));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Invalid Magenheim definition JSON: {exception.Message}", exception);
        }
    }

    private static RefinementRule ToRefinementRule(RefinementRuleDocument document)
    {
        if (document is null)
            throw new InvalidDataException("Refinement definition cannot be null.");

        return new RefinementRule(
            ParseEnum<CrystalTier>(document.SourceTier, "sourceTier"),
            ParseEnum<CrystalTier>(document.DestinationTier, "destinationTier"),
            document.BaseFailureChance,
            document.MinimumSkillLevel,
            RequireText(document.RequiredStation, "requiredStation"),
            document.FailureShardCount);
    }

    private static GeodeDefinition ToGeodeDefinition(
        GeodeDefinitionDocument document,
        InvalidAreaBehavior invalidAreaBehavior)
    {
        if (document is null)
            throw new InvalidDataException("Geode definition cannot be null.");

        var area = SpawnAreaValidator.ParseConfiguredArea(document.Area, invalidAreaBehavior);
        if (!area.IsValid)
            throw new InvalidDataException($"Geode '{document.Id}' has invalid area: {area.Diagnostic}");

        var weights = document.Elements.Select(ToElementWeight).ToArray();
        return new GeodeDefinition(
            RequireText(document.Id, "id"),
            RequireText(document.Biome, "biome"),
            RequireText(document.PrefabName, "prefabName"),
            area.Area,
            document.GuaranteedCrystalCount,
            document.SecondCrystalChance,
            document.ThirdCrystalChance,
            Array.AsReadOnly(weights))
        {
            Placement = ToGeodePlacement(document.Placement),
        };
    }

    private static GeodePlacementDefinition ToGeodePlacement(GeodePlacementDocument document)
    {
        if (document is null)
            throw new InvalidDataException("Geode placement definition cannot be null.");

        return new GeodePlacementDefinition(
            document.BlockCheck,
            document.ForcePlacement,
            document.MinPerZone,
            document.MaxPerZone,
            document.MinAltitude,
            document.MaxAltitude,
            document.MinOceanDepth,
            document.MaxOceanDepth,
            document.MinTerrainDelta,
            document.MaxTerrainDelta,
            document.TerrainDeltaRadius,
            document.MinTilt,
            document.MaxTilt,
            document.InForest,
            document.ForestThresholdMin,
            document.ForestThresholdMax,
            document.ScaleMin,
            document.ScaleMax,
            document.GroupSizeMin,
            document.GroupSizeMax,
            document.GroupRadius,
            document.GroundOffset);
    }

    private static WorldgenCompatibilityPolicy ToWorldgenCompatibility(WorldgenCompatibilityDocument document)
    {
        if (document is null)
            throw new InvalidDataException("Worldgen compatibility definition cannot be null.");

        return new WorldgenCompatibilityPolicy(
            ParseEnum<InvalidAreaBehavior>(document.InvalidAreaBehavior, "worldgenCompatibility.invalidAreaBehavior"),
            ParseEnum<DuplicateRegistrationBehavior>(document.DuplicateRegistrationBehavior, "worldgenCompatibility.duplicateRegistrationBehavior"),
            AdditiveOnly: true)
        {
            DetectPrefabCollisions = document.DetectPrefabCollisions,
            IdentityComparison = ParseEnum<RegistrationIdentityComparison>(document.IdentityComparison, "worldgenCompatibility.identityComparison"),
            ExcludedRegistrationKeys = Array.AsReadOnly(document.ExcludedRegistrationKeys.Select(value => RequireText(value, "worldgenCompatibility.excludedRegistrationKeys")).ToArray()),
            ExcludedPrefabNames = Array.AsReadOnly(document.ExcludedPrefabNames.Select(value => RequireText(value, "worldgenCompatibility.excludedPrefabNames")).ToArray()),
        };
    }

    private static SocketEffectRule ToSocketEffectRule(SocketEffectRuleDocument document)
    {
        if (document is null)
            throw new InvalidDataException("Socket-effect definition cannot be null.");

        return new SocketEffectRule(
            ParseEnum<ElementalAlignment>(document.Element, "socketEffects.element"),
            ParseEnum<EquipmentCategory>(document.Category, "socketEffects.category"),
            ParseEnum<SocketEffectKind>(document.Effect, "socketEffects.effect"),
            document.SimpleMagnitude);
    }

    private static ElementWeight ToElementWeight(ElementWeightDocument document)
    {
        if (document is null)
            throw new InvalidDataException("Element weight definition cannot be null.");

        return new ElementWeight(
            ParseEnum<ElementalAlignment>(document.Element, "element"),
            document.Weight);
    }

    private static TEnum ParseEnum<TEnum>(string? value, string field)
        where TEnum : struct
    {
        if (value is null || string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value.Trim(), true, out TEnum parsed) || !Enum.IsDefined(typeof(TEnum), parsed))
            throw new InvalidDataException($"Field '{field}' contains unknown {typeof(TEnum).Name} value '{value}'.");
        return parsed;
    }

    private static string RequireText(string? value, string field)
    {
        if (value is null || string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"Field '{field}' is required and cannot be empty.");
        return value.Trim();
    }

    // Core stays JSON-library independent. Require all serialized component fields
    // here so missing enum/integer fields cannot silently acquire zero defaults.
    private sealed class UnderworldRequiredFieldsResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(
            System.Reflection.MemberInfo member, MemberSerialization serialization)
        {
            var property = base.CreateProperty(member, serialization);
            if (member.DeclaringType?.Namespace == typeof(UnderworldBiomeDefinition).Namespace)
                property.Required = Required.Always;
            return property;
        }
    }

    private sealed class UnderworldDocument
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; }
        [JsonProperty("biomes", Required = Required.Always)]
        public List<UnderworldBiomeDefinition> Biomes { get; set; } = null!;
        [JsonProperty("bosses", Required = Required.Always)]
        public List<UnderworldBossDefinition> Bosses { get; set; } = null!;
        [JsonProperty("deepstones", Required = Required.Always)]
        public List<UnderworldDeepstoneDefinition> Deepstones { get; set; } = null!;
    }

    private sealed class UnderworldArchitectureDocument
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; }
        [JsonProperty("pieces", Required = Required.Always)]
        public List<UnderworldBuildPieceDefinition> Pieces { get; set; } = null!;
    }

    private sealed class DefinitionFileDocument
    {
        [JsonProperty("underworld", Required = Required.AllowNull)]
        public UnderworldDocument? Underworld { get; set; }

        [JsonProperty("underworldArchitecture", Required = Required.AllowNull)]
        public UnderworldArchitectureDocument? UnderworldArchitecture { get; set; }

        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; }

        [JsonProperty("refinementRules", Required = Required.Always)]
        public List<RefinementRuleDocument> RefinementRules { get; set; } = null!;

        [JsonProperty("geodes", Required = Required.Always)]
        public List<GeodeDefinitionDocument> Geodes { get; set; } = null!;

        [JsonProperty("socketEffects", Required = Required.Always)]
        public List<SocketEffectRuleDocument> SocketEffects { get; set; } = null!;

        [JsonProperty("worldgenCompatibility", Required = Required.Always)]
        public WorldgenCompatibilityDocument WorldgenCompatibility { get; set; } = null!;
    }

    private sealed class RefinementRuleDocument
    {
        [JsonProperty("sourceTier", Required = Required.Always)]
        public string SourceTier { get; set; } = null!;

        [JsonProperty("destinationTier", Required = Required.Always)]
        public string DestinationTier { get; set; } = null!;

        [JsonProperty("baseFailureChance", Required = Required.Always)]
        public double BaseFailureChance { get; set; }

        [JsonProperty("minimumSkillLevel", Required = Required.Always)]
        public int MinimumSkillLevel { get; set; }

        [JsonProperty("requiredStation", Required = Required.Always)]
        public string RequiredStation { get; set; } = null!;

        [JsonProperty("failureShardCount", Required = Required.Always)]
        public int FailureShardCount { get; set; }
    }

    private sealed class GeodeDefinitionDocument
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = null!;

        [JsonProperty("biome", Required = Required.Always)]
        public string Biome { get; set; } = null!;

        [JsonProperty("prefabName", Required = Required.Always)]
        public string PrefabName { get; set; } = null!;

        [JsonProperty("area", Required = Required.Always)]
        public string Area { get; set; } = null!;

        [JsonProperty("guaranteedCrystalCount", Required = Required.Always)]
        public int GuaranteedCrystalCount { get; set; }

        [JsonProperty("secondCrystalChance", Required = Required.Always)]
        public double SecondCrystalChance { get; set; }

        [JsonProperty("thirdCrystalChance", Required = Required.Always)]
        public double ThirdCrystalChance { get; set; }

        [JsonProperty("elements", Required = Required.Always)]
        public List<ElementWeightDocument> Elements { get; set; } = null!;

        [JsonProperty("placement", Required = Required.Always)]
        public GeodePlacementDocument Placement { get; set; } = null!;
    }

    private sealed class GeodePlacementDocument
    {
        [JsonProperty("blockCheck", Required = Required.Always)]
        public bool BlockCheck { get; set; }

        [JsonProperty("forcePlacement", Required = Required.Always)]
        public bool ForcePlacement { get; set; }

        [JsonProperty("minPerZone", Required = Required.Always)]
        public double MinPerZone { get; set; }

        [JsonProperty("maxPerZone", Required = Required.Always)]
        public double MaxPerZone { get; set; }

        [JsonProperty("minAltitude", Required = Required.Always)]
        public double MinAltitude { get; set; }

        [JsonProperty("maxAltitude", Required = Required.Always)]
        public double MaxAltitude { get; set; }

        [JsonProperty("minOceanDepth", Required = Required.Always)]
        public double MinOceanDepth { get; set; }

        [JsonProperty("maxOceanDepth", Required = Required.Always)]
        public double MaxOceanDepth { get; set; }

        [JsonProperty("minTerrainDelta", Required = Required.Always)]
        public double MinTerrainDelta { get; set; }

        [JsonProperty("maxTerrainDelta", Required = Required.Always)]
        public double MaxTerrainDelta { get; set; }

        [JsonProperty("terrainDeltaRadius", Required = Required.Always)]
        public double TerrainDeltaRadius { get; set; }

        [JsonProperty("minTilt", Required = Required.Always)]
        public double MinTilt { get; set; }

        [JsonProperty("maxTilt", Required = Required.Always)]
        public double MaxTilt { get; set; }

        [JsonProperty("inForest", Required = Required.Always)]
        public bool InForest { get; set; }

        [JsonProperty("forestThresholdMin", Required = Required.Always)]
        public double ForestThresholdMin { get; set; }

        [JsonProperty("forestThresholdMax", Required = Required.Always)]
        public double ForestThresholdMax { get; set; }

        [JsonProperty("scaleMin", Required = Required.Always)]
        public double ScaleMin { get; set; }

        [JsonProperty("scaleMax", Required = Required.Always)]
        public double ScaleMax { get; set; }

        [JsonProperty("groupSizeMin", Required = Required.Always)]
        public int GroupSizeMin { get; set; }

        [JsonProperty("groupSizeMax", Required = Required.Always)]
        public int GroupSizeMax { get; set; }

        [JsonProperty("groupRadius", Required = Required.Always)]
        public double GroupRadius { get; set; }

        [JsonProperty("groundOffset", Required = Required.Always)]
        public double GroundOffset { get; set; }
    }

    private sealed class SocketEffectRuleDocument
    {
        [JsonProperty("element", Required = Required.Always)]
        public string Element { get; set; } = null!;

        [JsonProperty("category", Required = Required.Always)]
        public string Category { get; set; } = null!;

        [JsonProperty("effect", Required = Required.Always)]
        public string Effect { get; set; } = null!;

        [JsonProperty("simpleMagnitude", Required = Required.Always)]
        public double SimpleMagnitude { get; set; }
    }

    private sealed class WorldgenCompatibilityDocument
    {
        [JsonProperty("invalidAreaBehavior", Required = Required.Always)]
        public string InvalidAreaBehavior { get; set; } = null!;

        [JsonProperty("duplicateRegistrationBehavior", Required = Required.Always)]
        public string DuplicateRegistrationBehavior { get; set; } = null!;

        [JsonProperty("detectPrefabCollisions", Required = Required.Always)]
        public bool DetectPrefabCollisions { get; set; }

        [JsonProperty("identityComparison", Required = Required.Always)]
        public string IdentityComparison { get; set; } = null!;

        [JsonProperty("excludedRegistrationKeys", Required = Required.Always)]
        public List<string> ExcludedRegistrationKeys { get; set; } = null!;

        [JsonProperty("excludedPrefabNames", Required = Required.Always)]
        public List<string> ExcludedPrefabNames { get; set; } = null!;
    }

    private sealed class ElementWeightDocument
    {
        [JsonProperty("element", Required = Required.Always)]
        public string Element { get; set; } = null!;

        [JsonProperty("weight", Required = Required.Always)]
        public double Weight { get; set; }
    }
}
