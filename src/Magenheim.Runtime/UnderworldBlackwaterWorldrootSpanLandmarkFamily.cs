using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Monumental Blackwater navigation landmark: an authored drowned Worldroot arch promoted from
/// local ecology scale into a sparse world-scale span. This is the runtime counterpart for loading
/// art that shows enormous roots crossing navigable cavern basins; the image is not allowed to
/// promise free-standing terrain that the game cannot actually produce.
/// </summary>
internal sealed class BlackwaterWorldrootSpanLandmarkFamily : IUnderworldLandmarkStructureFamily
{
    private const int FamilySalt = 0x4B17D20F;
    private const string ModelId = "underworld-flora-blackwater-drowned-root-arch";

    // Deliberately sparse: these are kilometre-navigation silhouettes, not normal vegetation.
    // The larger exclusion radius also prevents ordinary landmark clutter from growing directly
    // through the root feet/opening.
    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(FamilySalt, cellSizeChunks: 18, exclusionRadiusChunks: 3);

    public string Kind => "blackwater-worldroot-span-landmark";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.BlackwaterDeep;
    public int PlacementSlot => 12;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldLandmarkReservationPolicy.IsAnchor(identity, key, ReservationProfile);

    public GameObject Compose(
        Vector3 position,
        UnderworldWorldIdentity identity,
        UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_BlackwaterWorldrootSpan_{key.X}_{key.Z}");
        root.transform.position = position;

        unchecked
        {
            var hash = identity.DerivedSeed32 ^ FamilySalt;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;

            try
            {
                // The source arch is roughly room-scale. Uniform promotion keeps its authored
                // proportions/colliders intact while producing an approximately 80-100 m class
                // silhouette from the same checked-in 7,932-triangle model.
                AddArch(
                    root.transform,
                    "WorldrootSpanPrimary",
                    new Vector3(0f, -1.4f, 0f),
                    Quaternion.Euler(0f, (hash & 31) - 15f, 0f),
                    24f);

                // A smaller crossing root breaks the single-perfect-arch read and supplies the
                // layered silhouette used by the loading-art target without creating a separate
                // procedural mesh system.
                AddArch(
                    root.transform,
                    "WorldrootSpanSecondary",
                    new Vector3(((hash >> 6) & 1) == 0 ? -23f : 23f, -0.8f, 8f),
                    Quaternion.Euler(5f, 32f + ((hash >> 8) & 23), -7f),
                    13f);

                // Two low buttress copies bury the ends into the landscape and make the span read
                // as a grown geological/root mass rather than a prop balanced on two points.
                AddArch(
                    root.transform,
                    "WorldrootButtressA",
                    new Vector3(-34f, -4.5f, -5f),
                    Quaternion.Euler(68f, -18f, 8f),
                    8f);
                AddArch(
                    root.transform,
                    "WorldrootButtressB",
                    new Vector3(34f, -4.5f, 4f),
                    Quaternion.Euler(72f, 198f, -6f),
                    8f);

                return root;
            }
            catch
            {
                Object.Destroy(root);
                throw;
            }
        }
    }

    private static void AddArch(
        Transform parent,
        string name,
        Vector3 localPosition,
        Quaternion localRotation,
        float scale)
    {
        var host = new GameObject(name);
        host.transform.SetParent(parent, false);
        host.transform.localPosition = localPosition;
        host.transform.localRotation = localRotation;
        host.transform.localScale = Vector3.one * scale;

        ModelAssets.Load(host, ModelId, item: false, hideOriginal: false);

        // One cull-only LOD keeps this deliberately low-poly silhouette readable at long range
        // without inventing a second mesh asset or letting it render indefinitely through fog.
        var renderers = host.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new System.InvalidOperationException($"Worldroot landmark model '{ModelId}' has no renderers.");
        var lod = host.AddComponent<LODGroup>();
        lod.SetLODs(new[] { new LOD(.006f, renderers) });
        lod.RecalculateBounds();
    }
}
