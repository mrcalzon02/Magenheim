using Magenheim.Core;

namespace Magenheim.Runtime;

internal sealed class RuntimeServices
{
    private RuntimeServices(CrystalRefinementService refinement)
    {
        Refinement = refinement;
    }

    internal CrystalRefinementService Refinement { get; }

    internal static RuntimeServices Create()
    {
        var refinement = new CrystalRefinementService(CrystalRefinementService.CreateCanonicalDefaults());
        return new RuntimeServices(refinement);
    }
}
