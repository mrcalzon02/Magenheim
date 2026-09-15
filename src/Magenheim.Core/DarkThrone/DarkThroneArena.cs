using System;
using System.Numerics;

namespace Magenheim.Core.DarkThrone;

/// <summary>
/// One authoritative arena geometry used by navigation, attacks, disengagement, and recovery.
/// Runtime adapters convert Unity vectors at the boundary instead of duplicating leash math.
/// </summary>
public sealed record DarkThroneArena(
    Vector3 Center,
    float HalfWidth,
    float HalfDepth,
    float RecoveryInset,
    float ParticipantMargin)
{
    public static DarkThroneArena CreateDefault(Vector3 center) =>
        new(center, HalfWidth: 26f, HalfDepth: 30f, RecoveryInset: 2f, ParticipantMargin: 8f);

    public void Validate()
    {
        if (!float.IsFinite(Center.X) || !float.IsFinite(Center.Y) || !float.IsFinite(Center.Z))
            throw new InvalidOperationException("Dark Throne arena center must be finite.");
        if (!float.IsFinite(HalfWidth) || HalfWidth <= 0f || !float.IsFinite(HalfDepth) || HalfDepth <= 0f)
            throw new InvalidOperationException("Dark Throne arena dimensions must be finite and positive.");
        if (!float.IsFinite(RecoveryInset) || RecoveryInset < 0f || RecoveryInset >= MathF.Min(HalfWidth, HalfDepth))
            throw new InvalidOperationException("Dark Throne recovery inset must remain inside the arena.");
        if (!float.IsFinite(ParticipantMargin) || ParticipantMargin < 0f)
            throw new InvalidOperationException("Dark Throne participant margin must be finite and non-negative.");
    }

    public bool Contains(Vector3 position)
    {
        Validate();
        return MathF.Abs(position.X - Center.X) <= HalfWidth
            && MathF.Abs(position.Z - Center.Z) <= HalfDepth;
    }

    public bool IsEncounterParticipantPosition(Vector3 position)
    {
        Validate();
        return MathF.Abs(position.X - Center.X) <= HalfWidth + ParticipantMargin
            && MathF.Abs(position.Z - Center.Z) <= HalfDepth + ParticipantMargin;
    }

    public Vector3 ClampDestination(Vector3 requested)
    {
        Validate();
        var minX = Center.X - HalfWidth + RecoveryInset;
        var maxX = Center.X + HalfWidth - RecoveryInset;
        var minZ = Center.Z - HalfDepth + RecoveryInset;
        var maxZ = Center.Z + HalfDepth - RecoveryInset;
        return new Vector3(
            Math.Clamp(requested.X, minX, maxX),
            requested.Y,
            Math.Clamp(requested.Z, minZ, maxZ));
    }

    public Vector3 RecoveryPoint(Vector3 invalidPosition)
    {
        var clamped = ClampDestination(invalidPosition);
        return new Vector3(clamped.X, Center.Y, clamped.Z);
    }
}
