using Magenheim.Core;
using Magenheim.Core.Definitions;

namespace Magenheim.Runtime;

internal sealed class RuntimeServices
{
    private RuntimeServices(
        MagenheimDefinitionSet definitions,
        CrystalRefinementService refinement)
    {
        Definitions = definitions;
        Refinement = refinement;
    }

    internal MagenheimDefinitionSet Definitions { get; }
    internal CrystalRefinementService Refinement { get; }

    internal static RuntimeServices Create(MagenheimDefinitionSet definitions)
    {
        var refinement = new CrystalRefinementService(definitions.RefinementRules);
        return new RuntimeServices(definitions, refinement);
    }
}
