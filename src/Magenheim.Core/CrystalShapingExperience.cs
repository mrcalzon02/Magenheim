using System;

namespace Magenheim.Core;

/// <summary>
/// Initial Crystal Shaping experience weights recovered from the reconciled vertical-slice
/// design. Runtime code feeds these values into Valheim's native skill progression rather
/// than maintaining a parallel level/experience system.
/// </summary>
public static class CrystalShapingExperience
{
    public const float CrackGeode = 0.15f;
    public const float SocketInstall = 0.25f;
    public const float CrystalExtraction = 0.50f;

    public static float ForRefinementAttempt(CrystalTier sourceTier)
    {
        return sourceTier switch
        {
            CrystalTier.Rough => 0.50f,
            CrystalTier.Simple => 1.00f,
            CrystalTier.Crystal => 2.00f,
            CrystalTier.Advanced => 4.00f,
            CrystalTier.Master => throw new InvalidOperationException("Master crystals cannot be refined and have no refinement experience weight."),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceTier), sourceTier, "Unknown crystal tier.")
        };
    }
}
