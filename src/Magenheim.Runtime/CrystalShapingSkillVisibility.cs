using HarmonyLib;

namespace Magenheim.Runtime;

// Valheim's dialog lists instantiated player skills, not all registered definitions.
// Initialize our skill through the normal lookup before the dialog takes its snapshot.
[HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
internal static class CrystalShapingSkillVisibility
{
    private static void Prefix(Player player)
    {
        if (player is null) return;
        var skills = player.GetSkills();
        var skillType = EarthContentRegistrar.CrystalShapingSkill;
        if (skills is null || skillType == Skills.SkillType.None) return;

        // Do not create an invalid skill if another mod has removed its definition.
        if (!skills.m_skills.Exists(definition => definition.m_skill == skillType)) return;

        // GetSkillLevel creates a missing level-zero entry and preserves existing XP.
        // Raising the skill, even artificially, is not necessary for visibility.
        skills.GetSkillLevel(skillType);
    }
}
