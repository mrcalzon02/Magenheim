using System;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;
using UnityEngine.UI;

namespace Magenheim.Runtime;

/// <summary>
/// Two tabs, one Valheim Minimap.
///
/// Surface and Underworld own separate vanilla Minimap map-data payloads and terrain textures, but
/// exploration, fog reveal, pins, shared-map behavior, zoom, input and rendering stay on Valheim's
/// existing Minimap implementation. Switching tabs swaps only the data/context currently bound to
/// that one Minimap instance.
/// </summary>
internal sealed class UnderworldMapTabRuntime : MonoBehaviour
{
    private const string CustomDataPrefix = "magenheim.map.underworld.v1.";
    private const float MapGenerationTimeoutSeconds = 120f;

    // AsyncLocal deliberately replaces ThreadStatic here. Map-generation mods commonly move the
    // ordinary GenerateWorldMap sampling work into Task.Run; ExecutionContext propagation carries
    // this immutable Underworld sample context into those workers without globally rerouting the
    // unrelated terrain-generation threads or touching Unity singletons off the main thread.
    private static readonly AsyncLocal<MapGenerationContext?> UnderworldGenerationContext = new();
    private static UnderworldMapTabRuntime? _instance;

    private UnderworldRuntimeServices? _services;
    private ManualLogSource? _log;
    private Minimap? _map;
    private MapLayerState? _surface;
    private MapLayerState? _underworld;
    private UnderworldLayer _boundLayer = UnderworldLayer.Surface;
    private UnderworldLayer _selectedLayer = UnderworldLayer.Surface;
    private bool _wasLargeOpen;
    private bool _loadedPlayerUnderworldData;
    private bool _underworldGenerationPending;
    private float _underworldGenerationDeadline;
    private long? _worldUid;

    private GameObject? _tabRoot;
    private Button? _surfaceButton;
    private Button? _underworldButton;

    internal static bool IsRoutingUnderworldGeneration => UnderworldGenerationContext.Value is not null;

    internal void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _instance = this;
    }

    internal static void NotifyVanillaMapLoaded(Minimap map) => _instance?.OnVanillaMapLoaded(map);

    internal static MapBindingScope? BeginPhysicalMapOperation(Minimap map)
    {
        var runtime = _instance;
        if (runtime is null || runtime._map != map || runtime._surface is null) return null;
        var physical = runtime.ResolvePhysicalLayer();
        if (physical == runtime._boundLayer) return null;
        var restore = runtime._boundLayer;
        if (!runtime.EnsureBound(physical)) return null;
        return new MapBindingScope(runtime, restore);
    }

    internal static MapBindingScope? BeginSurfaceProfileSave(Minimap map)
    {
        var runtime = _instance;
        if (runtime is null || runtime._map != map || runtime._surface is null) return null;
        runtime.CaptureBoundMapData();
        if (runtime._boundLayer == UnderworldLayer.Surface) return null;
        var restore = runtime._boundLayer;
        if (!runtime.EnsureBound(UnderworldLayer.Surface)) return null;
        return new MapBindingScope(runtime, restore);
    }

    internal static void PrepareLocalPlayerSave(Player player)
    {
        var runtime = _instance;
        if (runtime is null || player is null || player != Player.m_localPlayer) return;
        runtime.CaptureBoundMapData();
        runtime.PersistUnderworldMapToPlayer(player);
    }

    internal static MapGenerationScope? BeginMapGeneration(Minimap map)
    {
        var runtime = _instance;
        var services = runtime?._services;
        var identity = services?.InstanceLifecycle.Identity;
        if (runtime is null || runtime._map != map ||
            runtime._boundLayer != UnderworldLayer.Underworld ||
            services is null || identity is null)
            return null;

        var previous = UnderworldGenerationContext.Value;
        var waterLevel = ZoneSystem.instance is null ? 30d : ZoneSystem.instance.m_waterLevel;
        UnderworldGenerationContext.Value = new MapGenerationContext(
            services.TerrainDomain,
            identity.DerivedSeed32,
            waterLevel);
        return new MapGenerationScope(previous);
    }

    internal static void EndMapGeneration(MapGenerationScope? scope)
    {
        if (scope is null) return;
        UnderworldGenerationContext.Value = scope.Previous;
    }

    internal static bool TryRouteMapSample(double x, double z, out UnderworldTerrainResult terrain)
    {
        terrain = default;
        var context = UnderworldGenerationContext.Value;
        if (context is null) return false;

        var noise = UnderworldTerrainNoise.Fractal01(context.Seed, x, z);
        terrain = UnderworldTerrainLifecycle.Evaluate(
            context.Domain,
            new UnderworldTerrainSample(x, 0d, z, context.WaterLevel, 0d, noise),
            context.Seed);
        return true;
    }

    internal static Heightmap.Biome MapDonorBiome(UnderworldTerrainResult terrain)
    {
        if (!terrain.Admitted) return Heightmap.Biome.Ocean;
        return terrain.Biome switch
        {
            UnderworldTerrainBiome.FungalForest => Heightmap.Biome.Mistlands,
            UnderworldTerrainBiome.BlackwaterDeep => terrain.WaterDepth > 0.5d
                ? Heightmap.Biome.Ocean
                : Heightmap.Biome.Swamp,
            UnderworldTerrainBiome.SulfurousWastes => Heightmap.Biome.AshLands,
            UnderworldTerrainBiome.FrozenCaverns => Heightmap.Biome.DeepNorth,
            UnderworldTerrainBiome.FractureZones => Heightmap.Biome.Mountain,
            UnderworldTerrainBiome.GreatDecay => Heightmap.Biome.Swamp,
            _ => Heightmap.Biome.Ocean,
        };
    }

    private void OnVanillaMapLoaded(Minimap map)
    {
        if (!map) return;

        ReleaseUnderworldTextures();
        DestroyTabs();

        _map = map;
        _worldUid = ZNet.instance?.GetWorldUID();
        _surface = MapLayerState.CaptureCurrent(map, RuntimeGameApi.GetMinimapMapData(map), ownsTextures: false);
        _underworld = null;
        _boundLayer = UnderworldLayer.Surface;
        _selectedLayer = ResolvePhysicalLayer();
        _loadedPlayerUnderworldData = false;
        _underworldGenerationPending = false;
        _underworldGenerationDeadline = 0f;
        _wasLargeOpen = Minimap.IsOpen();

        BuildTabs(map);
        _log?.LogInfo("Bound Overworld and Underworld tabs to the single vanilla Minimap runtime.");
    }

    private void Update()
    {
        var map = _map;
        if (!map || _surface is null) return;
        if (ZNet.instance is null)
        {
            SetTabsVisible(false);
            return;
        }

        if (_worldUid != ZNet.instance.GetWorldUID())
        {
            // A new world's own Minimap.LoadMapData postfix will rebuild the state. Until then,
            // never carry layer payloads across a Valheim world boundary.
            SetTabsVisible(false);
            return;
        }

        TryLoadUnderworldMapFromPlayer();

        if (_underworldGenerationPending && !PollPendingUnderworldGeneration())
        {
            var openWhileGenerating = Minimap.IsOpen();
            _wasLargeOpen = openWhileGenerating;
            SetTabsVisible(openWhileGenerating);
            RefreshTabState();
            return;
        }

        var largeOpen = Minimap.IsOpen();
        var physical = ResolvePhysicalLayer();

        if (largeOpen && !_wasLargeOpen)
        {
            _selectedLayer = physical;
            EnsureBound(_selectedLayer);
        }
        else if (!largeOpen)
        {
            _selectedLayer = physical;
            EnsureBound(physical);
        }

        _wasLargeOpen = largeOpen;
        SetTabsVisible(largeOpen);
        RefreshTabState();
    }

    private void LateUpdate()
    {
        var map = _map;
        if (!map || !Minimap.IsOpen()) return;

        // A player marker on the other world's tab is actively misleading. All native marker
        // positioning remains Valheim-owned; this only hides the local marker while browsing the
        // non-physical layer.
        var physical = ResolvePhysicalLayer();
        if (_selectedLayer != physical && map.m_largeMarker)
            map.m_largeMarker.gameObject.SetActive(false);
    }

    private void SelectLayer(UnderworldLayer layer)
    {
        if (_underworldGenerationPending && layer != UnderworldLayer.Underworld) return;
        _selectedLayer = layer;
        if (!EnsureBound(layer))
            _selectedLayer = _boundLayer;
        RefreshTabState();
    }

    private bool EnsureBound(UnderworldLayer layer)
    {
        var map = _map;
        if (!map || _surface is null) return false;
        if (_underworldGenerationPending && layer != UnderworldLayer.Underworld) return false;
        if (layer == _boundLayer) return true;

        if (layer == UnderworldLayer.Underworld && !EnsureUnderworldState(map))
            return false;

        CaptureBoundMapData();
        var state = layer == UnderworldLayer.Surface ? _surface : _underworld;
        if (state is null) return false;
        ApplyState(map, layer, state);
        return true;
    }

    private bool EnsureUnderworldState(Minimap map)
    {
        if (_underworld is not null &&
            _underworld.MapTexture &&
            _underworld.MapTexture.width == map.m_textureSize)
            return true;

        var services = _services;
        var identity = services?.InstanceLifecycle.Identity;
        if (services is null || identity is null ||
            (services.InstanceLifecycle.Phase != UnderworldInstancePhase.Admitting &&
             services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active))
        {
            _log?.LogWarning("Underworld map tab is waiting for active instance terrain authority.");
            return false;
        }

        var previousLayer = _boundLayer;
        CaptureBoundMapData();
        ReleaseUnderworldTextures();

        var state = MapLayerState.CreateOwned(map);
        _underworld = state;
        _boundLayer = UnderworldLayer.Underworld;
        BindTextures(map, state);

        // Build a valid empty payload using Valheim's own map reset + serialization path. No
        // Magenheim fog/exploration codec exists.
        map.Reset();
        RuntimeGameApi.ClearMinimapPins(map);

        // This is the ordinary Minimap.GenerateWorldMap call. A narrow WorldGenerator data-router
        // below supplies Underworld terrain/biome samples only for the duration of this call.
        map.GenerateWorldMap();

        if (!HasGeneratedMapPixels(state))
        {
            _underworldGenerationPending = true;
            _underworldGenerationDeadline = Time.unscaledTime + MapGenerationTimeoutSeconds;
            _log?.LogInfo(
                "Underworld Minimap generation continues asynchronously; retaining the Underworld " +
                "map binding until the vanilla/modded GenerateWorldMap path finishes.");
            return true;
        }

        FinishUnderworldGeneration(state);
        if (previousLayer == UnderworldLayer.Surface && _surface is not null)
            ApplyState(map, UnderworldLayer.Surface, _surface);
        return true;
    }

    private bool PollPendingUnderworldGeneration()
    {
        var map = _map;
        var state = _underworld;
        if (!_underworldGenerationPending || !map || state is null) return true;

        if (HasGeneratedMapPixels(state))
        {
            FinishUnderworldGeneration(state);
            _underworldGenerationPending = false;
            _underworldGenerationDeadline = 0f;
            return true;
        }

        if (Time.unscaledTime <= _underworldGenerationDeadline) return false;

        _log?.LogWarning(
            $"Underworld Minimap generation did not publish texture data within {MapGenerationTimeoutSeconds:0}s; " +
            "discarding the incomplete layer map so the next bind can retry.");
        if (_surface is not null) ApplyState(map, UnderworldLayer.Surface, _surface);
        ReleaseUnderworldTextures();
        _underworldGenerationPending = false;
        _underworldGenerationDeadline = 0f;
        _selectedLayer = ResolvePhysicalLayer();
        return true;
    }

    private void FinishUnderworldGeneration(MapLayerState state)
    {
        var map = _map;
        if (!map) return;

        state.MapData = RuntimeGameApi.GetMinimapMapData(map);
        LoadUnderworldPayloadFromPlayerIfAvailable(state);
        SetMapDataPreservingPublicPosition(map, state.MapData);
        _log?.LogInfo(
            $"Generated Underworld map through vanilla Minimap at {map.m_textureSize}x{map.m_textureSize}; " +
            "fog, pins and exploration remain Valheim-owned.");
    }

    private static bool HasGeneratedMapPixels(MapLayerState state) =>
        TextureHasContent(state.MapTexture) && TextureHasContent(state.HeightTexture);

    private static bool TextureHasContent(Texture2D texture)
    {
        if (!texture || texture.width <= 0 || texture.height <= 0) return false;
        try
        {
            var x0 = texture.width / 2;
            var y0 = texture.height / 2;
            var probes = new[]
            {
                texture.GetPixel(x0, y0),
                texture.GetPixel(texture.width / 4, texture.height / 4),
                texture.GetPixel(texture.width * 3 / 4, texture.height / 4),
                texture.GetPixel(texture.width / 4, texture.height * 3 / 4),
                texture.GetPixel(texture.width * 3 / 4, texture.height * 3 / 4),
            };
            foreach (var color in probes)
                if (color.a > 0.001f || color.r > 0.001f || color.g > 0.001f || color.b > 0.001f)
                    return true;
            return false;
        }
        catch (UnityException)
        {
            // A mod is allowed to replace a map texture with a non-readable GPU texture. If it has
            // done so, generation has taken ownership; do not deadlock the layer selector.
            return true;
        }
    }

    private void CaptureBoundMapData()
    {
        var map = _map;
        if (!map) return;
        var state = _boundLayer == UnderworldLayer.Surface ? _surface : _underworld;
        if (state is null) return;

        state.MapData = RuntimeGameApi.GetMinimapMapData(map);
        state.CaptureTextureReferences(map);

        if (_boundLayer == UnderworldLayer.Underworld && Player.m_localPlayer is { } player)
            PersistUnderworldMapToPlayer(player);
    }

    private void ApplyState(Minimap map, UnderworldLayer layer, MapLayerState state)
    {
        BindTextures(map, state);
        SetMapDataPreservingPublicPosition(map, state.MapData);
        _boundLayer = layer;
    }

    private static void BindTextures(Minimap map, MapLayerState state)
    {
        map.m_mapTexture = state.MapTexture;
        map.m_forestMaskTexture = state.ForestMaskTexture;
        map.m_heightTexture = state.HeightTexture;
        map.m_fogTexture = state.FogTexture;

        map.m_mapImageLarge.material.SetTexture("_MainTex", state.MapTexture);
        map.m_mapImageLarge.material.SetTexture("_MaskTex", state.ForestMaskTexture);
        map.m_mapImageLarge.material.SetTexture("_HeightTex", state.HeightTexture);
        map.m_mapImageLarge.material.SetTexture("_FogTex", state.FogTexture);
        map.m_mapImageSmall.material.SetTexture("_MainTex", state.MapTexture);
        map.m_mapImageSmall.material.SetTexture("_MaskTex", state.ForestMaskTexture);
        map.m_mapImageSmall.material.SetTexture("_HeightTex", state.HeightTexture);
        map.m_mapImageSmall.material.SetTexture("_FogTex", state.FogTexture);
    }

    private static void SetMapDataPreservingPublicPosition(Minimap map, byte[] data)
    {
        var net = ZNet.instance;
        var publicPosition = net?.IsReferencePositionPublic() ?? false;
        RuntimeGameApi.SetMinimapMapData(map, data);
        UnderworldMinimapGameApi.ResetDynamicPinCaches(map);
        if (net is not null) net.SetPublicReferencePosition(publicPosition);
    }

    internal static bool AllowDynamicPinUpdate(Minimap map)
    {
        var runtime = _instance;
        if (runtime is null || runtime._map != map) return true;
        return runtime._boundLayer == runtime.ResolvePhysicalLayer();
    }

    private UnderworldLayer ResolvePhysicalLayer()
    {
        var services = _services;
        if (services is not null &&
            services.TryResolveLocalSession(out _, out var layer, out _, out _) &&
            layer == UnderworldLayer.Underworld)
            return UnderworldLayer.Underworld;
        return UnderworldLayer.Surface;
    }

    private void TryLoadUnderworldMapFromPlayer()
    {
        if (_loadedPlayerUnderworldData || _underworld is null) return;
        LoadUnderworldPayloadFromPlayerIfAvailable(_underworld);
    }

    private void LoadUnderworldPayloadFromPlayerIfAvailable(MapLayerState state)
    {
        if (_loadedPlayerUnderworldData) return;
        var player = Player.m_localPlayer;
        if (player is null || ZNet.instance is null) return;
        _loadedPlayerUnderworldData = true;

        var key = MapDataKey(ZNet.instance.GetWorldUID());
        if (!player.m_customData.TryGetValue(key, out var encoded) || string.IsNullOrWhiteSpace(encoded))
            return;

        try
        {
            state.MapData = Convert.FromBase64String(encoded);
            if (_boundLayer == UnderworldLayer.Underworld && _map)
                SetMapDataPreservingPublicPosition(_map!, state.MapData);
            _log?.LogInfo("Loaded Underworld exploration/pins through Valheim's native minimap payload codec.");
        }
        catch (FormatException exception)
        {
            _log?.LogWarning($"Ignoring invalid Underworld map payload '{key}': {exception.Message}");
        }
    }

    private void PersistUnderworldMapToPlayer(Player player)
    {
        if (_underworld is null || ZNet.instance is null || _underworld.MapData.Length == 0) return;
        player.m_customData[MapDataKey(ZNet.instance.GetWorldUID())] =
            Convert.ToBase64String(_underworld.MapData);
    }

    private static string MapDataKey(long worldUid) => CustomDataPrefix + worldUid.ToString("X16");

    private void BuildTabs(Minimap map)
    {
        if (!map.m_largeRoot) return;

        var root = new GameObject("Magenheim_MapLayerTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        root.transform.SetParent(map.m_largeRoot.transform, false);
        root.transform.SetAsLastSibling();
        var rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -14f);
        rect.sizeDelta = new Vector2(326f, 36f);

        var layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        _surfaceButton = CreateTabButton(root.transform, "Overworld", () => SelectLayer(UnderworldLayer.Surface));
        _underworldButton = CreateTabButton(root.transform, "Underworld", () => SelectLayer(UnderworldLayer.Underworld));
        _tabRoot = root;
        RefreshTabState();
    }

    private static Button CreateTabButton(Transform parent, string label, Action action)
    {
        var buttonObject = new GameObject(
            "Magenheim_MapTab_" + label,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = (RectTransform)buttonObject.transform;
        rect.sizeDelta = new Vector2(160f, 36f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.82f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 18;
        text.font = ResolveUiFont(parent);

        return button;
    }

    private static Font ResolveUiFont(Transform root)
    {
        var existing = root.GetComponentInChildren<Text>(true);
        if (existing && existing.font) return existing.font;
        var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (legacy) return legacy;
        throw new InvalidOperationException("Valheim map UI exposes no usable font for Magenheim layer tabs.");
    }

    private void SetTabsVisible(bool visible)
    {
        if (_tabRoot && _tabRoot.activeSelf != visible) _tabRoot.SetActive(visible);
    }

    private void RefreshTabState()
    {
        if (_surfaceButton)
            _surfaceButton.interactable = !_underworldGenerationPending &&
                _selectedLayer != UnderworldLayer.Surface;
        if (_underworldButton)
            _underworldButton.interactable = _selectedLayer != UnderworldLayer.Underworld;
    }

    private void ReleaseUnderworldTextures()
    {
        if (_underworld is not null) _underworld.DestroyOwnedTextures();
        _underworld = null;
    }

    private void DestroyTabs()
    {
        if (_tabRoot) Destroy(_tabRoot);
        _tabRoot = null;
        _surfaceButton = null;
        _underworldButton = null;
    }

    private void OnDestroy()
    {
        _underworldGenerationPending = false;
        CaptureBoundMapData();
        if (Player.m_localPlayer is { } player) PersistUnderworldMapToPlayer(player);
        ReleaseUnderworldTextures();
        DestroyTabs();
        if (ReferenceEquals(_instance, this)) _instance = null;
    }

    internal sealed class MapGenerationContext
    {
        internal MapGenerationContext(UnderworldInstanceTerrainDomain domain, int seed, double waterLevel)
        {
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
            Seed = seed;
            WaterLevel = waterLevel;
        }

        internal UnderworldInstanceTerrainDomain Domain { get; }
        internal int Seed { get; }
        internal double WaterLevel { get; }
    }

    internal sealed class MapGenerationScope
    {
        internal MapGenerationScope(MapGenerationContext? previous) => Previous = previous;
        internal MapGenerationContext? Previous { get; }
    }

    internal sealed class MapBindingScope : IDisposable
    {
        private UnderworldMapTabRuntime? _runtime;
        private readonly UnderworldLayer _restore;

        internal MapBindingScope(UnderworldMapTabRuntime runtime, UnderworldLayer restore)
        {
            _runtime = runtime;
            _restore = restore;
        }

        public void Dispose()
        {
            var runtime = _runtime;
            _runtime = null;
            runtime?.EnsureBound(_restore);
        }
    }

    private sealed class MapLayerState
    {
        internal Texture2D MapTexture = null!;
        internal Texture2D ForestMaskTexture = null!;
        internal Texture2D HeightTexture = null!;
        internal Texture2D FogTexture = null!;
        internal byte[] MapData = Array.Empty<byte>();
        internal bool OwnsTextures;

        internal static MapLayerState CaptureCurrent(Minimap map, byte[] data, bool ownsTextures) => new()
        {
            MapTexture = map.m_mapTexture,
            ForestMaskTexture = map.m_forestMaskTexture,
            HeightTexture = map.m_heightTexture,
            FogTexture = map.m_fogTexture,
            MapData = data,
            OwnsTextures = ownsTextures,
        };

        internal static MapLayerState CreateOwned(Minimap map)
        {
            var state = new MapLayerState
            {
                MapTexture = CloneTextureShape(map.m_mapTexture, map.m_textureSize),
                ForestMaskTexture = CloneTextureShape(map.m_forestMaskTexture, map.m_textureSize),
                HeightTexture = CloneTextureShape(map.m_heightTexture, map.m_textureSize),
                FogTexture = CloneTextureShape(map.m_fogTexture, map.m_textureSize),
                OwnsTextures = true,
            };
            return state;
        }

        internal void CaptureTextureReferences(Minimap map)
        {
            MapTexture = map.m_mapTexture;
            ForestMaskTexture = map.m_forestMaskTexture;
            HeightTexture = map.m_heightTexture;
            FogTexture = map.m_fogTexture;
        }

        internal void DestroyOwnedTextures()
        {
            if (!OwnsTextures) return;
            if (MapTexture) UnityEngine.Object.Destroy(MapTexture);
            if (ForestMaskTexture) UnityEngine.Object.Destroy(ForestMaskTexture);
            if (HeightTexture) UnityEngine.Object.Destroy(HeightTexture);
            if (FogTexture) UnityEngine.Object.Destroy(FogTexture);
            OwnsTextures = false;
        }

        private static Texture2D CloneTextureShape(Texture2D source, int size)
        {
            var texture = new Texture2D(size, size, source.format, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = source.filterMode,
                anisoLevel = source.anisoLevel,
            };
            return texture;
        }
    }
}

[HarmonyPatch(typeof(Minimap), "LoadMapData")]
internal static class UnderworldMinimapLoadMapDataPatch
{
    private static void Postfix(Minimap __instance) =>
        UnderworldMapTabRuntime.NotifyVanillaMapLoaded(__instance);
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.SaveMapData))]
internal static class UnderworldMinimapSaveMapDataPatch
{
    private static void Prefix(Minimap __instance, out UnderworldMapTabRuntime.MapBindingScope? __state) =>
        __state = UnderworldMapTabRuntime.BeginSurfaceProfileSave(__instance);

    private static void Postfix(UnderworldMapTabRuntime.MapBindingScope? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.SavePlayerData), new[] { typeof(Player) })]
internal static class UnderworldMinimapPlayerSavePatch
{
    private static void Prefix(Player __0) => UnderworldMapTabRuntime.PrepareLocalPlayerSave(__0);
}

[HarmonyPatch(typeof(Minimap), "UpdateExplore", new[] { typeof(float), typeof(Player) })]
internal static class UnderworldMinimapUpdateExplorePatch
{
    private static void Prefix(Minimap __instance, out UnderworldMapTabRuntime.MapBindingScope? __state) =>
        __state = UnderworldMapTabRuntime.BeginPhysicalMapOperation(__instance);

    private static void Postfix(UnderworldMapTabRuntime.MapBindingScope? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(Minimap), "Explore", new[] { typeof(Vector3), typeof(float) })]
internal static class UnderworldMinimapExplorePatch
{
    private static void Prefix(Minimap __instance, out UnderworldMapTabRuntime.MapBindingScope? __state) =>
        __state = UnderworldMapTabRuntime.BeginPhysicalMapOperation(__instance);

    private static void Postfix(UnderworldMapTabRuntime.MapBindingScope? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.GetSharedMapData), new[] { typeof(byte[]) })]
internal static class UnderworldMinimapGetSharedMapDataPatch
{
    private static void Prefix(Minimap __instance, out UnderworldMapTabRuntime.MapBindingScope? __state) =>
        __state = UnderworldMapTabRuntime.BeginPhysicalMapOperation(__instance);

    private static void Postfix(UnderworldMapTabRuntime.MapBindingScope? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.AddSharedMapData), new[] { typeof(byte[]) })]
internal static class UnderworldMinimapAddSharedMapDataPatch
{
    private static void Prefix(Minimap __instance, out UnderworldMapTabRuntime.MapBindingScope? __state) =>
        __state = UnderworldMapTabRuntime.BeginPhysicalMapOperation(__instance);

    private static void Postfix(UnderworldMapTabRuntime.MapBindingScope? __state) => __state?.Dispose();
}

[HarmonyPatch(
    typeof(Minimap),
    nameof(Minimap.DiscoverLocation),
    new[] { typeof(Vector3), typeof(Minimap.PinType), typeof(string), typeof(bool) })]
internal static class UnderworldMinimapDiscoverLocationPatch
{
    private static void Prefix(Minimap __instance, out UnderworldMapTabRuntime.MapBindingScope? __state) =>
        __state = UnderworldMapTabRuntime.BeginPhysicalMapOperation(__instance);

    private static void Postfix(UnderworldMapTabRuntime.MapBindingScope? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(Minimap), "UpdateDynamicPins", new[] { typeof(float) })]
internal static class UnderworldMinimapDynamicPinsPatch
{
    private static bool Prefix(Minimap __instance) =>
        UnderworldMapTabRuntime.AllowDynamicPinUpdate(__instance);
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.GenerateWorldMap))]
internal static class UnderworldMinimapGenerateWorldMapPatch
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Minimap __instance, out UnderworldMapTabRuntime.MapGenerationScope? __state) =>
        __state = UnderworldMapTabRuntime.BeginMapGeneration(__instance);

    private static Exception? Finalizer(
        Exception? __exception,
        UnderworldMapTabRuntime.MapGenerationScope? __state)
    {
        UnderworldMapTabRuntime.EndMapGeneration(__state);
        return __exception;
    }
}

[HarmonyPatch(
    typeof(WorldGenerator),
    nameof(WorldGenerator.GetBiome),
    new[] { typeof(float), typeof(float), typeof(float), typeof(bool) })]
internal static class UnderworldMinimapWorldGeneratorBiomePatch
{
    private static bool Prefix(float __0, float __1, ref Heightmap.Biome __result)
    {
        if (!UnderworldMapTabRuntime.TryRouteMapSample(__0, __1, out var terrain)) return true;
        __result = UnderworldMapTabRuntime.MapDonorBiome(terrain);
        return false;
    }
}

[HarmonyPatch(
    typeof(WorldGenerator),
    nameof(WorldGenerator.GetBiomeHeight),
    new[]
    {
        typeof(Heightmap.Biome),
        typeof(float),
        typeof(float),
        typeof(Color),
        typeof(bool),
        typeof(bool),
    },
    new[]
    {
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Out,
        ArgumentType.Normal,
        ArgumentType.Normal,
    })]
internal static class UnderworldMinimapWorldGeneratorHeightPatch
{
    private static bool Prefix(float __1, float __2, ref Color __3, ref float __result)
    {
        if (!UnderworldMapTabRuntime.TryRouteMapSample(__1, __2, out var terrain)) return true;

        __3 = Color.clear;
        if (!terrain.Admitted)
        {
            var waterLevel = ZoneSystem.instance is null ? 30f : ZoneSystem.instance.m_waterLevel;
            __result = waterLevel - 200f;
            return false;
        }

        __result = (float)terrain.Height;
        return false;
    }
}
