using System;
using Magenheim.Core.Underworld;

internal static class UnderworldSpatialDomainTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException(
                    $"Underworld spatial-domain assertion {assertions} failed: {message}");
        }

        var domain = UnderworldSpatialDomain.CreateDefault();
        Assert(domain.SchemaVersion == UnderworldSpatialDomain.CurrentSchemaVersion,
            "Default spatial domain must expose the current schema.");
        Assert(domain.Fingerprint.Length == 64,
            "Spatial-domain fingerprint must be SHA-256 hex.");
        Assert(domain.HostBaseY + domain.LogicalMinY > 7000d,
            "Default Underworld host band must remain far above ordinary surface terrain.");

        var identity = UnderworldWorldIdentityFactory.Derive("world-spatial", "seed-spatial");
        var logical = new UnderworldAnchor(identity.DerivedWorldId, 120d, 64d, -340d, 35f);
        var host = UnderworldSpatialDomain.ToHostAnchor(domain, identity, UnderworldLayer.Underworld, logical);

        Assert(host.WorldId == identity.ParentWorldId,
            "Physical host anchors must belong to the active parent Valheim world.");
        Assert(Math.Abs(host.Y - (domain.HostBaseY + logical.Y)) < 0.000001d,
            "Underworld logical Y must map into the reserved host vertical band.");
        Assert(UnderworldSpatialDomain.ContainsHostPoint(domain, host.X, host.Y, host.Z),
            "Mapped Underworld host anchor must lie inside the reserved domain.");

        var roundTrip = UnderworldSpatialDomain.ToLogicalUnderworldAnchor(domain, identity, host);
        Assert(roundTrip.WorldId == logical.WorldId,
            "Inverse mapping must restore the logical derived-world identity.");
        Assert(Math.Abs(roundTrip.X - logical.X) < 0.000001d &&
               Math.Abs(roundTrip.Y - logical.Y) < 0.000001d &&
               Math.Abs(roundTrip.Z - logical.Z) < 0.000001d,
            "Underworld spatial mapping must round-trip coordinates exactly within floating-point tolerance.");
        Assert(Math.Abs(NormalizedDelta(roundTrip.HeadingDegrees, logical.HeadingDegrees)) < 0.001f,
            "Underworld spatial mapping must round-trip heading.");

        var repeat = UnderworldSpatialDomain.ToHostAnchor(domain, identity, UnderworldLayer.Underworld, logical);
        Assert(repeat == host,
            "Identical identity and logical anchor must map deterministically.");

        var otherIdentity = FindDifferentQuarterTurn(identity, "world-spatial", "seed-spatial-other-");
        var otherHost = UnderworldSpatialDomain.ToHostAnchor(domain, otherIdentity, UnderworldLayer.Underworld,
            logical with { WorldId = otherIdentity.DerivedWorldId });
        Assert(host.X != otherHost.X || host.Z != otherHost.Z || host.HeadingDegrees != otherHost.HeadingDegrees,
            "DerivedSeed32 must influence the deterministic Underworld host orientation.");

        var surface = new UnderworldAnchor(identity.ParentWorldId, 15d, 42d, -9d, 370f);
        var surfaceHost = UnderworldSpatialDomain.ToHostAnchor(domain, identity, UnderworldLayer.Surface, surface);
        Assert(surfaceHost.X == surface.X && surfaceHost.Y == surface.Y && surfaceHost.Z == surface.Z,
            "Surface mapping must not translate player coordinates.");
        Assert(Math.Abs(surfaceHost.HeadingDegrees - 10f) < 0.001f,
            "Surface mapping may normalize heading without otherwise changing placement.");

        AssertThrows(Assert,
            () => UnderworldSpatialDomain.ToHostAnchor(domain, identity, UnderworldLayer.Underworld,
                logical with { X = domain.RadiusMeters + 1d, Z = 0d }),
            "Logical positions beyond the reserved radius must fail closed.");
        AssertThrows(Assert,
            () => UnderworldSpatialDomain.ToHostAnchor(domain, identity, UnderworldLayer.Underworld,
                logical with { Y = domain.LogicalMaxY + 1d }),
            "Logical positions above the reserved vertical range must fail closed.");
        AssertThrows(Assert,
            () => UnderworldSpatialDomain.ToHostAnchor(domain, identity, UnderworldLayer.Underworld,
                logical with { WorldId = identity.ParentWorldId }),
            "Underworld logical anchors with the parent-world identity must be rejected.");

        var changedDomain = UnderworldSpatialDomain.ValidateAndFreeze(
            UnderworldSpatialDomain.CurrentSchemaVersion,
            domain.RadiusMeters - 1d,
            domain.HostBaseY,
            domain.LogicalMinY,
            domain.LogicalMaxY,
            domain.MappingAlgorithm);
        Assert(changedDomain.Fingerprint != domain.Fingerprint,
            "Gameplay-significant spatial-domain changes must alter the domain fingerprint.");

        var forged = domain with { HostBaseY = domain.HostBaseY + 1d };
        AssertThrows(Assert,
            () => UnderworldSpatialDomain.ToHostAnchor(forged, identity, UnderworldLayer.Underworld, logical),
            "Spatial definitions whose fields no longer match their fingerprint must fail closed.");

        return assertions;
    }

    private static UnderworldWorldIdentity FindDifferentQuarterTurn(
        UnderworldWorldIdentity baseline,
        string worldId,
        string seedPrefix)
    {
        var baselineQuarter = (int)((uint)baseline.DerivedSeed32 & 3u);
        for (var index = 0; index < 32; index++)
        {
            var candidate = UnderworldWorldIdentityFactory.Derive(worldId, seedPrefix + index);
            if ((int)((uint)candidate.DerivedSeed32 & 3u) != baselineQuarter)
                return candidate;
        }
        throw new InvalidOperationException("Test could not derive a different spatial quarter-turn within bounded attempts.");
    }

    private static float NormalizedDelta(float left, float right)
    {
        var delta = (left - right) % 360f;
        if (delta > 180f) delta -= 360f;
        if (delta < -180f) delta += 360f;
        return delta;
    }

    private static void AssertThrows(Action<bool, string> assert, Action action, string message)
    {
        var threw = false;
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            threw = true;
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }
        assert(threw, message);
    }
}
