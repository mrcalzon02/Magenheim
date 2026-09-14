using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Socketing;
using Magenheim.Core.Transactions;

namespace Magenheim.Runtime;

internal sealed class RuntimeServices
{
    private RuntimeServices(
        MagenheimDefinitionSet definitions,
        CrystalRefinementService refinement,
        RefinementTransactionPlanner refinementTransactions,
        RefinementOperationGuard refinementOperations,
        GeodeOpeningOperationGuard geodeOpeningOperations,
        SocketOperationGuard socketOperations,
        SocketExtractionOperationGuard socketExtractionOperations)
    {
        Definitions = definitions;
        Refinement = refinement;
        RefinementTransactions = refinementTransactions;
        RefinementOperations = refinementOperations;
        GeodeOpeningOperations = geodeOpeningOperations;
        SocketOperations = socketOperations;
        SocketExtractionOperations = socketExtractionOperations;
    }

    internal MagenheimDefinitionSet Definitions { get; }
    internal CrystalRefinementService Refinement { get; }
    internal RefinementTransactionPlanner RefinementTransactions { get; }
    internal RefinementOperationGuard RefinementOperations { get; }
    internal GeodeOpeningOperationGuard GeodeOpeningOperations { get; }
    internal SocketOperationGuard SocketOperations { get; }
    internal SocketExtractionOperationGuard SocketExtractionOperations { get; }

    internal static RuntimeServices Create(MagenheimDefinitionSet definitions)
    {
        var refinement = new CrystalRefinementService(definitions.RefinementRules);
        var refinementTransactions = new RefinementTransactionPlanner(refinement);
        var refinementOperations = new RefinementOperationGuard(refinementTransactions);
        var geodeOpeningOperations = new GeodeOpeningOperationGuard();
        var socketOperations = new SocketOperationGuard();
        var socketExtractionOperations = new SocketExtractionOperationGuard();
        return new RuntimeServices(
            definitions,
            refinement,
            refinementTransactions,
            refinementOperations,
            geodeOpeningOperations,
            socketOperations,
            socketExtractionOperations);
    }
}
