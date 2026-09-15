using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using Magenheim.Core.Socketing;

namespace Magenheim.Runtime.Compatibility;

/// <summary>
/// Optional Jewelcrafting discovery boundary. This assembly deliberately carries no
/// compile-time Jewelcrafting dependency: Magenheim must remain fully functional as a
/// standalone crystal, shaping, architecture, furniture and production mod.
/// </summary>
internal static class JewelcraftingCompatibility
{
    internal const string PluginGuid = "org.bepinex.plugins.jewelcrafting";

    internal static JewelcraftingRuntimeState Detect()
    {
        try
        {
            IDictionary plugins = Chainloader.PluginInfos;
            if (!plugins.Contains(PluginGuid))
                return JewelcraftingRuntimeState.Absent;

            object? pluginInfo = plugins[PluginGuid];
            if (pluginInfo is null)
                return JewelcraftingRuntimeState.DetectedWithoutApi("Jewelcrafting is registered, but its plugin metadata is unavailable.");

            Type? apiType = FindApiType();
            if (apiType is null)
                return JewelcraftingRuntimeState.DetectedWithoutApi("Jewelcrafting is loaded, but its public API type could not be resolved.");

            MethodInfo? isLoaded = apiType.GetMethod("IsLoaded", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (isLoaded is null || isLoaded.ReturnType != typeof(bool))
                return JewelcraftingRuntimeState.DetectedWithoutApi("Jewelcrafting API.IsLoaded() is unavailable or has an incompatible signature.");

            if (!(bool)isLoaded.Invoke(null, null))
                return JewelcraftingRuntimeState.DetectedWithoutApi("Jewelcrafting API is present but reports that Jewelcrafting is not loaded.");

            return JewelcraftingRuntimeState.Available(apiType.Assembly.GetName().Version?.ToString() ?? "unknown");
        }
        catch (Exception ex)
        {
            return JewelcraftingRuntimeState.Failed($"Jewelcrafting discovery failed without mutating either mod: {ex.GetType().Name}: {ex.Message}");
        }
    }

    internal static EquipmentSocketProviderResolution SelectProvider(
        bool integrationEnabled,
        bool preferJewelcrafting,
        bool failClosedOnInteropError)
    {
        JewelcraftingRuntimeState state = Detect();
        return EquipmentSocketProviderRouter.Resolve(new EquipmentSocketProviderContext(
            state.IsDetected,
            state.IsApiAvailable,
            integrationEnabled,
            preferJewelcrafting,
            failClosedOnInteropError));
    }

    internal static Type RequireApiType()
    {
        Type apiType = FindApiType()
            ?? throw new InvalidOperationException("Jewelcrafting public API type is unavailable after provider selection.");
        MethodInfo? isLoaded = apiType.GetMethod("IsLoaded", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
        if (isLoaded is null || isLoaded.ReturnType != typeof(bool) || !(bool)isLoaded.Invoke(null, null))
            throw new InvalidOperationException("Jewelcrafting public API boundary is no longer available after provider selection.");
        return apiType;
    }

    private static Type? FindApiType()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? apiType = assembly.GetType("Jewelcrafting.API", false, false)
                ?? assembly.GetType("Jewelcrafting.API.API", false, false);
            if (apiType is not null)
                return apiType;
        }

        return null;
    }
}

internal sealed record JewelcraftingRuntimeState(
    bool IsDetected,
    bool IsApiAvailable,
    bool DiscoveryFailed,
    string Version,
    string Diagnostic)
{
    internal static JewelcraftingRuntimeState Absent { get; } = new(false, false, false, string.Empty, "Jewelcrafting is not installed.");

    internal static JewelcraftingRuntimeState Available(string version) =>
        new(true, true, false, version, $"Jewelcrafting {version} is available through its public API boundary.");

    internal static JewelcraftingRuntimeState DetectedWithoutApi(string diagnostic) =>
        new(true, false, false, string.Empty, diagnostic);

    internal static JewelcraftingRuntimeState Failed(string diagnostic) =>
        new(true, false, true, string.Empty, diagnostic);
}
