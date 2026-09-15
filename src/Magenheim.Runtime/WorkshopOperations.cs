using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Transactions;
using Magenheim.Runtime.Networking;
using UnityEngine;

namespace Magenheim.Runtime;

internal enum WorkshopOperationKind
{
    OpenGeode = 0,
    RefineCrystal = 1,
}

internal sealed class WorkshopOperationDefinition
{
    internal WorkshopOperationDefinition(string recipeName, WorkshopOperationKind kind, string sourcePrefab, string previewOutputPrefab, int minimumStationLevel, string requiredStationIdentity, string? geodeId, ElementalAlignment? element, CrystalTier? sourceTier)
    {
        RecipeName = recipeName; Kind = kind; SourcePrefab = sourcePrefab; PreviewOutputPrefab = previewOutputPrefab; MinimumStationLevel = minimumStationLevel; RequiredStationIdentity = requiredStationIdentity; GeodeId = geodeId; Element = element; SourceTier = sourceTier;
    }
    internal string RecipeName { get; }
    internal WorkshopOperationKind Kind { get; }
    internal string SourcePrefab { get; }
    internal string PreviewOutputPrefab { get; }
    internal int MinimumStationLevel { get; }
    internal string RequiredStationIdentity { get; }
    internal string? GeodeId { get; }
    internal ElementalAlignment? Element { get; }
    internal CrystalTier? SourceTier { get; }
}

internal static class WorkshopOperationCatalog
{
    private const string GeodePrefabPrefix = "Magenheim_Geode_";
    private static IReadOnlyList<WorkshopOperationDefinition> _definitions = Array.Empty<WorkshopOperationDefinition>();
    private static Dictionary<string, WorkshopOperationDefinition> _byRecipe = new(StringComparer.Ordinal);
    private static bool _configured;
    internal static void Configure(MagenheimDefinitionSet definitions)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (_configured) return;
        var operations = new List<WorkshopOperationDefinition>();
        foreach (var geode in definitions.Geodes)
        {
            var dominant = ElementVisualPalette.DominantElement(geode);
            operations.Add(new WorkshopOperationDefinition(BuildOpenRecipeName(geode.PrefabName), WorkshopOperationKind.OpenGeode, geode.PrefabName, $"Magenheim_Crystal_{dominant}_Rough", 1, WorkshopRegistrar.StationPrefab, geode.Id, null, null));
        }
        foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
            foreach (var rule in definitions.RefinementRules.OrderBy(rule => (int)rule.SourceTier))
                operations.Add(new WorkshopOperationDefinition($"Magenheim_Operation_Refine_{element}_{rule.SourceTier}_{rule.DestinationTier}", WorkshopOperationKind.RefineCrystal, $"Magenheim_Crystal_{element}_{rule.SourceTier}", $"Magenheim_Crystal_{element}_{rule.DestinationTier}", (int)rule.SourceTier + 1, rule.RequiredStation, null, element, rule.SourceTier));
        var duplicates = operations.GroupBy(operation => operation.RecipeName, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        if (duplicates.Length > 0) throw new InvalidOperationException("Duplicate Magenheim workshop operation identity: " + string.Join(", ", duplicates));
        _definitions = operations.AsReadOnly(); _byRecipe = operations.ToDictionary(operation => operation.RecipeName, StringComparer.Ordinal); _configured = true;
    }
    internal static IReadOnlyList<WorkshopOperationDefinition> All { get { EnsureConfigured(); return _definitions; } }
    internal static bool TryGet(string recipeName, out WorkshopOperationDefinition definition) { EnsureConfigured(); return _byRecipe.TryGetValue(recipeName, out definition!); }
    private static string BuildOpenRecipeName(string prefabName)
    {
        if (!prefabName.StartsWith(GeodePrefabPrefix, StringComparison.Ordinal)) throw new InvalidOperationException($"Geode prefab '{prefabName}' does not use the required '{GeodePrefabPrefix}' identity prefix.");
        return "Magenheim_Operation_Open_" + prefabName.Substring(GeodePrefabPrefix.Length);
    }
    private static void EnsureConfigured() { if (!_configured) throw new InvalidOperationException("Workshop operation catalog was used before definition authority configured it."); }
}

internal sealed class WorkshopOperationRegistrar : IDisposable
{
    private readonly ManualLogSource _log; private bool _subscribed; private bool _registered;
    internal WorkshopOperationRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));
    internal void Register() { if (_subscribed || _registered) return; PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable; _subscribed = true; }
    private void OnVanillaPrefabsAvailable()
    {
        if (_registered) return;
        try
        {
            foreach (var operation in WorkshopOperationCatalog.All)
            {
                if (PrefabManager.Instance.GetPrefab(operation.SourcePrefab) is null) throw new InvalidOperationException($"Workshop operation source prefab '{operation.SourcePrefab}' is unavailable.");
                if (PrefabManager.Instance.GetPrefab(operation.PreviewOutputPrefab) is null) throw new InvalidOperationException($"Workshop operation preview prefab '{operation.PreviewOutputPrefab}' is unavailable.");
                var config = new RecipeConfig { Name = operation.RecipeName, Item = operation.PreviewOutputPrefab, Amount = 1, CraftingStation = WorkshopRegistrar.StationPrefab, MinStationLevel = operation.MinimumStationLevel, Enabled = true };
                config.AddRequirement(operation.SourcePrefab, 1);
                if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config))) throw new InvalidOperationException($"Jotunn refused workshop operation recipe '{operation.RecipeName}'.");
            }
            _registered = true;
            var geodeOperations = WorkshopOperationCatalog.All.Count(operation => operation.Kind == WorkshopOperationKind.OpenGeode);
            var refinementOperations = WorkshopOperationCatalog.All.Count(operation => operation.Kind == WorkshopOperationKind.RefineCrystal);
            _log.LogInfo($"Registered {WorkshopOperationCatalog.All.Count} Geologist's Workstation operations: {geodeOperations} biome geode openings and {refinementOperations} elemental refinement operations.");
        }
        catch (Exception exception) { _log.LogError($"Geologist's Workstation operation registration failed: {exception}"); throw; }
        finally { Dispose(); }
    }
    public void Dispose() { if (!_subscribed) return; PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable; _subscribed = false; }
}

internal static class WorkshopOperationsRuntime
{
    private const long LocalPeerId = 0L; private const long LocalSessionGeneration = 1L;
    private static RuntimeServices? _services; private static DefinitionAuthoritySynchronizer? _authority; private static ManualLogSource? _log;
    internal static void Configure(RuntimeServices services, DefinitionAuthoritySynchronizer authority, ManualLogSource log) { _services = services ?? throw new ArgumentNullException(nameof(services)); _authority = authority ?? throw new ArgumentNullException(nameof(authority)); _log = log ?? throw new ArgumentNullException(nameof(log)); }
    internal static bool TryHandleCrafting(InventoryGui gui, Player player)
    {
        if (gui is null || player is null) return false;
        var recipe = RuntimeGameApi.GetCraftRecipe(gui); if (recipe is null || !WorkshopOperationCatalog.TryGet(recipe.name, out var operation)) return false;
        try
        {
            if (_services is null || _authority is null || _log is null) throw new InvalidOperationException("Workshop operation runtime was not configured before crafting.");
            if (ZNet.instance is null) { player.Message(MessageHud.MessageType.Center, "Magenheim workstation transactions require an active network session."); return true; }
            if (!ZNet.instance.IsServer()) { WorkshopOperationRpc.TrySubmitClient(gui, player, operation, out var remoteDiagnostic); player.Message(MessageHud.MessageType.Center, remoteDiagnostic); return true; }
            if (!TryGetCurrentMagenheimStation(player, out var station)) { player.Message(MessageHud.MessageType.Center, "Use a Geologist's Workstation for this operation."); return true; }
            var inventory = player.GetInventory(); var source = FindSource(inventory, operation.SourcePrefab);
            if (source is null) { player.Message(MessageHud.MessageType.Center, "The required source item is no longer in your inventory."); return true; }
            if (operation.Kind == WorkshopOperationKind.OpenGeode) ExecuteLocalGeodeOpening(gui, player, inventory, source, operation); else ExecuteLocalRefinement(gui, player, inventory, source, station, operation);
        }
        catch (Exception exception) { _log?.LogError($"Geologist's Workstation operation '{operation.RecipeName}' failed before completion: {exception}"); player.Message(MessageHud.MessageType.Center, "Magenheim operation failed safely; source inventory was not intentionally consumed."); }
        return true;
    }
    private static void ExecuteLocalGeodeOpening(InventoryGui gui, Player player, Inventory inventory, ItemDrop.ItemData source, WorkshopOperationDefinition operation)
    {
        var services = _services!; var authority = _authority!; var geode = services.Definitions.Geodes.Single(entry => string.Equals(entry.Id, operation.GeodeId, StringComparison.Ordinal));
        var skill = Mathf.Clamp(Mathf.FloorToInt(player.GetSkillLevel(EarthContentRegistrar.CrystalShapingSkill)), 0, 100);
        var crackingRequest = new GeodeCrackingRequest(geode, ServerRandom.NextUnit(), ServerRandom.NextUnit(), new[] { ServerRandom.NextUnit(), ServerRandom.NextUnit(), ServerRandom.NextUnit() }, skill);
        var preview = GeodeCrackingService.Crack(crackingRequest); if (!preview.IsSuccess) { player.Message(MessageHud.MessageType.Center, preview.Reason); return; }
        var grants = preview.Crystals.GroupBy(CrystalPrefab).Select(group => new InventoryGrant(group.Key, group.Count())).ToArray();
        var fits = WorkshopInventoryTransactions.CanApply(inventory, source, 1, grants, out var capacityReason);
        var request = new GeodeOpeningTransactionRequest(authority.LocalAuthorityResult, source.m_stack, fits ? preview.Crystals.Count : 0, crackingRequest);
        var key = new GeodeOpeningOperationKey(LocalPeerId, LocalSessionGeneration, Guid.NewGuid().ToString("N")); var decision = services.GeodeOpeningOperations.Begin(key, request);
        if (!decision.MutationAuthorized) { player.Message(MessageHud.MessageType.Center, fits ? decision.Diagnostic : capacityReason); return; }
        if (!WorkshopInventoryTransactions.TryApplyAndCommit(inventory, source, decision.Plan.ConsumeGeodeCount, grants, () => services.GeodeOpeningOperations.MarkApplied(key), out var mutationError)) { services.GeodeOpeningOperations.AbortPrepared(key); player.Message(MessageHud.MessageType.Center, mutationError); return; }
        player.RaiseSkill(EarthContentRegistrar.CrystalShapingSkill, CrystalShapingExperience.ForGeodeCracking(decision.Plan.GrantCrystals.Count));
        player.Message(MessageHud.MessageType.Center, $"Opened {ElementVisualPalette.DisplayBiome(geode.Biome)} geode: {decision.Plan.GrantCrystals.Count} Rough crystal(s)."); RuntimeGameApi.RefreshCraftingPanel(gui);
    }
    private static void ExecuteLocalRefinement(InventoryGui gui, Player player, Inventory inventory, ItemDrop.ItemData source, CraftingStation station, WorkshopOperationDefinition operation)
    {
        var services = _services!; var authority = _authority!;
        if (operation.SourceTier is null || operation.Element is null) throw new InvalidOperationException($"Refinement operation '{operation.RecipeName}' has incomplete elemental/tier identity.");
        if (!HasRequiredStationIdentity(station, operation.RequiredStationIdentity)) { player.Message(MessageHud.MessageType.Center, $"This refinement requires {RequiredStationDisplay(operation.RequiredStationIdentity)}."); return; }
        var skill = Mathf.Clamp(Mathf.FloorToInt(player.GetSkillLevel(EarthContentRegistrar.CrystalShapingSkill)), 0, 100);
        var refinementRequest = new RefinementRequest(new Crystal(operation.Element.Value, operation.SourceTier.Value), skill, operation.RequiredStationIdentity, ServerRandom.NextUnit());
        var preview = services.Refinement.Refine(refinementRequest); if (preview.Outcome != RefinementOutcome.Success && preview.Outcome != RefinementOutcome.FailedDestroyed) { player.Message(MessageHud.MessageType.Center, preview.Reason); return; }
        InventoryGrant[] grants;
        if (preview.Outcome == RefinementOutcome.Success) { if (preview.Output is null) throw new InvalidOperationException("Successful refinement preview has no output."); grants = new[] { new InventoryGrant(CrystalPrefab(preview.Output.Value), 1) }; }
        else grants = preview.ShardReturnCount > 0 ? new[] { new InventoryGrant(ShardPrefab(refinementRequest.Input.Element), preview.ShardReturnCount) } : Array.Empty<InventoryGrant>();
        var fits = WorkshopInventoryTransactions.CanApply(inventory, source, 1, grants, out var capacityReason);
        var request = new RefinementTransactionRequest(authority.LocalAuthorityResult, source.m_stack, fits ? (grants.Length == 0 ? 0 : 1) : 0, refinementRequest);
        var key = new RefinementOperationKey(LocalPeerId, LocalSessionGeneration, Guid.NewGuid().ToString("N")); var decision = services.RefinementOperations.Begin(key, request);
        if (!decision.MutationAuthorized) { player.Message(MessageHud.MessageType.Center, fits ? decision.Diagnostic : capacityReason); return; }
        if (!WorkshopInventoryTransactions.TryApplyAndCommit(inventory, source, decision.Plan.ConsumeSourceCount, grants, () => services.RefinementOperations.MarkApplied(key), out var mutationError)) { services.RefinementOperations.AbortPrepared(key); player.Message(MessageHud.MessageType.Center, mutationError); return; }
        if (decision.Plan.AwardExperience) player.RaiseSkill(EarthContentRegistrar.CrystalShapingSkill, CrystalShapingExperience.ForRefinementAttempt(operation.SourceTier.Value));
        player.Message(MessageHud.MessageType.Center, decision.Plan.Diagnostic); RuntimeGameApi.RefreshCraftingPanel(gui);
    }
    private static ItemDrop.ItemData? FindSource(Inventory inventory, string prefabName) => inventory.GetAllItems().FirstOrDefault(item => item.m_dropPrefab && string.Equals(item.m_dropPrefab.name, prefabName, StringComparison.Ordinal));
    private static bool TryGetCurrentMagenheimStation(Player player, out CraftingStation station) { station = player.GetCurrentCraftingStation(); return station && string.Equals(NormalizeCloneName(station.gameObject.name), WorkshopRegistrar.StationPrefab, StringComparison.Ordinal); }
    private static bool HasRequiredStationIdentity(CraftingStation station, string requiredIdentity) { if (string.Equals(requiredIdentity, WorkshopRegistrar.StationPrefab, StringComparison.Ordinal)) return true; var extensions = new List<StationExtension>(); StationExtension.FindExtensions(station, station.transform.position, extensions); return extensions.Any(extension => string.Equals(NormalizeCloneName(extension.gameObject.name), requiredIdentity, StringComparison.Ordinal)); }
    private static string RequiredStationDisplay(string identity) { if (string.Equals(identity, WorkshopRegistrar.FracturingPrefab, StringComparison.Ordinal)) return "Fracturing Block"; if (string.Equals(identity, WorkshopRegistrar.FacetingPrefab, StringComparison.Ordinal)) return "Faceting Wheel"; if (string.Equals(identity, WorkshopRegistrar.ResonancePrefab, StringComparison.Ordinal)) return "Resonance Frame"; return "Geologist's Workstation"; }
    private static string NormalizeCloneName(string name) => name.EndsWith("(Clone)", StringComparison.Ordinal) ? name.Substring(0, name.Length - "(Clone)".Length) : name;
    private static string CrystalPrefab(Crystal crystal) => $"Magenheim_Crystal_{crystal.Element}_{crystal.Tier}";
    private static string ShardPrefab(ElementalAlignment element) => $"Magenheim_Shard_{element}";
}

internal readonly struct InventoryGrant { internal InventoryGrant(string prefabName, int amount) { PrefabName = prefabName; Amount = amount; } internal string PrefabName { get; } internal int Amount { get; } }
internal static class WorkshopInventoryTransactions
{
    internal static bool CanApply(Inventory inventory, ItemDrop.ItemData source, int consumeAmount, IReadOnlyList<InventoryGrant> grants, out string reason)
    {
        if (consumeAmount <= 0 || !inventory.ContainsItem(source) || source.m_stack < consumeAmount) { reason = "The source item is no longer available in the required amount."; return false; }
        var emptySlots = inventory.GetEmptySlots() + (source.m_stack == consumeAmount ? 1 : 0); var items = inventory.GetAllItems();
        foreach (var grantGroup in grants.GroupBy(grant => grant.PrefabName, StringComparer.Ordinal))
        {
            var prefab = PrefabManager.Instance.GetPrefab(grantGroup.Key); var drop = prefab ? prefab.GetComponent<ItemDrop>() : null; if (!drop) { reason = $"Output prefab '{grantGroup.Key}' is unavailable; source was not consumed."; return false; }
            var prototype = drop.m_itemData; var needed = grantGroup.Sum(grant => grant.Amount); if (needed < 0) { reason = "Output amount cannot be negative."; return false; }
            var freeStackSpace = items.Where(item => string.Equals(item.m_shared.m_name, prototype.m_shared.m_name, StringComparison.Ordinal) && item.m_quality == prototype.m_quality).Sum(item => Math.Max(0, item.m_shared.m_maxStackSize - item.m_stack));
            var remainder = Math.Max(0, needed - freeStackSpace); if (remainder == 0) continue; var stackSize = Math.Max(1, prototype.m_shared.m_maxStackSize); var requiredSlots = (remainder + stackSize - 1) / stackSize; emptySlots -= requiredSlots;
            if (emptySlots < 0) { reason = "Not enough inventory capacity for the operation output; source was not consumed."; return false; }
        }
        reason = string.Empty; return true;
    }
    internal static bool TryApply(Inventory inventory, ItemDrop.ItemData source, int consumeAmount, IReadOnlyList<InventoryGrant> grants, out string error) => TryApplyCore(inventory, source, consumeAmount, grants, null, out error);
    internal static bool TryApplyAndCommit(Inventory inventory, ItemDrop.ItemData source, int consumeAmount, IReadOnlyList<InventoryGrant> grants, Func<bool> commitReplayState, out string error) { if (commitReplayState is null) throw new ArgumentNullException(nameof(commitReplayState)); return TryApplyCore(inventory, source, consumeAmount, grants, commitReplayState, out error); }
    private static bool TryApplyCore(Inventory inventory, ItemDrop.ItemData source, int consumeAmount, IReadOnlyList<InventoryGrant> grants, Func<bool>? commitReplayState, out string error)
    {
        if (!CanApply(inventory, source, consumeAmount, grants, out error)) return false; var beforeItems = inventory.GetAllItems().ToArray(); var beforeStacks = beforeItems.ToDictionary(item => item, item => item.m_stack);
        try { if (!inventory.RemoveItem(source, consumeAmount)) { error = "Source item changed before transaction application; nothing was granted."; return false; } foreach (var grant in grants) { if (grant.Amount <= 0) continue; var prefab = PrefabManager.Instance.GetPrefab(grant.PrefabName) ?? throw new InvalidOperationException($"Output prefab '{grant.PrefabName}' disappeared after capacity preflight."); if (!inventory.AddItem(prefab, grant.Amount)) throw new InvalidOperationException($"Inventory refused preflighted output '{grant.PrefabName}' x{grant.Amount}."); } if (commitReplayState is not null && !commitReplayState()) throw new InvalidOperationException("Replay-state commit refused the applied inventory transaction."); error = string.Empty; return true; }
        catch (Exception exception) { RollBack(inventory, beforeItems, beforeStacks, source); error = $"Inventory transaction rolled back: {exception.Message}"; return false; }
    }
    private static void RollBack(Inventory inventory, ItemDrop.ItemData[] beforeItems, IReadOnlyDictionary<ItemDrop.ItemData, int> beforeStacks, ItemDrop.ItemData source)
    {
        var originalSet = new HashSet<ItemDrop.ItemData>(beforeItems); foreach (var current in inventory.GetAllItems().ToArray()) if (!originalSet.Contains(current)) inventory.RemoveItem(current); foreach (var original in beforeItems) if (inventory.ContainsItem(original)) original.m_stack = beforeStacks[original];
        if (!inventory.ContainsItem(source)) { source.m_stack = beforeStacks[source]; if (!inventory.AddItem(source)) throw new InvalidOperationException("Rollback could not restore the consumed source item."); }
        RuntimeGameApi.NotifyInventoryChanged(inventory);
    }
}
internal static class ServerRandom { private static readonly System.Security.Cryptography.RandomNumberGenerator Generator = System.Security.Cryptography.RandomNumberGenerator.Create(); private static readonly object Sync = new object(); internal static double NextUnit() { var bytes = new byte[4]; lock (Sync) Generator.GetBytes(bytes); var value = BitConverter.ToUInt32(bytes, 0); return value / ((double)uint.MaxValue + 1d); } }
[HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
internal static class WorkshopCraftingPatch { private static bool Prefix(InventoryGui __instance, Player player) => !WorkshopOperationsRuntime.TryHandleCrafting(__instance, player); }
