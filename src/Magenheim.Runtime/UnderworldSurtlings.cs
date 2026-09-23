namespace Magenheim.Runtime;

/// <summary>
/// The elemental Surtling family (docs/UNDERWORLD_ELEMENTAL_SURTLINGS_DESIGN.md): six elemental kits
/// on two shared bodies, twelve console-spawnable creatures bound to humanoid donor skeletons.
/// </summary>
/// <remarks>
/// One roster, one registrar, one binder: the design forbids a registrar, AI or persistence system per
/// element. Umbral is a Surtling phenotype only; it is deliberately not an ElementalAlignment, and
/// Radiance reuses the existing Radiance identity by name.
/// </remarks>
internal static class UnderworldSurtlings
{
    internal enum Body { Feminine, Masculine }

    /// <param name="Element">Display element; Radiance matches the existing alignment's name.</param>
    /// <param name="Donor">Humanoid donor: skeleton, animator, attacks, AI, faction and loot.</param>
    /// <param name="Scale">Uniform prefab scale. Earth reads heavy partly through size.</param>
    /// <param name="Girth">Across-bone scale on top of the donor's measured height. 1.00 throughout: each
    /// element's proportion is authored into its geometry, and scaling it again here would double it.</param>
    internal sealed record Entry(string Element, Body Body, string Donor, float Scale, float Girth, string Home)
    {
        internal string ModelId => $"underworld-surtling-{Element.ToLowerInvariant()}-{Body.ToString().ToLowerInvariant()}";
        internal string Prefab => $"Magenheim_Underworld_Surtling_{Element}_{Body}";
        internal string DisplayName => $"{Element} Surtling";
    }

    internal static readonly Entry[] All =
    {
        // Charred_Melee measures 2.32m to the head joint; 0.72 lands a Fire Surtling at adult height.
        new("Fire", Body.Feminine, "Charred_Melee", 0.70f, 1.00f, "Sulfurous Wastes"),
        new("Fire", Body.Masculine, "Charred_Melee", 0.74f, 1.00f, "Sulfurous Wastes"),
        new("Water", Body.Feminine, "Draugr", 1.00f, 1.00f, "Blackwater Deep"),
        new("Water", Body.Masculine, "Draugr", 1.05f, 1.00f, "Blackwater Deep"),
        new("Earth", Body.Feminine, "Draugr_Elite", 1.10f, 1.00f, "Fracture Zones"),
        new("Earth", Body.Masculine, "Draugr_Elite", 1.18f, 1.00f, "Fracture Zones"),
        new("Wind", Body.Feminine, "Skeleton", 1.02f, 1.00f, "Fracture Zones"),
        new("Wind", Body.Masculine, "Skeleton", 1.06f, 1.00f, "Fracture Zones"),
        new("Radiance", Body.Feminine, "Draugr", 1.04f, 1.00f, "Luminous crystal sites"),
        new("Radiance", Body.Masculine, "Draugr", 1.08f, 1.00f, "Luminous crystal sites"),
        new("Umbral", Body.Feminine, "Skeleton", 1.06f, 1.00f, "Low-luminosity deeps"),
        new("Umbral", Body.Masculine, "Skeleton", 1.10f, 1.00f, "Low-luminosity deeps"),
    };

    /// <summary>Humanoid donors surveyed at registration so the choice above rests on logged evidence.</summary>
    internal static readonly string[] DonorCandidates =
    {
        "Draugr", "Draugr_Elite", "Draugr_Ranged", "Skeleton", "Skeleton_Poison", "Charred_Melee", "Charred_Mage",
        "Charred_Archer", "Dverger", "DvergerMage", "DvergerMageFire", "DvergerMageIce", "DvergerMageSupport",
        "Goblin", "GoblinBrute", "GoblinShaman", "Fenring", "Troll", "BogWitchKvastur",
    };
}
