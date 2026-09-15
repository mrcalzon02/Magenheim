using System;
using System.Reflection;
using BepInEx.Logging;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime.Compatibility;

/// <summary>
/// Additively exposes the authoritative Magenheim crystal prefabs to Jewelcrafting.
/// No duplicate crystal items are created: architecture, production, rituals and
/// Crystal Shaping continue to consume the same Magenheim prefab identities.
/// </summary>
internal sealed class JewelcraftingCrystalRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal JewelcraftingCrystalRegistrar(ManualLogSource log)
        => _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered)
            return;

        PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        _subscribed = true;
    }

    private void OnVanillaPrefabsAvailable()
    {
        if (_registered)
            return;

        try
        {
            Type apiType = JewelcraftingCompatibility.RequireApiType();
            MethodInfo addGem = apiType.GetMethod(
                "AddGem",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(GameObject), typeof(string) },
                null)
                ?? throw new MissingMethodException(apiType.FullName, "AddGem(GameObject, string)");

            var count = 0;
            foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
            {
                var gemFamily = $"Magenheim {element}";
                foreach (CrystalTier tier in Enum.GetValues(typeof(CrystalTier)))
                {
                    var prefabName = $"Magenheim_Crystal_{element}_{tier}";
                    var prefab = PrefabManager.Instance.GetPrefab(prefabName)
                        ?? throw new InvalidOperationException(
                            $"Cannot register Magenheim crystal '{prefabName}' with Jewelcrafting because its authoritative prefab is unavailable.");

                    addGem.Invoke(null, new object[] { prefab, gemFamily });
                    count++;
                }
            }

            _registered = true;
            _log.LogInfo($"Registered {count} authoritative Magenheim crystal prefabs with Jewelcrafting without creating duplicate crystal items.");
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Jewelcrafting rejected Magenheim crystal registration: {exception.InnerException.Message}",
                exception.InnerException);
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!_subscribed)
            return;
        PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        _subscribed = false;
    }
}
