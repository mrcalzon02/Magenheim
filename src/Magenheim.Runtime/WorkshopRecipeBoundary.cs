using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Keeps the Geologist's Workstation a specialist Magenheim crafting surface.</summary>
internal sealed class WorkshopRecipeBoundary : MonoBehaviour
{
    private ManualLogSource? _log;
    private float _nextAudit;
    private readonly HashSet<int> _reported = new HashSet<int>();

    internal void Configure(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

    private void Update()
    {
        if (Time.unscaledTime < _nextAudit) return;
        _nextAudit = Time.unscaledTime + 2f;
        var database = ObjectDB.instance;
        if (database == null || database.m_recipes == null) return;
        foreach (var recipe in database.m_recipes)
        {
            if (recipe == null || recipe.m_craftingStation == null || !TargetsStation(recipe.m_craftingStation) || IsMagenheimRecipe(recipe)) continue;
            var id = recipe.GetInstanceID();
            recipe.m_craftingStation = null;
            if (_reported.Add(id)) _log?.LogWarning($"Detached foreign recipe '{recipe.name}' from the Geologist's Workstation specialist recipe boundary.");
        }
    }

    private static bool TargetsStation(CraftingStation station)
    {
        var name = station.gameObject != null ? station.gameObject.name : station.name;
        return string.Equals(Normalize(name), WorkshopRegistrar.StationPrefab, StringComparison.Ordinal);
    }

    private static bool IsMagenheimRecipe(Recipe recipe)
    {
        if (recipe.name.StartsWith("Magenheim_", StringComparison.Ordinal)) return true;
        var item = recipe.m_item;
        return item != null && item.gameObject != null && Normalize(item.gameObject.name).StartsWith("Magenheim_", StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        const string clone = "(Clone)";
        return value.EndsWith(clone, StringComparison.Ordinal) ? value.Substring(0, value.Length - clone.Length) : value;
    }
}
