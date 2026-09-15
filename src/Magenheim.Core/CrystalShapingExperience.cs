using System;

namespace Magenheim.Core;

/// <summary>
/// Crystal Shaping experience rewards. Runtime feeds these values into Valheim's native skill
/// progression rather than maintaining a parallel level/experience system. Successful higher-tier
/// shaping is deliberately worth more experience; failed refinement does not receive the success reward.
/// </summary>
public static class CrystalShapingExperience
{
    public const float CrackGeode = 1.00f;
    public const float BonusCrystalExtraction = 0.50f;
    public const float SocketInstall = 0.25f;
    public const float CrystalExtraction = 0.50f;

    public static float ForGeodeCracking(int crystalCount)
    {
        if (crystalCount < 1 || crystalCount > 3)
            throw new ArgumentOutOfRangeException(nameof(crystalCount), crystalCount, "Geode cracking must yield between one and three crystals.");

        return CrackGeode + ((crystalCount - 1) * BonusCrystalExtraction);
    }

    public static float ForRefinementAttempt(CrystalTier sourceTier)
    {
        return sourceTier switch
        {
            CrystalTier.Rough => 2.00f,
            CrystalTier.Simple => 4.00f,
            CrystalTier.Crystal => 7.00f,
            CrystalTier.Advanced => 12.00f,
            CrystalTier.Master => throw new InvalidOperationException("Master crystals cannot be refined and have no refinement experience weight."),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceTier), sourceTier, "Unknown crystal tier.")
        };
    }
}
