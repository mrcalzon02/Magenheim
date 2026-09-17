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
using UnityEngine.UI;

namespace Magenheim.Runtime;

/// <summary>
/// Explicit per-item socket management surface, shown only while the local player is using the
/// Crystal Enchanting Dais. The Dais is the sole socket station and carries no crafting recipes,
/// so this surface owns the panel instead of competing with a station's recipe list -- hosting it
/// on the Geologist's Workstation displaced that station's normal crafting menu.
///
/// All three socket operations live here: opening a slot, installing a crystal, and removing one.
/// None of them belong to the geode/refinement chain.
///
/// The presentation is hosted by Valheim's InventoryGui and copies
/// the active crafting panel/button/text visuals instead of drawing a separate IMGUI debug window.
/// It never edits shared prefabs: all mutation is performed through Magenheim's namespaced
/// ItemData metadata and pure-core transaction planners. Remote clients submit immutable socket
/// intent to the server and apply only the approved plan after stale-state revalidation.
/// </summary>
internal sealed class CrystalDaisSocketOverlay : MonoBehaviour
{
    private const long LocalPeerId = 0L;
    private const long LocalSessionGeneration = 1L;
    private const float RefreshInterval = .25f;

    private RuntimeServices? _services;
    private DefinitionAuthoritySynchronizer? _authority;
    private ManualLogSource? _log;
    private ItemDrop.ItemData? _selectedEquipment;
    private string _status = "Select equipment to manage its Magenheim sockets.";

    private GameObject? _nativeRoot;
    private ScrollRect? _nativeScroll;
    private RectTransform? _nativeContent;
    private float _nextRefreshAt;
    private bool _refreshRequested = true;

    internal void Configure(
        RuntimeServices services,
        DefinitionAuthoritySynchronizer authority,
        ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _refreshRequested = true;
    }

    private void Update()
    {
        var player = Player.m_localPlayer;
        var shouldShow = _services is not null &&
                         _authority is not null &&
                         player is not null &&
                         InventoryGui.IsVisible() &&
                         TryGetMagenheimStation(player, out _);

        if (!shouldShow)
        {
            _selectedEquipment = null;
            if (_nativeRoot) _nativeRoot.SetActive(false);
            _refreshRequested = true;
            return;
        }

        try
        {
            EnsureNativeUi();
            if (!_nativeRoot || !_nativeContent) return;
            _nativeRoot.SetActive(true);
            _nativeRoot.transform.SetAsLastSibling();

            if (_refreshRequested || Time.unscaledTime >= _nextRefreshAt)
            {
                RefreshNativeUi(player!);
                _nextRefreshAt = Time.unscaledTime + RefreshInterval;
                _refreshRequested = false;
            }
        }
        catch (Exception exception)
        {
            _log?.LogError($"Magenheim native socket workstation UI failed safely: {exception}");
            _status = "Socketing UI failed safely; no intentional item mutation was completed.";
            if (_nativeRoot) _nativeRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_nativeRoot) UnityEngine.Object.Destroy(_nativeRoot);
        _nativeRoot = null;
        _nativeContent = null;
        _nativeScroll = null;
    }

    private void EnsureNativeUi()
    {
        if (_nativeRoot && _nativeContent && _nativeScroll) return;
        var inventoryGui = InventoryGui.instance;
        if (!inventoryGui) return;

        var root = new GameObject("Magenheim.Socketing.NativePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(inventoryGui.transform, false);
        var rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = new Vector2(1f, .5f);
        rootRect.anchorMax = new Vector2(1f, .5f);
        rootRect.pivot = new Vector2(1f, .5f);
        rootRect.anchoredPosition = new Vector2(-24f, 0f);
        rootRect.sizeDelta = new Vector2(520f, 720f);
        CopyNativePanelStyle(root.GetComponent<Image>(), inventoryGui);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(root.transform, false);
        var viewportRect = (RectTransform)viewport.transform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(18f, 18f);
        viewportRect.offsetMax = new Vector2(-18f, -18f);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, .012f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        var content = (RectTransform)contentObject.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 34f;

        _nativeRoot = root;
        _nativeContent = content;
        _nativeScroll = scroll;
        _refreshRequested = true;
    }

    private void RefreshNativeUi(Player player)
    {
        if (_nativeContent is null || _nativeScroll is null || _services is null || _authority is null) return;
        var inventory = player.GetInventory();
        if (_selectedEquipment is not null && !inventory.ContainsItem(_selectedEquipment))
            _selectedEquipment = null;

        var previousScroll = _nativeScroll.verticalNormalizedPosition;
        ClearChildren(_nativeContent);

        AddNativeLabel(_nativeContent, "Magenheim Socketing", 34f, true);
        AddNativeLabel(_nativeContent,
            "Choose an individual weapon, armor piece, shield, tool, or utility item. Socket data remains attached to that exact item.",
            54f);
        AddSectionLabel(_nativeContent, "Equipment");

        var candidates = EligibleOrSocketedEquipment(inventory).ToArray();
        if (candidates.Length == 0)
        {
            AddNativeLabel(_nativeContent, "No socketable equipment in inventory. Weapons, armor, shields, tools and utility items can take crystals.", 30f);
        }
        else
        {
            foreach (var candidate in candidates)
            {
                var stateText = DescribeSocketState(candidate, out var malformed);
                var descriptor = ItemSocketAdapter.Describe(candidate);
                var selected = ReferenceEquals(candidate, _selectedEquipment) ? "● " : string.Empty;
                var label = $"{selected}{DisplayName(candidate)}  [{descriptor.Category}]  {stateText}";
                if (IsRecoveryOnly(candidate)) label += "  [removal only]";
                if (malformed) label += "  [metadata error]";
                var captured = candidate;
                AddNativeButton(_nativeContent, label, () =>
                {
                    _selectedEquipment = captured;
                    _status = malformed
                        ? "Selected item has malformed Magenheim metadata. Mutation is disabled until that metadata is repaired."
                        : $"Selected {DisplayName(captured)}.";
                    _refreshRequested = true;
                });
            }
        }

        if (_selectedEquipment is not null)
            BuildSelectedEquipmentUi(player, inventory, _selectedEquipment);
        else
            AddNativeLabel(_nativeContent, "No equipment selected.", 30f);

        AddSectionLabel(_nativeContent, "Status");
        AddNativeLabel(_nativeContent, _status, 60f);

        Canvas.ForceUpdateCanvases();
        _nativeScroll.verticalNormalizedPosition = Mathf.Clamp01(previousScroll);
    }

    private void BuildSelectedEquipmentUi(Player player, Inventory inventory, ItemDrop.ItemData equipment)
    {
        if (_nativeContent is null || _services is null || _authority is null) return;

        var descriptor = ItemSocketAdapter.Describe(equipment);
        if (!ItemSocketAdapter.TryRead(equipment, out var state, out var metadataError))
        {
            AddNativeLabel(_nativeContent, $"Socket metadata error: {metadataError}", 48f);
            return;
        }

        var eligibility = SocketEligibilityService.Evaluate(descriptor, _services.SocketPolicy);
        AddSectionLabel(_nativeContent, "Selected Equipment");
        AddNativeLabel(_nativeContent, $"{DisplayName(equipment)}  |  {descriptor.ModOrigin}  |  {descriptor.Category}", 34f, true);
        AddNativeLabel(_nativeContent,
            $"Sockets: {state.InstalledCrystals.Count}/{state.UnlockedSlots} unlocked; policy maximum {eligibility.MaximumSlots}.",
            32f);

        if (!eligibility.IsEligible)
            AddNativeLabel(_nativeContent, $"New socket/install operations disabled: {eligibility.Reason}", 44f);

        if (state.InstalledCrystals.Count > 0)
        {
            AddSectionLabel(_nativeContent, "Installed Crystals");
            for (var i = 0; i < state.InstalledCrystals.Count; i++)
            {
                var crystalIndex = i;
                var crystal = state.InstalledCrystals[i];
                AddNativeButton(
                    _nativeContent,
                    $"Extract {i + 1}. {crystal.Tier} {crystal.Element}",
                    () =>
                    {
                        TryExtract(player, inventory, equipment, crystalIndex);
                        _refreshRequested = true;
                    },
                    CanRequestMutation());
            }
        }

        AddNativeButton(_nativeContent, "Open one socket", () =>
        {
            TryAddSlot(player, inventory, equipment);
            _refreshRequested = true;
        }, CanRequestMutation() && eligibility.IsEligible);

        AddSectionLabel(_nativeContent, "Install a Crystal");
        var crystals = SocketableCrystals(inventory).ToArray();
        if (crystals.Length == 0)
        {
            AddNativeLabel(_nativeContent, "No Simple-or-better Magenheim crystals are in inventory.", 34f);
        }
        else
        {
            foreach (var crystalItem in crystals)
            {
                if (!TryParseCrystal(crystalItem, out var crystal)) continue;
                var capturedItem = crystalItem;
                var capturedCrystal = crystal;
                AddNativeButton(
                    _nativeContent,
                    $"Install {crystal.Tier} {crystal.Element}  (x{crystalItem.m_stack})",
                    () =>
                    {
                        TryInstall(player, inventory, equipment, capturedItem, capturedCrystal);
                        _refreshRequested = true;
                    },
                    CanRequestMutation() && eligibility.IsEligible && state.FreeSlots > 0);
            }
        }

        if (IsRemoteMutationClient())
        {
            AddNativeLabel(_nativeContent,
                "Remote socket changes are resolved by the server, then applied only if this exact item still matches the approved original state.",
                54f);
        }
        else if (!IsLocalMutationHost())
        {
            AddNativeLabel(_nativeContent,
                "Socket mutation is unavailable until the server admits this client's synchronized Magenheim gameplay authority.",
                54f);
        }
    }

    private static void ClearChildren(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            UnityEngine.Object.Destroy(child);
        }
    }

    private static void AddSectionLabel(Transform parent, string text) =>
        AddNativeLabel(parent, text, 30f, true);

    private static void AddNativeLabel(Transform parent, string text, float preferredHeight, bool emphasize = false)
    {
        var inventoryGui = InventoryGui.instance;
        if (!inventoryGui) return;

        var template = NativeTextTemplate(inventoryGui);
        GameObject label;
        if (template)
        {
            label = UnityEngine.Object.Instantiate(template, parent, false);
            label.name = "Magenheim.Socketing.Text";
            StripInteractiveComponents(label);
        }
        else
        {
            label = CreateFallbackText(parent);
        }

        label.SetActive(true);
        SetText(label, text);
        var rect = label.GetComponent<RectTransform>() ?? label.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        var layout = label.GetComponent<LayoutElement>() ?? label.AddComponent<LayoutElement>();
        layout.minHeight = preferredHeight;
        layout.preferredHeight = preferredHeight;
        if (emphasize) TrySetFontStyle(label, FontStyle.Bold);
    }

    private static void AddNativeButton(Transform parent, string text, Action action, bool interactable = true)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        var inventoryGui = InventoryGui.instance;
        if (!inventoryGui) return;

        var buttonObject = new GameObject("Magenheim.Socketing.Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.GetComponent<Image>();
        var button = buttonObject.GetComponent<Button>();
        CopyNativeButtonStyle(image, button, inventoryGui);

        var nativeButton = NativeCraftButton(inventoryGui);
        var textTemplate = nativeButton ? FindTextObject(nativeButton.gameObject) : NativeTextTemplate(inventoryGui);
        GameObject label;
        if (textTemplate)
        {
            label = UnityEngine.Object.Instantiate(textTemplate, buttonObject.transform, false);
            StripInteractiveComponents(label);
        }
        else
        {
            label = CreateFallbackText(buttonObject.transform);
        }
        label.name = "Label";
        label.SetActive(true);
        SetText(label, text);
        var labelRect = label.GetComponent<RectTransform>() ?? label.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 5f);
        labelRect.offsetMax = new Vector2(-12f, -5f);
        labelRect.localScale = Vector3.one;

        var layout = buttonObject.GetComponent<LayoutElement>();
        layout.minHeight = 40f;
        layout.preferredHeight = 40f;
        button.interactable = interactable;
        button.onClick.AddListener(() => action());
    }

    private static void CopyNativePanelStyle(Image target, InventoryGui inventoryGui)
    {
        var sourceObject = FieldGameObject(inventoryGui, "m_crafting");
        var source = sourceObject ? sourceObject.GetComponent<Image>() ?? sourceObject.GetComponentInChildren<Image>(true) : null;
        if (!source)
        {
            var button = NativeCraftButton(inventoryGui);
            source = button ? button.GetComponent<Image>() : null;
        }
        if (!source)
        {
            target.color = new Color(.08f, .075f, .065f, .98f);
            return;
        }

        target.sprite = source.sprite;
        target.overrideSprite = source.overrideSprite;
        target.material = source.material;
        target.type = source.type;
        target.preserveAspect = source.preserveAspect;
        target.color = source.color;
    }

    private static void CopyNativeButtonStyle(Image targetImage, Button targetButton, InventoryGui inventoryGui)
    {
        var source = NativeCraftButton(inventoryGui);
        if (!source)
        {
            targetImage.color = new Color(.22f, .18f, .11f, 1f);
            return;
        }

        var sourceImage = source.GetComponent<Image>();
        if (sourceImage)
        {
            targetImage.sprite = sourceImage.sprite;
            targetImage.overrideSprite = sourceImage.overrideSprite;
            targetImage.material = sourceImage.material;
            targetImage.type = sourceImage.type;
            targetImage.preserveAspect = sourceImage.preserveAspect;
            targetImage.color = sourceImage.color;
        }
        targetButton.transition = source.transition;
        targetButton.colors = source.colors;
        targetButton.spriteState = source.spriteState;
        targetButton.navigation = source.navigation;
        targetButton.targetGraphic = targetImage;
    }

    private static Button? NativeCraftButton(InventoryGui inventoryGui)
    {
        var field = AccessTools.Field(typeof(InventoryGui), "m_craftButton");
        return field?.GetValue(inventoryGui) as Button;
    }

    private static GameObject? NativeTextTemplate(InventoryGui inventoryGui) =>
        FieldGameObject(inventoryGui, "m_recipeName") ??
        FieldGameObject(inventoryGui, "m_craftingStationName");

    private static GameObject? FieldGameObject(InventoryGui inventoryGui, string fieldName)
    {
        var field = AccessTools.Field(typeof(InventoryGui), fieldName);
        var value = field?.GetValue(inventoryGui);
        if (value is GameObject gameObject) return gameObject;
        return value is Component component ? component.gameObject : null;
    }

    private static GameObject? FindTextObject(GameObject root)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (!component) continue;
            var property = component.GetType().GetProperty("text");
            if (property is not null && property.CanWrite && property.PropertyType == typeof(string))
                return component.gameObject;
        }
        return null;
    }

    private static void SetText(GameObject root, string text)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (!component) continue;
            var property = component.GetType().GetProperty("text");
            if (property is null || !property.CanWrite || property.PropertyType != typeof(string)) continue;
            property.SetValue(component, text, null);
            return;
        }
    }

    private static void TrySetFontStyle(GameObject root, FontStyle style)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (!component) continue;
            var property = component.GetType().GetProperty("fontStyle");
            if (property is null || !property.CanWrite || property.PropertyType != typeof(FontStyle)) continue;
            property.SetValue(component, style, null);
            return;
        }
    }

    private static void StripInteractiveComponents(GameObject root)
    {
        foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
            UnityEngine.Object.Destroy(selectable);
        foreach (var layout in root.GetComponents<LayoutElement>())
            UnityEngine.Object.Destroy(layout);
    }

    private static GameObject CreateFallbackText(Transform parent)
    {
        var gameObject = new GameObject("Magenheim.Socketing.FallbackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        var text = gameObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = new Color(.92f, .86f, .74f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return gameObject;
    }

    /// <summary>
    /// The equipment the list offers. Items the policy disallows are not shown at all: a list
    /// entry the player cannot act on is noise, and it reads as a bug rather than a rule.
    ///
    /// One narrow exception is deliberate. An item can hold crystals and *then* become
    /// ineligible, because socket policy is configurable and can change between sessions or
    /// between a server and a client. Hiding that item would strand the player's crystals
    /// inside it with no way to reach them, so it is still listed while it has something to
    /// recover, and only for that. Leftover empty metadata is not something to recover, so it
    /// is filtered out with the rest.
    /// </summary>
    private IEnumerable<ItemDrop.ItemData> EligibleOrSocketedEquipment(Inventory inventory)
    {
        if (_services is null) yield break;

        foreach (var item in inventory.GetAllItems())
        {
            var descriptor = ItemSocketAdapter.Describe(item);
            if (SocketEligibilityService.Evaluate(descriptor, _services.SocketPolicy).IsEligible)
            {
                yield return item;
                continue;
            }

            // Disallowed. Offer it only while it still holds a crystal to take back out.
            if (ItemSocketAdapter.TryRead(item, out var state, out _) &&
                state.InstalledCrystals.Count > 0)
                yield return item;
        }
    }

    /// <summary>True when the item is listed only so its installed crystals can be recovered.</summary>
    private bool IsRecoveryOnly(ItemDrop.ItemData item)
    {
        if (_services is null) return false;
        return !SocketEligibilityService.Evaluate(
            ItemSocketAdapter.Describe(item), _services.SocketPolicy).IsEligible;
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
            CrystalEnchantingDaisRegistrar.PrefabName,
            StringComparison.Ordinal);
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
        var raw = item.m_shared.m_name;
        var localized = Localization.instance?.Localize(raw);
        if (!string.IsNullOrWhiteSpace(localized)) return localized!;
        if (!string.IsNullOrWhiteSpace(raw)) return raw;
        return item.m_dropPrefab ? item.m_dropPrefab.name : "Unnamed item";
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
        RuntimeGameApi.NotifyInventoryChanged(inventory);
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
