using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Definitions;

namespace Magenheim.Core.Worldgen;

public static class DefinitionWorldgenPlanner
{
    public static WorldgenAdditionPlan Build(
        MagenheimDefinitionSet definitions,
        IEnumerable<ObservedWorldgenRegistration> observed)
    {
        if (definitions is null)
            throw new ArgumentNullException(nameof(definitions));
        if (observed is null)
            throw new ArgumentNullException(nameof(observed));
        if (definitions.SchemaVersion != MagenheimDefinitionValidator.CurrentSchemaVersion)
            throw new InvalidOperationException(
                $"Worldgen planning requires definition schema {MagenheimDefinitionValidator.CurrentSchemaVersion}; received {definitions.SchemaVersion}.");
        if (definitions.Geodes is null)
            throw new InvalidOperationException("Worldgen planning requires a validated geode collection.");
        if (definitions.WorldgenCompatibility is null)
            throw new InvalidOperationException("Worldgen planning requires a validated compatibility policy.");

        var desired = definitions.Geodes
            .Select(geode => new DesiredWorldgenAddition(
                geode.Id,
                GeodeWorldPrefabIdentity.FromItemPrefabName(geode.PrefabName),
                geode.Area)
            {
                Biome = geode.Biome,
            })
            .ToArray();

        return WorldgenAdditionPlanner.Build(
            desired,
            observed,
            definitions.WorldgenCompatibility);
    }
}
