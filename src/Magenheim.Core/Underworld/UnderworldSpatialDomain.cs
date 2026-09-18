using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldSpatialDomainDefinition(
    int SchemaVersion,
    double RadiusMeters,
    double HostCenterX,
    double HostCenterZ,
    double HostBaseY,
    double LogicalMinY,
    double LogicalMaxY,
    string MappingAlgorithm,
    string Fingerprint);

/// <summary>A horizontal world column, in region-local coordinates.</summary>
public readonly struct UnderworldColumn
{
    public UnderworldColumn(double x, double z)
    {
        X = x;
        Z = z;
    }

    public double X { get; }

    public double Z { get; }
}

public sealed record UnderworldHostAnchor(
    string WorldId,
    double X,
    double Y,
    double Z,
    float HeadingDegrees);

/// <summary>
/// Deterministic spatial authority for hosting the logical Underworld inside the active parent
/// Valheim world, in the same save, as a reserved region far outside ordinary surface play. The
/// playable layer uses logical Underworld coordinates; runtime adapters map those coordinates into
/// the reserved host region immediately before placement or generation.
/// </summary>
/// <remarks>
/// The region is offset horizontally, not vertically. Valheim terrain is a 2D heightfield --
/// Heightmap.m_heights is a flat list and WorldGenerator.GetBiomeHeight returns one height per
/// column -- so surface ground and Underworld ground can never occupy the same (x, z). An earlier
/// schema offset only Y, which could host objects but could never host generated terrain, and that
/// dead end is what pushed the implementation into a separate save file.
///
/// Valheim solves this exact problem the same way: Ashlands and Deep North are landmasses placed
/// beyond the ordinary world (worldSize = 10000, waterEdge = 10500, deepNorthMinDistance = 12000
/// with deepNorthYOffset = 4000), streamed by the same ZoneSystem into the same save. The
/// Underworld is another such region.
/// </remarks>
public static class UnderworldSpatialDomain
{
    public const int CurrentSchemaVersion = 2;
    public const string CurrentMappingAlgorithm = "seed32-quarter-turn-offset-v2";

    /// <summary>The reserved region's nearest edge must clear the vanilla world and its far landmasses.</summary>
    public const double MinimumHostClearanceMeters = 20000d;

    private const double DefaultRadiusMeters = 8000d;
    // Offset along +X so the region cannot collide with Deep North (+Z) or Ashlands (-Z).
    private const double DefaultHostCenterX = 40000d;
    private const double DefaultHostCenterZ = 0d;
    // Now that the region owns its own heightfield columns, Underworld terrain generates at ordinary
    // Valheim altitudes, so water level, swimming and every height-driven system behave normally.
    private const double DefaultHostBaseY = 0d;
    private const double DefaultLogicalMinY = -256d;
    private const double DefaultLogicalMaxY = 1792d;

    public static UnderworldSpatialDomainDefinition CreateDefault() =>
        ValidateAndFreeze(
            CurrentSchemaVersion,
            DefaultRadiusMeters,
            DefaultHostCenterX,
            DefaultHostCenterZ,
            DefaultHostBaseY,
            DefaultLogicalMinY,
            DefaultLogicalMaxY,
            CurrentMappingAlgorithm);

    public static UnderworldSpatialDomainDefinition ValidateAndFreeze(
        int schemaVersion,
        double radiusMeters,
        double hostCenterX,
        double hostCenterZ,
        double hostBaseY,
        double logicalMinY,
        double logicalMaxY,
        string mappingAlgorithm)
    {
        ValidateFields(schemaVersion, radiusMeters, hostCenterX, hostCenterZ, hostBaseY, logicalMinY, logicalMaxY, mappingAlgorithm);
        var algorithm = mappingAlgorithm.Trim();
        return new UnderworldSpatialDomainDefinition(
            schemaVersion,
            radiusMeters,
            hostCenterX,
            hostCenterZ,
            hostBaseY,
            logicalMinY,
            logicalMaxY,
            algorithm,
            ComputeFingerprint(
                schemaVersion,
                radiusMeters,
                hostCenterX,
                hostCenterZ,
                hostBaseY,
                logicalMinY,
                logicalMaxY,
                algorithm));
    }

    public static void ValidateDefinition(UnderworldSpatialDomainDefinition domain)
    {
        if (domain is null) throw new ArgumentNullException(nameof(domain));
        ValidateFields(
            domain.SchemaVersion,
            domain.RadiusMeters,
            domain.HostCenterX,
            domain.HostCenterZ,
            domain.HostBaseY,
            domain.LogicalMinY,
            domain.LogicalMaxY,
            domain.MappingAlgorithm);
        var expected = ComputeFingerprint(
            domain.SchemaVersion,
            domain.RadiusMeters,
            domain.HostCenterX,
            domain.HostCenterZ,
            domain.HostBaseY,
            domain.LogicalMinY,
            domain.LogicalMaxY,
            domain.MappingAlgorithm.Trim());
        if (!string.Equals(expected, domain.Fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld spatial-domain fingerprint does not match its fields.");
    }

    public static UnderworldHostAnchor ToHostAnchor(
        UnderworldSpatialDomainDefinition domain,
        UnderworldWorldIdentity identity,
        UnderworldLayer layer,
        UnderworldAnchor anchor)
    {
        ValidateDefinition(domain);
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        ValidateFiniteAnchor(anchor);

        if (layer == UnderworldLayer.Surface)
        {
            if (!string.Equals(anchor.WorldId, identity.ParentWorldId, StringComparison.Ordinal))
                throw new InvalidOperationException("Surface anchor does not belong to the parent Valheim world.");
            return new UnderworldHostAnchor(
                identity.ParentWorldId,
                anchor.X,
                anchor.Y,
                anchor.Z,
                NormalizeHeading(anchor.HeadingDegrees));
        }

        if (!string.Equals(anchor.WorldId, identity.DerivedWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld anchor does not belong to the derived logical world.");
        ValidateLogicalUnderworldBounds(domain, anchor.X, anchor.Y, anchor.Z);

        var quarterTurns = QuarterTurns(identity.DerivedSeed32);
        var rotated = Rotate(anchor.X, anchor.Z, quarterTurns);
        return new UnderworldHostAnchor(
            identity.ParentWorldId,
            domain.HostCenterX + rotated.X,
            domain.HostBaseY + anchor.Y,
            domain.HostCenterZ + rotated.Z,
            NormalizeHeading(anchor.HeadingDegrees + quarterTurns * 90f));
    }

    public static UnderworldAnchor ToLogicalUnderworldAnchor(
        UnderworldSpatialDomainDefinition domain,
        UnderworldWorldIdentity identity,
        UnderworldHostAnchor hostAnchor)
    {
        ValidateDefinition(domain);
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (hostAnchor is null) throw new ArgumentNullException(nameof(hostAnchor));
        ValidateFiniteHostAnchor(hostAnchor);
        if (!string.Equals(hostAnchor.WorldId, identity.ParentWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld host anchor does not belong to the parent Valheim world.");

        var localY = hostAnchor.Y - domain.HostBaseY;
        var quarterTurns = QuarterTurns(identity.DerivedSeed32);
        var local = Rotate(
            hostAnchor.X - domain.HostCenterX,
            hostAnchor.Z - domain.HostCenterZ,
            (4 - quarterTurns) & 3);
        ValidateLogicalUnderworldBounds(domain, local.X, localY, local.Z);

        return new UnderworldAnchor(
            identity.DerivedWorldId,
            local.X,
            localY,
            local.Z,
            NormalizeHeading(hostAnchor.HeadingDegrees - quarterTurns * 90f));
    }

    public static bool ContainsHostPoint(
        UnderworldSpatialDomainDefinition domain,
        double x,
        double y,
        double z)
    {
        ValidateDefinition(domain);
        if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z)) return false;
        var radiusSquared = domain.RadiusMeters * domain.RadiusMeters;
        var dx = x - domain.HostCenterX;
        var dz = z - domain.HostCenterZ;
        var horizontalSquared = dx * dx + dz * dz;
        return horizontalSquared <= radiusSquared &&
               y >= domain.HostBaseY + domain.LogicalMinY &&
               y <= domain.HostBaseY + domain.LogicalMaxY;
    }

    /// <summary>
    /// Whether a world (x, z) column falls inside the reserved Underworld region. Terrain generation
    /// runs per column with no vertical context, so this is the test the generation hooks use to
    /// decide whether a column belongs to the Underworld or to untouched surface world.
    /// </summary>
    public static bool ContainsHostColumn(UnderworldSpatialDomainDefinition domain, double x, double z)
    {
        ValidateDefinition(domain);
        if (!IsFinite(x) || !IsFinite(z)) return false;
        var dx = x - domain.HostCenterX;
        var dz = z - domain.HostCenterZ;
        return dx * dx + dz * dz <= domain.RadiusMeters * domain.RadiusMeters;
    }

    /// <summary>Converts a world column into the region-local coordinates the terrain rules expect.</summary>
    /// <remarks>
    /// Returns a named struct rather than a value tuple on purpose: Magenheim.Runtime targets net462,
    /// where System.ValueTuple is a facade assembly that Valheim and BepInEx do not ship, so a tuple
    /// crossing this boundary fails to load at runtime.
    /// </remarks>
    public static UnderworldColumn ToLogicalColumn(UnderworldSpatialDomainDefinition domain, double x, double z)
    {
        ValidateDefinition(domain);
        return new UnderworldColumn(x - domain.HostCenterX, z - domain.HostCenterZ);
    }

    private static void ValidateFields(
        int schemaVersion,
        double radiusMeters,
        double hostCenterX,
        double hostCenterZ,
        double hostBaseY,
        double logicalMinY,
        double logicalMaxY,
        string mappingAlgorithm)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new InvalidOperationException(
                $"Unsupported Underworld spatial-domain schema {schemaVersion}. Expected {CurrentSchemaVersion}.");
        if (!IsFinite(radiusMeters) || radiusMeters <= 0d)
            throw new InvalidOperationException("Underworld spatial radius must be a positive finite value.");
        if (!IsFinite(hostCenterX) || !IsFinite(hostCenterZ) ||
            !IsFinite(hostBaseY) || !IsFinite(logicalMinY) || !IsFinite(logicalMaxY))
            throw new InvalidOperationException("Underworld spatial-domain coordinates must be finite.");
        var centreDistance = Math.Sqrt(hostCenterX * hostCenterX + hostCenterZ * hostCenterZ);
        if (centreDistance - radiusMeters < MinimumHostClearanceMeters)
            throw new InvalidOperationException(
                "The reserved Underworld region must clear ordinary surface play and Valheim's own far landmasses by at least " +
                MinimumHostClearanceMeters.ToString(CultureInfo.InvariantCulture) + "m.");
        if (logicalMaxY <= logicalMinY)
            throw new InvalidOperationException("Underworld logical vertical maximum must exceed the minimum.");

        var algorithm = RequireText(mappingAlgorithm, nameof(mappingAlgorithm));
        if (!string.Equals(algorithm, CurrentMappingAlgorithm, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported Underworld spatial mapping algorithm '{algorithm}'.");

        var hostMinY = hostBaseY + logicalMinY;
        var hostMaxY = hostBaseY + logicalMaxY;
        if (!IsFinite(hostMinY) || !IsFinite(hostMaxY) || hostMaxY <= hostMinY)
            throw new InvalidOperationException("Underworld host vertical band is invalid.");
    }

    private static void ValidateLogicalUnderworldBounds(
        UnderworldSpatialDomainDefinition domain,
        double x,
        double y,
        double z)
    {
        var radiusSquared = domain.RadiusMeters * domain.RadiusMeters;
        if (x * x + z * z > radiusSquared)
            throw new InvalidOperationException("Underworld logical anchor lies outside the reserved playable radius.");
        if (y < domain.LogicalMinY || y > domain.LogicalMaxY)
            throw new InvalidOperationException("Underworld logical anchor lies outside the reserved vertical domain.");
    }

    private static (double X, double Z) Rotate(double x, double z, int quarterTurns) =>
        quarterTurns switch
        {
            0 => (x, z),
            1 => (-z, x),
            2 => (-x, -z),
            3 => (z, -x),
            _ => throw new InvalidOperationException("Quarter-turn rotation must be normalized to 0..3."),
        };

    private static int QuarterTurns(int derivedSeed32) => (int)((uint)derivedSeed32 & 3u);

    private static void ValidateFiniteAnchor(UnderworldAnchor anchor)
    {
        RequireText(anchor.WorldId, nameof(anchor.WorldId));
        if (!IsFinite(anchor.X) || !IsFinite(anchor.Y) || !IsFinite(anchor.Z) ||
            float.IsNaN(anchor.HeadingDegrees) || float.IsInfinity(anchor.HeadingDegrees))
            throw new InvalidOperationException("Underworld logical anchor contains non-finite values.");
    }

    private static void ValidateFiniteHostAnchor(UnderworldHostAnchor anchor)
    {
        RequireText(anchor.WorldId, nameof(anchor.WorldId));
        if (!IsFinite(anchor.X) || !IsFinite(anchor.Y) || !IsFinite(anchor.Z) ||
            float.IsNaN(anchor.HeadingDegrees) || float.IsInfinity(anchor.HeadingDegrees))
            throw new InvalidOperationException("Underworld host anchor contains non-finite values.");
    }

    private static float NormalizeHeading(float heading)
    {
        var normalized = heading % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static string RequireText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A non-empty value is required.", field);
        return value.Trim();
    }

    private static string ComputeFingerprint(
        int schemaVersion,
        double radiusMeters,
        double hostCenterX,
        double hostCenterZ,
        double hostBaseY,
        double logicalMinY,
        double logicalMaxY,
        string mappingAlgorithm)
    {
        var payload = new StringBuilder()
            .Append("underworld-spatial-schema=").Append(schemaVersion).Append('\n')
            .Append("mapping=").Append(mappingAlgorithm).Append('\n')
            .Append("radius=").Append(radiusMeters.ToString("R", CultureInfo.InvariantCulture)).Append('\n')
            .Append("host-center-x=").Append(hostCenterX.ToString("R", CultureInfo.InvariantCulture)).Append('\n')
            .Append("host-center-z=").Append(hostCenterZ.ToString("R", CultureInfo.InvariantCulture)).Append('\n')
            .Append("host-base-y=").Append(hostBaseY.ToString("R", CultureInfo.InvariantCulture)).Append('\n')
            .Append("logical-min-y=").Append(logicalMinY.ToString("R", CultureInfo.InvariantCulture)).Append('\n')
            .Append("logical-max-y=").Append(logicalMaxY.ToString("R", CultureInfo.InvariantCulture)).Append('\n')
            .ToString();

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var result = new StringBuilder(hash.Length * 2);
        foreach (var value in hash)
            result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return result.ToString();
    }
}
