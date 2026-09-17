using System;
using Magenheim.Core;

internal static class CrystalStaffDurabilityTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Crystal staff durability assertion {assertions} failed: {message}");
        }

        Assert(CrystalStaffDurability.ForTier(CrystalTier.Simple) == 150f, "Simple staves start the durability ladder at 150.");
        Assert(CrystalStaffDurability.ForTier(CrystalTier.Crystal) == 225f, "Crystal staves gain one tier step.");
        Assert(CrystalStaffDurability.ForTier(CrystalTier.Advanced) == 300f, "Advanced staves gain two tier steps.");
        Assert(CrystalStaffDurability.ForTier(CrystalTier.Master) == 375f, "Master staves gain three tier steps.");

        var ordered = new[] { CrystalTier.Simple, CrystalTier.Crystal, CrystalTier.Advanced, CrystalTier.Master };
        for (var index = 1; index < ordered.Length; index++)
            Assert(CrystalStaffDurability.ForTier(ordered[index]) > CrystalStaffDurability.ForTier(ordered[index - 1]),
                "Durability must increase strictly with staff tier.");

        var roughRejected = false;
        try { CrystalStaffDurability.ForTier(CrystalTier.Rough); }
        catch (ArgumentOutOfRangeException) { roughRejected = true; }
        Assert(roughRejected, "Rough is not a staff tier and must not resolve a durability value.");

        var unknownRejected = false;
        try { CrystalStaffDurability.ForTier((CrystalTier)99); }
        catch (ArgumentOutOfRangeException) { unknownRejected = true; }
        Assert(unknownRejected, "Undefined tiers fail closed rather than defaulting.");

        Assert(CrystalStaffDurability.TryResolveTier("Magenheim_Staff_Venom_Crystal", out var venom) && venom == CrystalTier.Crystal,
            "Canonical staff identities resolve their tier suffix.");
        Assert(CrystalStaffDurability.TryResolveTier("Magenheim_Staff_Earth_Master", out var earth) && earth == CrystalTier.Master,
            "Every family shares the same identity grammar.");
        Assert(!CrystalStaffDurability.TryResolveTier("Magenheim_Staff_Earth_Rough", out _),
            "Rough is refused even when spelled as a staff identity.");
        Assert(!CrystalStaffDurability.TryResolveTier("StaffIceShards", out _), "Vanilla staves are not Magenheim staves.");
        Assert(!CrystalStaffDurability.TryResolveTier("OtherMod_Staff_Venom_Crystal", out _), "Foreign identities fail closed.");
        Assert(!CrystalStaffDurability.TryResolveTier("Magenheim_Staff_Venom_Legendary", out _), "Unknown tier suffixes fail closed.");
        Assert(!CrystalStaffDurability.TryResolveTier("Magenheim_Staff_Venom_", out _), "A missing suffix fails closed.");
        Assert(!CrystalStaffDurability.TryResolveTier(null, out _), "Missing identities fail closed.");
        Assert(!CrystalStaffDurability.TryResolveTier("   ", out _), "Blank identities fail closed.");

        return assertions;
    }
}
