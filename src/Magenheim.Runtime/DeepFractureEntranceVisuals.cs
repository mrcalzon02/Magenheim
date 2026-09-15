using System;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DeepFractureEntranceVisuals
{
    internal const string InteriorAnchorName = "Magenheim_DeepFracture_InteriorAnchor";
    private const string RockMaterialSourcePrefab = "Rock_4";

    internal static void Build(GameObject locationContainer)
    {
        if (locationContainer is null)
            throw new ArgumentNullException(nameof(locationContainer));

        var sourceRock = PrefabManager.Instance.GetPrefab(RockMaterialSourcePrefab)
            ?? throw new InvalidOperationException($"Deep Fracture entrance requires vanilla material source prefab '{RockMaterialSourcePrefab}'.");
        var sourceRenderer = sourceRock.GetComponentsInChildren<Renderer>(includeInactive: true).FirstOrDefault()
            ?? throw new InvalidOperationException($"Deep Fracture entrance material source '{RockMaterialSourcePrefab}' has no renderer.");
        if (sourceRenderer.sharedMaterial is null)
            throw new InvalidOperationException($"Deep Fracture entrance material source '{RockMaterialSourcePrefab}' has no material.");

        var stoneMaterial = new Material(sourceRenderer.sharedMaterial)
        {
            name = "Magenheim_DeepFracture_Stone"
        };
        var shadowMaterial = new Material(sourceRenderer.sharedMaterial)
        {
            name = "Magenheim_DeepFracture_Shadow"
        };
        var crystalMaterial = new Material(sourceRenderer.sharedMaterial)
        {
            name = "Magenheim_DeepFracture_CrystalSeam"
        };

        TintMaterial(shadowMaterial, new Color(0.06f, 0.07f, 0.08f, 1f), emission: null);
        TintMaterial(crystalMaterial, new Color(0.42f, 0.62f, 0.70f, 1f), new Color(0.04f, 0.10f, 0.12f, 1f));

        CreateRock(locationContainer.transform, "Fracture_LeftMass", new Vector3(-2.35f, 1.65f, 0.15f), new Vector3(2.8f, 4.3f, 3.6f), Quaternion.Euler(3f, 14f, -9f), stoneMaterial);
        CreateRock(locationContainer.transform, "Fracture_RightMass", new Vector3(2.35f, 1.7f, 0.1f), new Vector3(2.8f, 4.5f, 3.6f), Quaternion.Euler(-4f, -13f, 8f), stoneMaterial);
        CreateRock(locationContainer.transform, "Fracture_Overhang", new Vector3(0f, 4.1f, 0.05f), new Vector3(4.4f, 1.3f, 3.4f), Quaternion.Euler(-5f, 2f, 1f), stoneMaterial);
        CreateRock(locationContainer.transform, "Fracture_LeftTalus", new Vector3(-3.55f, 0.35f, 0.55f), new Vector3(2.2f, 1.2f, 2.6f), Quaternion.Euler(0f, 24f, 12f), stoneMaterial);
        CreateRock(locationContainer.transform, "Fracture_RightTalus", new Vector3(3.5f, 0.3f, 0.45f), new Vector3(2.1f, 1.1f, 2.5f), Quaternion.Euler(0f, -27f, -10f), stoneMaterial);

        var shadow = CreateRock(
            locationContainer.transform,
            "Fracture_MouthShadow",
            new Vector3(0f, 1.65f, 1.48f),
            new Vector3(2.35f, 3.15f, 0.18f),
            Quaternion.identity,
            shadowMaterial);
        var shadowCollider = shadow.GetComponent<Collider>();
        if (shadowCollider is not null)
            shadowCollider.enabled = false;

        var anchor = new GameObject(InteriorAnchorName);
        anchor.transform.SetParent(locationContainer.transform, worldPositionStays: false);
        anchor.transform.localPosition = new Vector3(0f, 1.15f, -1.2f);
        anchor.transform.localRotation = Quaternion.identity;

        CreateCrystalSeam(locationContainer.transform, "Fracture_CrystalSeam_Left", new Vector3(-1.15f, 2.1f, -1.62f), new Vector3(0.12f, 1.45f, 0.12f), Quaternion.Euler(0f, 0f, -18f), crystalMaterial);
        CreateCrystalSeam(locationContainer.transform, "Fracture_CrystalSeam_Right", new Vector3(1.25f, 2.45f, -1.58f), new Vector3(0.10f, 1.05f, 0.10f), Quaternion.Euler(0f, 0f, 22f), crystalMaterial);
    }

    private static GameObject CreateRock(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = name;
        rock.transform.SetParent(parent, worldPositionStays: false);
        rock.transform.localPosition = position;
        rock.transform.localScale = scale;
        rock.transform.localRotation = rotation;

        var renderer = rock.GetComponent<Renderer>()
            ?? throw new InvalidOperationException($"Generated Deep Fracture entrance object '{name}' has no renderer.");
        renderer.sharedMaterial = material;
        return rock;
    }

    private static void CreateCrystalSeam(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        var seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seam.name = name;
        seam.transform.SetParent(parent, worldPositionStays: false);
        seam.transform.localPosition = position;
        seam.transform.localScale = scale;
        seam.transform.localRotation = rotation;

        var collider = seam.GetComponent<Collider>();
        if (collider is not null)
            collider.enabled = false;

        var renderer = seam.GetComponent<Renderer>()
            ?? throw new InvalidOperationException($"Generated Deep Fracture crystal seam '{name}' has no renderer.");
        renderer.sharedMaterial = material;
    }

    private static void TintMaterial(Material material, Color color, Color? emission)
    {
        if (material.HasProperty("_Color"))
            material.color = color;
        if (emission.HasValue && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
        }
    }
}
