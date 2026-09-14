using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Socketing;
using Magenheim.Core.Transactions;

namespace Magenheim.Runtime;

internal sealed class RuntimeServices
{
    private RuntimeServices(
        MagenheimDefinitionSet definitions,
        SocketEligibilityPolicy socketPolicy,
        CrystalRefinementService refinement,
        RefinementTransactionPlanner refinementTransactions,
        RefinementOperationGuard refinementOperations,
        GeodeOpeningOperationGuard geodeOpeningOperations,
        SocketOperationGuard socketOperations,
        SocketExtractionOperationGuard socketExtractionOperations)
    {
        Definitions = definitions;
        SocketPolicy = socketPolicy;
        Refinement = refinement;
        RefinementTransactions = refinementTransactions;
        RefinementOperations = refinementOperations;
        GeodeOpeningOperations = geodeOpeningOperations;
        SocketOperations = socketOperations;
        SocketExtractionOperations = socketExtractionOperations;
    }

    internal MagenheimDefinitionSet Definitions { get; }
    internal SocketEligibilityPolicy SocketPolicy { get; }
    internal CrystalRefinementService Refinement { get; }
    internal RefinementTransactionPlanner RefinementTransactions { get; }
    internal RefinementOperationGuard RefinementOperations { get; }
    internal GeodeOpeningOperationGuard GeodeOpeningOperations { get; }
    internal SocketOperationGuard SocketOperations { get; }
    internal SocketExtractionOperationGuard SocketExtractionOperations { get; }

    internal static RuntimeServices Create(
        MagenheimDefinitionSet definitions,
        SocketEligibilityPolicy socketPolicy)
    {
        if (definitions is null) throw new System.ArgumentNullException(nameof(definitions));
        if (socketPolicy is null) throw new System.ArgumentNullException(nameof(socketPolicy));

        var refinement = new CrystalRefinementService(definitions.RefinementRules);
        var refinementTransactions = new RefinementTransactionPlanner(refinement);
        var refinementOperations = new RefinementOperationGuard(refinementTransactions);
        var geodeOpeningOperations = new GeodeOpeningOperationGuard();
        var socketOperations = new SocketOperationGuard();
        var socketExtractionOperations = new SocketExtractionOperationGuard();
        return new RuntimeServices(
            definitions,
            socketPolicy,
            refinement,
            refinementTransactions,
            refinementOperations,
            geodeOpeningOperations,
            socketOperations,
            socketExtractionOperations);
    }
}
