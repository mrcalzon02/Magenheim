using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Transactions;

namespace Magenheim.Runtime;

internal sealed class RuntimeServices
{
    private RuntimeServices(
        MagenheimDefinitionSet definitions,
        CrystalRefinementService refinement,
        RefinementTransactionPlanner refinementTransactions,
        RefinementOperationGuard refinementOperations,
        GeodeOpeningOperationGuard geodeOpeningOperations)
    {
        Definitions = definitions;
        Refinement = refinement;
        RefinementTransactions = refinementTransactions;
        RefinementOperations = refinementOperations;
        GeodeOpeningOperations = geodeOpeningOperations;
    }

    internal MagenheimDefinitionSet Definitions { get; }
    internal CrystalRefinementService Refinement { get; }
    internal RefinementTransactionPlanner RefinementTransactions { get; }
    internal RefinementOperationGuard RefinementOperations { get; }
    internal GeodeOpeningOperationGuard GeodeOpeningOperations { get; }

    internal static RuntimeServices Create(MagenheimDefinitionSet definitions)
    {
        var refinement = new CrystalRefinementService(definitions.RefinementRules);
        var refinementTransactions = new RefinementTransactionPlanner(refinement);
        var refinementOperations = new RefinementOperationGuard(refinementTransactions);
        var geodeOpeningOperations = new GeodeOpeningOperationGuard();
        return new RuntimeServices(
            definitions,
            refinement,
            refinementTransactions,
            refinementOperations,
            geodeOpeningOperations);
    }
}
