using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Magenheim.Core;
using Magenheim.Core.Socketing;
using Magenheim.Runtime.Networking;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Explicit per-item socket management surface shown only while the local player is using
/// the Geologist's Workstation. It never edits shared prefabs: all mutation is performed
/// through Magenheim's namespaced ItemData metadata and pure-core transaction planners.
/// Remote clients submit immutable socket intent to the server and apply only the approved
/// plan to the exact selected ItemData after stale-state revalidation.
/// </summary>
internal sealed class SocketWorkstationOverlay : MonoBehaviour
{
    private const long LocalPeerId = 0L;
    private const long LocalSessionGeneration = 1L;
    private const int WindowId = 0x4D474E48; // MGNH

    private RuntimeServices? _services;
    private DefinitionAuthoritySynchronizer? _authority;
    private ManualLogSource? _log;
    private Rect _window = new Rect(0f, 0f, 470f, 620f);
    private Vector2 _equipmentScroll;
    private Vector2 _crystalScroll;
    private ItemDrop.ItemData? _selectedEquipment;
    private string _status = "Select equipment to manage its Magenheim sockets.";

    internal void Configure(
        RuntimeServices services,
        DefinitionAuthoritySynchronizer authority,
        ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _window.x = Math.Max(10f, Screen.width - _window.width - 24f);
        _window.y = Math.Max(10f, (Screen.height - _window.height) * 0.5f);
    }

    private void OnGUI()
    {
        var player = Player.m_localPlayer;
        if (_services is null || _authority is null || player is null ||
            !InventoryGui.IsVisible() || !TryGetMagenheimStation(player, out _))
        {
            _selectedEquipment = null;
            return;
        }

        _window = GUILayout.Window(WindowId, _window, DrawWindow, "Magenheim Socketing");
    }

    private void DrawWindow(int id)
    {
        try
        {
            var player = Player.m_localPlayer;
            if (player is null || _services is null || _authority is null)
                return;

            var inventory = player.GetInventory();
            if (_selectedEquipment is not null && !inventory.ContainsItem(_selectedEquipment))
                _selectedEquipment = null;

            GUILayout.Label("Equipment");
            GUILayout.Label("Choose an individual weapon, armor piece, shield, tool, or utility item. Foreign prefabs are never rewritten.");

            _equipmentScroll = GUILayout.BeginScrollView(_equipmentScroll, GUILayout.Height(190f));
            foreach (var candidate in EligibleOrSocketedEquipment(inventory))
            {
                var stateText = DescribeSocketState(candidate, out var malformed);
                var descriptor = ItemSocketAdapter.Describe(candidate);
                var selected = ReferenceEquals(candidate, _selectedEquipment) ? "> " : string.Empty;
                var label = $"{selected}{DisplayName(candidate)} [{descriptor.Category}] {stateText}";
                if (malformed) label += " [metadata error]";
                if (GUILayout.Button(label))
                {
                    _selectedEquipment = candidate;
                    _status = malformed
                        ? "Selected item has malformed Magenheim metadata. Mutation is disabled until that metadata is repaired."
                        : $"Selected {DisplayName(candidate)}.";
                }
            }
            GUILayout.EndScrollView();

            if (_selectedEquipment is not null)
                DrawSelectedEquipment(player, inventory, _selectedEquipment);
            else
                GUILayout.Label("No equipment selected.");

            GUILayout.Space(6f);
            GUILayout.Label("Status");
            GUILayout.TextArea(_status, GUILayout.Height(58f));
        }
        catch (Exception exception)
        {
            _log?.LogError($"Magenheim socket workstation UI failed safely: {exception}");
            _status = "Socketing UI failed safely; no intentional item mutation was completed.";
        }

        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }

    private void DrawSelectedEquipment(Player player, Inventory inventory, ItemDrop.ItemData equipment)
    {
        if (_services is null || _authority is null) return;

        var descriptor = ItemSocketAdapter.Describe(equipment);
        if (!ItemSocketAdapter.TryRead(equipment, out var state, out var metadataError))
        {
            GUILayout.Label($"Socket metadata error: {metadataError}");
            return;
        }

        var eligibility = SocketEligibilityService.Evaluate(descriptor, _services.SocketPolicy);
        GUILayout.Label($"Selected: {DisplayName(equipment)}");
        GUILayout.Label($"Origin: {descriptor.ModOrigin} | Category: {descriptor.Category}");
        GUILayout.Label($"Sockets: {state.InstalledCrystals.Count}/{state.UnlockedSlots} unlocked, policy maximum {eligibility.MaximumSlots}");

        if (!eligibility.IsEligible)
            GUILayout.Label($"New socket/install operations disabled: {eligibility.Reason}");

        if (state.InstalledCrystals.Count > 0)
        {
            GUILayout.Label("Installed crystals:");
            for (var i = 0; i < state.InstalledCrystals.Count; i++)
            {
                var crystal = state.InstalledCrystals[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}. {crystal.Tier} {crystal.Element}");
                GUI.enabled = CanRequestMutation() && HasFacetingWheel(player);
                if (GUILayout.Button("Extract", GUILayout.Width(90f)))
                    TryExtract(player, inventory, equipment, i);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            if (!HasFacetingWheel(player))
                GUILayout.Label("Crystal extraction requires the Faceting Wheel upgrade.");
        }

        GUI.enabled = CanRequestMutation() && eligibility.IsEligible;
        if (GUILayout.Button("Open one socket"))
            TryAddSlot(player, inventory, equipment);
        GUI.enabled = true;

        GUILayout.Label("Install a crystal");
        _crystalScroll = GUILayout.BeginScrollView(_crystalScroll, GUILayout.Height(120f));
        var crystals = SocketableCrystals(inventory).ToArray();
        if (crystals.Length == 0)
        {
            GUILayout.Label("No Simple-or-better Magenheim crystals are in inventory.");
        }
        else
        {
            foreach (var crystalItem in crystals)
            {
                if (!TryParseCrystal(crystalItem, out var crystal)) continue;
                GUI.enabled = CanRequestMutation() && eligibility.IsEligible && state.FreeSlots > 0;
                if (GUILayout.Button($"Install {crystal.Tier} {crystal.Element} (x{crystalItem.m_stack})"))
                    TryInstall(player, inventory, equipment, crystalItem, crystal);
                GUI.enabled = true;
            }
        }
        GUILayout.EndScrollView();

        if (IsRemoteMutationClient())
        {
            GUILayout.Label(
                "Remote socket changes are resolved by the server, then applied only if this exact item still matches the approved original state.");
        }
        else if (!IsLocalMutationHost())
        {
            GUILayout.Label(
                "Socket mutation is unavailable until the server admits this client's synchronized Magenheim gameplay authority.");
        }
    }

    private IEnumerable<ItemDrop.ItemData> EligibleOrSocketedEquipment(Inventory inventory)
    {
        if (_services is null) yield break;

        foreach (var item in inventory.GetAllItems())
        {
            var descriptor = ItemSocketAdapter.Describe(item);
            var eligibility = SocketEligibilityService.Evaluate(descriptor, _services.SocketPolicy);
            var hasMetadata = item.m_customData is not null &&
                              item.m_customData.ContainsKey(SocketMetadataCodec.CustomDataKey);
            if (eligibility.IsEligible || hasMetadata)
                yield return item;
        }
    }

    private static IEnumerable<ItemDrop.ItemData> SocketableCrystals(Inventory inventory)
    {
        foreach (var item in inventory.GetAllItems())
        {
            if (TryParseCrystal(item, out var crystal) && crystal.Tier != CrystalTier.Rough)
                yield return item;
        }
    }

    private void TryAddSlot(Player player, Inventory inventory, ItemDrop.ItemData equipment)
    {
        if (_services is null || _authority is null) return;
        if (!IsLocalMutationHost())
        {
            if (IsRemoteMutationClient())
            {
                SocketOperationRpc.TrySubmitAddSlot(player, equipment, out _status);
                return;
            }

            _status = "Socket mutation requires admitted server authority.";
            return;
        }

        if (!ItemSocketAdapter.TryRead(equipment, out var state, out var metadataError))
        {
            _status = metadataError;
            return;
        }

        var key = new SocketOperationKey(LocalPeerId, LocalSessionGeneration, Guid.NewGuid().ToString("N"));
        var request = new SocketTransactionRequest(
            _authority.LocalAuthorityResult,
            ItemSocketAdapter.Describe(equipment),
            _services.SocketPolicy,
            state,
            SocketTransactionKind.AddSlot,
            null,
            0);
        var decision = _services.SocketOperations.Begin(key, request);
        if (!decision.MutationAuthorized)
        {
            _status = decision.Diagnostic;
            return;
        }

        if (!SocketStatesStillMatch(equipment, decision.Plan.OriginalState, out var staleReason))
        {
            _services.SocketOperations.AbortPrepared(key);
            _status = staleReason;
            return;
        }

        try
        {
            ItemSocketAdapter.Write(equipment, decision.Plan.ResultState);
            NotifyInventoryChanged(inventory);
            if (!_services.SocketOperations.MarkApplied(key))
                throw new InvalidOperationException("Applied socket-open operation could not be committed to replay state.");
            _status = decision.Plan.Diagnostic;
        }
        catch
        {
            _services.SocketOperations.AbortPrepared(key);
            throw;
        }
    }

    private void TryInstall(
        Player player,
        Inventory inventory,
        ItemDrop.ItemData equipment,
        ItemDrop.ItemData crystalItem,
        Crystal crystal)
    {
        if (_services is null || _authority is null) return;
        if (!IsLocalMutationHost())
        {
            if (IsRemoteMutationClient())
            {
                SocketOperationRpc.TrySubmitInstall(player, equipment, crystalItem, crystal, out _status);
                return;
            }

            _status = "Crystal installation requires admitted server authority.";
            return;
        }
        if (!inventory.ContainsItem(crystalItem) || crystalItem.m_stack < 1)
        {
            _status = "Selected crystal is no longer available.";
            return;
        }
        if (!ItemSocketAdapter.TryRead(equipment, out var state, out var metadataError))
        {
            _status = metadataError;
            return;
        }

        var key = new SocketOperationKey(LocalPeerId, LocalSessionGeneration, Guid.NewGuid().ToString("N"));
        var request = new SocketTransactionRequest(
            _authority.LocalAuthorityResult,
            ItemSocketAdapter.Describe(equipment),
            _services.SocketPolicy,
            state,
            SocketTransactionKind.InstallCrystal,
            crystal,
            crystalItem.m_stack);
        var decision = _services.SocketOperations.Begin(key, request);
        if (!decision.MutationAuthorized)
        {
            _status = decision.Diagnostic;
            return;
        }

        if (!SocketStatesStillMatch(equipment, decision.Plan.OriginalState, out var staleReason))
        {
            _services.SocketOperations.AbortPrepared(key);
            _status = staleReason;
            return;
        }

        var snapshot = InventorySnapshot.Capture(inventory);
        var oldMetadata = CaptureMagenheimMetadata(equipment);
        try
        {
            if (!inventory.RemoveItem(crystalItem, decision.Plan.ConsumeCrystalCount))
                throw new InvalidOperationException("Selected crystal changed before installation; no socket metadata was written.");

            ItemSocketAdapter.Write(equipment, decision.Plan.ResultState);
            NotifyInventoryChanged(inventory);

            if (!_services.SocketOperations.MarkApplied(key))
                throw new InvalidOperationException("Applied crystal-install operation could not be committed to replay state.");

            if (decision.Plan.CrystalShapingExperience > 0d)
                player.RaiseSkill(EarthContentRegistrar.CrystalShapingSkill, (float)decision.Plan.CrystalShapingExperience);
            _status = decision.Plan.Diagnostic;
        }
        catch (Exception exception)
        {
            snapshot.Restore(inventory);
            RestoreMagenheimMetadata(equipment, oldMetadata);
            _services.SocketOperations.AbortPrepared(key);
            NotifyInventoryChanged(inventory);
            _status = $"Crystal installation rolled back: {exception.Message}";
        }
    }

    private void TryExtract(
        Player player,
        Inventory inventory,
        ItemDrop.ItemData equipment,
        int crystalIndex)
    {
        if (_services is null || _authority is null) return;
        if (!IsLocalMutationHost())
        {
            if (IsRemoteMutationClient())
            {
                SocketOperationRpc.TrySubmitExtraction(player, equipment, crystalIndex, out _status);
                return;
            }

            _status = "Crystal extraction requires admitted server authority.";
            return;
        }
        if (!HasFacetingWheel(player))
        {
            _status = "Crystal extraction requires the Faceting Wheel upgrade.";
            return;
        }
        if (!ItemSocketAdapter.TryRead(equipment, out var state, out var metadataError))
        {
            _status = metadataError;
            return;
        }

        var skill = Mathf.Clamp(
            Mathf.FloorToInt(player.GetSkillLevel(EarthContentRegistrar.CrystalShapingSkill)),
            0,
            100);
        var key = new SocketExtractionOperationKey(
            LocalPeerId,
            LocalSessionGeneration,
            Guid.NewGuid().ToString("N"));
        var request = new SocketExtractionRequest(
            _authority.LocalAuthorityResult,
            state,
            crystalIndex,
            skill,
            SocketExtractionService.RequiredStationId,
            ServerRandom.NextUnit());
        var decision = _services.SocketExtractionOperations.Begin(key, request);
        if (!decision.MutationAuthorized)
        {
            _status = decision.Diagnostic;
            return;
        }

        if (!SocketStatesStillMatch(equipment, decision.Plan.OriginalState, out var staleReason))
        {
            _services.SocketExtractionOperations.AbortPrepared(key);
            _status = staleReason;
            return;
        }

        var outputPrefab = decision.Plan.ReturnsCrystal
            ? CrystalPrefab(decision.Plan.ReturnedCrystal!.Value)
            : decision.Plan.ShardElement is { } element
                ? ShardPrefab(element)
                : string.Empty;
        var outputAmount = decision.Plan.ReturnsCrystal ? 1 : decision.Plan.ShardReturnCount;
        var prefab = PrefabManager.Instance.GetPrefab(outputPrefab);
        if (!prefab || outputAmount <= 0)
        {
            _services.SocketExtractionOperations.AbortPrepared(key);
            _status = $"Extraction output '{outputPrefab}' is unavailable; socket state was not changed.";
            return;
        }
        if (!inventory.CanAddItem(prefab, outputAmount))
        {
            _services.SocketExtractionOperations.AbortPrepared(key);
            _status = "Not enough inventory capacity for extraction output; socket state was not changed.";
            return;
        }

        var snapshot = InventorySnapshot.Capture(inventory);
        var oldMetadata = CaptureMagenheimMetadata(equipment);
        try
        {
            ItemSocketAdapter.Write(equipment, decision.Plan.ResultState);
            if (!inventory.AddItem(prefab, outputAmount))
                throw new InvalidOperationException("Inventory refused the preflighted extraction output.");
            NotifyInventoryChanged(inventory);

            if (!_services.SocketExtractionOperations.MarkApplied(key))
                throw new InvalidOperationException("Applied extraction could not be committed to replay state.");

            if (decision.Plan.CrystalShapingExperience > 0d)
                player.RaiseSkill(EarthContentRegistrar.CrystalShapingSkill, (float)decision.Plan.CrystalShapingExperience);
            _status = decision.Plan.Diagnostic;
        }
        catch (Exception exception)
        {
            snapshot.Restore(inventory);
            RestoreMagenheimMetadata(equipment, oldMetadata);
            _services.SocketExtractionOperations.AbortPrepared(key);
            NotifyInventoryChanged(inventory);
            _status = $"Crystal extraction rolled back: {exception.Message}";
        }
    }

    private bool CanRequestMutation() => IsLocalMutationHost() || IsRemoteMutationClient();

    private bool IsLocalMutationHost() =>
        ZNet.instance is not null && ZNet.instance.IsServer() &&
        _authority is not null && _authority.LocalAuthorityResult.MutationAuthorized;

    private bool IsRemoteMutationClient() =>
        ZNet.instance is not null && !ZNet.instance.IsServer() &&
        _authority is not null && _authority.IsClientMutationAuthorized;

    private static bool TryGetMagenheimStation(Player player, out CraftingStation station)
    {
        station = player.GetCurrentCraftingStation();
        return station && string.Equals(
            NormalizeCloneName(station.gameObject.name),
            WorkshopRegistrar.StationPrefab,
            StringComparison.Ordinal);
    }

    private static bool HasFacetingWheel(Player player)
    {
        if (!TryGetMagenheimStation(player, out var station)) return false;
        var extensions = new List<StationExtension>();
        StationExtension.FindExtensions(station, station.transform.position, extensions);
        return extensions.Any(extension => string.Equals(
            NormalizeCloneName(extension.gameObject.name),
            WorkshopRegistrar.FacetingPrefab,
            StringComparison.Ordinal));
    }

    private static bool TryParseCrystal(ItemDrop.ItemData item, out Crystal crystal)
    {
        crystal = default;
        if (item is null || !item.m_dropPrefab) return false;
        const string prefix = "Magenheim_Crystal_";
        var prefabName = item.m_dropPrefab.name;
        if (!prefabName.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var remainder = prefabName.Substring(prefix.Length);
        var separator = remainder.LastIndexOf('_');
        if (separator <= 0 || separator >= remainder.Length - 1) return false;
        var elementText = remainder.Substring(0, separator);
        var tierText = remainder.Substring(separator + 1);
        if (!Enum.TryParse(elementText, false, out ElementalAlignment element) ||
            !Enum.IsDefined(typeof(ElementalAlignment), element) ||
            !Enum.TryParse(tierText, false, out CrystalTier tier) ||
            !Enum.IsDefined(typeof(CrystalTier), tier))
            return false;

        crystal = new Crystal(element, tier);
        return true;
    }

    private static string DescribeSocketState(ItemDrop.ItemData item, out bool malformed)
    {
        malformed = !ItemSocketAdapter.TryRead(item, out var state, out _);
        return malformed ? "sockets ?/?" : $"sockets {state.InstalledCrystals.Count}/{state.UnlockedSlots}";
    }

    private static string DisplayName(ItemDrop.ItemData item)
    {
        var localized = Localization.instance?.Localize(item.m_shared.m_name);
        if (!string.IsNullOrWhiteSpace(localized) && localized != item.m_shared.m_name)
            return localized;
        return item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name;
    }

    private static bool SocketStatesStillMatch(
        ItemDrop.ItemData equipment,
        SocketState expected,
        out string reason)
    {
        if (!ItemSocketAdapter.TryRead(equipment, out var current, out var diagnostic))
        {
            reason = $"Socket metadata changed or became invalid before mutation: {diagnostic}";
            return false;
        }

        if (!StatesMatch(current, expected))
        {
            reason = "Socket metadata changed after operation planning; mutation was cancelled without overwriting the newer item state.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool StatesMatch(SocketState left, SocketState right)
    {
        if (left.UnlockedSlots != right.UnlockedSlots ||
            left.InstalledCrystals.Count != right.InstalledCrystals.Count)
            return false;
        for (var i = 0; i < left.InstalledCrystals.Count; i++)
            if (left.InstalledCrystals[i] != right.InstalledCrystals[i]) return false;
        return true;
    }

    private static MetadataSnapshot CaptureMagenheimMetadata(ItemDrop.ItemData item)
    {
        if (item.m_customData is not null &&
            item.m_customData.TryGetValue(SocketMetadataCodec.CustomDataKey, out var value))
            return new MetadataSnapshot(true, value);
        return new MetadataSnapshot(false, string.Empty);
    }

    private static void RestoreMagenheimMetadata(ItemDrop.ItemData item, MetadataSnapshot snapshot)
    {
        item.m_customData ??= new Dictionary<string, string>();
        if (snapshot.HadValue)
            item.m_customData[SocketMetadataCodec.CustomDataKey] = snapshot.Value;
        else
            item.m_customData.Remove(SocketMetadataCodec.CustomDataKey);
    }

    private static void NotifyInventoryChanged(Inventory inventory)
    {
        var changed = AccessTools.Method(typeof(Inventory), "Changed");
        changed?.Invoke(inventory, Array.Empty<object>());
        InventoryGui.instance?.m_playerGrid?.UpdateInventory(inventory, Player.m_localPlayer, null);
    }

    private static string NormalizeCloneName(string name) =>
        name.EndsWith("(Clone)", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - "(Clone)".Length)
            : name;

    private static string CrystalPrefab(Crystal crystal) =>
        $"Magenheim_Crystal_{crystal.Element}_{crystal.Tier}";

    private static string ShardPrefab(ElementalAlignment element) =>
        $"Magenheim_Shard_{element}";

    private readonly struct MetadataSnapshot
    {
        internal MetadataSnapshot(bool hadValue, string value)
        {
            HadValue = hadValue;
            Value = value;
        }

        internal bool HadValue { get; }
        internal string Value { get; }
    }

    private sealed class InventorySnapshot
    {
        private readonly ItemDrop.ItemData[] _items;
        private readonly Dictionary<ItemDrop.ItemData, int> _stacks;

        private InventorySnapshot(ItemDrop.ItemData[] items)
        {
            _items = items;
            _stacks = items.ToDictionary(item => item, item => item.m_stack);
        }

        internal static InventorySnapshot Capture(Inventory inventory) =>
            new(inventory.GetAllItems().ToArray());

        internal void Restore(Inventory inventory)
        {
            var originalSet = new HashSet<ItemDrop.ItemData>(_items);
            foreach (var current in inventory.GetAllItems().ToArray())
            {
                if (!originalSet.Contains(current))
                    inventory.RemoveItem(current);
            }

            foreach (var original in _items)
            {
                if (inventory.ContainsItem(original))
                {
                    original.m_stack = _stacks[original];
                    continue;
                }

                original.m_stack = _stacks[original];
                if (!inventory.AddItem(original))
                    throw new InvalidOperationException(
                        $"Socket transaction rollback could not restore '{DisplayName(original)}'.");
            }
        }
    }
}
