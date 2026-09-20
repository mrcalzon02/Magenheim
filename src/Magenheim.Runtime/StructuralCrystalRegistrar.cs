using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Magenheim.Runtime;

/// <summary>
/// Owns the unaligned bulk building crystal.
///
/// Every Magenheim crystal carries an elemental alignment and a refinement tier, which is
/// precisely what makes one worth socketing into gear. A beam, a foundation or a blade wants
/// neither: it wants mass. Fusing four shards destroys the alignment -- the same trade the
/// grinding chain already makes for Crystal Dust -- and returns one structural block, which is
/// what every Magenheim recipe that used to cost vanilla Crystal is built from.
///
/// The item is registered here rather than with the fusing recipes because Jotunn resolves a
/// PieceConfig requirement the moment the CustomPiece is constructed, so this must run before
/// every registrar that costs it. The recipes that produce it need the Geologist's Workstation,
/// which is registered later still, so they live in ShardRecipeRegistrar with the other
/// deterministic conversions.
/// </summary>
internal sealed class StructuralCrystalRegistrar : IDisposable
{
    internal const string PrefabName = "Magenheim_StructuralCrystal";

    private const string PlaceholderBasePrefab = "Stone";
    private const string ModelAsset = "structural";
    private const int StackSize = 50;
    private const float Weight = .5f;

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal StructuralCrystalRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterItem;
        _subscribed = true;
    }

    private void RegisterItem()
    {
        if (_registered) return;
        try
        {
            if (PrefabManager.Instance.GetPrefab(PrefabName) || CustomItem.IsCustomItem(PrefabName))
                throw new InvalidOperationException($"Cannot replace occupied structural crystal identity '{PrefabName}'.");

            var item = new CustomItem(PrefabName, PlaceholderBasePrefab);
            var shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_name = "Structural Crystal";
            shared.m_description =
                "A dense, unaligned block fused from elemental crystal shards. The fusing destroys every " +
                "trace of alignment, so it holds no charge and will not sit in a socket -- what it holds is " +
                "load. This is the crystal Magenheim builds and forges with.";
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = StackSize;
            shared.m_weight = Weight;
            shared.m_value = 0;
            shared.m_dlc = string.Empty;
            shared.m_icons = new[] { EarthAssets.Icon(ModelAsset) };
            EarthAssets.ReplaceVisual(item.ItemPrefab, ModelAsset);

            if (!ItemManager.Instance.AddItem(item))
                throw new InvalidOperationException($"Jotunn refused structural crystal item '{PrefabName}'.");

            _registered = true;
            _log.LogInfo($"Registered the unaligned structural building crystal '{PrefabName}'.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Structural crystal registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterItem;
        _subscribed = false;
    }
}
