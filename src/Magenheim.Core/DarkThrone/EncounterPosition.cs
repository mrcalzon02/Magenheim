namespace Magenheim.Core.DarkThrone;

/// <summary>World coordinates at the pure encounter/runtime boundary.</summary>
public readonly record struct EncounterPosition(float X, float Y, float Z)
{
    public static EncounterPosition Zero => new(0f, 0f, 0f);
}
