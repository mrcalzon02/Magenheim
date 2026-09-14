using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Worldgen;
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
            });

            var document = root.ToObject<DefinitionFileDocument>(serializer)
                ?? throw new InvalidDataException("Magenheim definition document deserialized to null.");

            var refinementRules = document.RefinementRules.Select(ToRefinementRule).ToArray();
            var geodes = document.Geodes.Select(ToGeodeDefinition).ToArray();

            return MagenheimDefinitionValidator.ValidateAndFreeze(
                document.SchemaVersion,
                refinementRules,
                geodes);
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

    private static GeodeDefinition ToGeodeDefinition(GeodeDefinitionDocument document)
    {
        if (document is null)
            throw new InvalidDataException("Geode definition cannot be null.");

        var area = SpawnAreaValidator.ParseConfiguredArea(document.Area, InvalidAreaBehavior.Reject);
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
            Array.AsReadOnly(weights));
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
        if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value.Trim(), true, out TEnum parsed) || !Enum.IsDefined(typeof(TEnum), parsed))
            throw new InvalidDataException($"Field '{field}' contains unknown {typeof(TEnum).Name} value '{value}'.");
        return parsed;
    }

    private static string RequireText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"Field '{field}' is required and cannot be empty.");
        return value.Trim();
    }

    private sealed class DefinitionFileDocument
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; }

        [JsonProperty("refinementRules", Required = Required.Always)]
        public List<RefinementRuleDocument> RefinementRules { get; set; } = null!;

        [JsonProperty("geodes", Required = Required.Always)]
        public List<GeodeDefinitionDocument> Geodes { get; set; } = null!;
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
    }

    private sealed class ElementWeightDocument
    {
        [JsonProperty("element", Required = Required.Always)]
        public string Element { get; set; } = null!;

        [JsonProperty("weight", Required = Required.Always)]
        public double Weight { get; set; }
    }
}
