using System;
using System.IO;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using Magenheim.Core;
using HarmonyLib;

namespace Magenheim
{
    [BepInPlugin(PluginConstants.Guid, PluginConstants.Name, PluginConstants.Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public sealed class MagenheimPlugin : BaseUnityPlugin
    {
        public DefinitionSnapshot Definitions { get; private set; }
        private void Awake()
        {
            try
            {
                string directory = Path.GetDirectoryName(Info.Location);
                var definitions = DefinitionLoader.Load(Path.Combine(directory, "default-data", "foundation.json"));
                var localization = LocalizationManager.Instance.GetLocalization();
                localization.AddJsonFile("English", File.ReadAllText(Path.Combine(directory, "Translations", "English.json")));
                GeologistPanel.ShapingSkill = SkillManager.Instance.AddSkill(new SkillConfig {
                    Identifier = PluginConstants.CrystalShapingId,
                    Name = "$skill_magenheim_crystal_shaping",
                    Description = "$skill_magenheim_crystal_shaping_desc",
                    IncreaseStep = 1f
                });
                Definitions = definitions;
                EarthContent.Log=Logger;
                GeologistPanel.Instance=gameObject.AddComponent<GeologistPanel>();
                GeologistPanel.Instance.Data=definitions;
                new Harmony(PluginConstants.Guid).PatchAll();
                PrefabManager.OnVanillaPrefabsAvailable += EarthContent.Register;
                Logger.LogInfo("Magenheim " + PluginConstants.Version + " foundation ready; elements=" + definitions.Elements.Count
                    + "; geodes=" + definitions.Geodes.Count + "; transitions=" + definitions.Steps.Count + "; SHA256=" + definitions.Hash);
                Logger.LogInfo("Earth gameplay prototype enabled. Workstation transactions restricted to solo worlds pending multiplayer authority.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Magenheim initialization failed: " + ex);
                enabled = false;
            }
        }
    }
}
