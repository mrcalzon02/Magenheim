using System;


namespace Magenheim.Core.DarkThrone;

/// <summary>
/// One authoritative arena geometry used by navigation, attacks, disengagement, and recovery.
/// Runtime adapters convert Unity vectors at the boundary instead of duplicating leash math.
/// </summary>
public sealed record DarkThroneArena(
    EncounterPosition Center,
    float HalfWidth,
    float HalfDepth,
    float RecoveryInset,
    float ParticipantMargin)
{
    public static DarkThroneArena CreateDefault(EncounterPosition center) =>
        new(center, HalfWidth: 26f, HalfDepth: 30f, RecoveryInset: 2f, ParticipantMargin: 8f);

    public void Validate()
    {
        if (!IsFinite(Center.X) || !IsFinite(Center.Y) || !IsFinite(Center.Z))
            throw new InvalidOperationException("Dark Throne arena center must be finite.");
        if (!IsFinite(HalfWidth) || HalfWidth <= 0f || !IsFinite(HalfDepth) || HalfDepth <= 0f)
            throw new InvalidOperationException("Dark Throne arena dimensions must be finite and positive.");
        if (!IsFinite(RecoveryInset) || RecoveryInset < 0f || RecoveryInset >= Math.Min(HalfWidth, HalfDepth))
            throw new InvalidOperationException("Dark Throne recovery inset must remain inside the arena.");
        if (!IsFinite(ParticipantMargin) || ParticipantMargin < 0f)
            throw new InvalidOperationException("Dark Throne participant margin must be finite and non-negative.");
    }

    public bool Contains(EncounterPosition position)
    {
        Validate();
        return Math.Abs(position.X - Center.X) <= HalfWidth
            && Math.Abs(position.Z - Center.Z) <= HalfDepth;
    }

    public bool IsEncounterParticipantPosition(EncounterPosition position)
    {
        Validate();
        return Math.Abs(position.X - Center.X) <= HalfWidth + ParticipantMargin
            && Math.Abs(position.Z - Center.Z) <= HalfDepth + ParticipantMargin;
    }

    public EncounterPosition ClampDestination(EncounterPosition requested)
    {
        Validate();
        var minX = Center.X - HalfWidth + RecoveryInset;
        var maxX = Center.X + HalfWidth - RecoveryInset;
        var minZ = Center.Z - HalfDepth + RecoveryInset;
        var maxZ = Center.Z + HalfDepth - RecoveryInset;
        return new EncounterPosition(
            Clamp(requested.X, minX, maxX),
            requested.Y,
            Clamp(requested.Z, minZ, maxZ));
    }

    public EncounterPosition RecoveryPoint(EncounterPosition invalidPosition)
    {
        var clamped = ClampDestination(invalidPosition);
        return new EncounterPosition(clamped.X, Center.Y, clamped.Z);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static float Clamp(float value, float minimum, float maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : value;
}
