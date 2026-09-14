using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Transactions;

namespace Magenheim.Runtime;

internal sealed class RuntimeServices
{
    private RuntimeServices(
        MagenheimDefinitionSet definitions,
        CrystalRefinementService refinement,
        GeodeOpeningOperationGuard geodeOpeningOperations)
    {
        Definitions = definitions;
        Refinement = refinement;
        GeodeOpeningOperations = geodeOpeningOperations;
    }

    internal MagenheimDefinitionSet Definitions { get; }
    internal CrystalRefinementService Refinement { get; }
    internal GeodeOpeningOperationGuard GeodeOpeningOperations { get; }

    internal static RuntimeServices Create(MagenheimDefinitionSet definitions)
    {
        var refinement = new CrystalRefinementService(definitions.RefinementRules);
        var geodeOpeningOperations = new GeodeOpeningOperationGuard();
        return new RuntimeServices(definitions, refinement, geodeOpeningOperations);
    }
}
