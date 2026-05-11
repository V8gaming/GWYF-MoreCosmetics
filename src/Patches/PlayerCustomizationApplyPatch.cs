using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GWYF_NewClothing.Patches;

[HarmonyPatch(typeof(PlayerCustomization), "ApplyCosmetic")]
internal static class PlayerCustomizationApplyPatch
{
    private static bool IsEnabled => PluginConfig.Enabled.Value;

    [HarmonyPostfix]
    private static void Postfix(PlayerCustomization __instance, int cosmeticId)
    {
        if (!IsEnabled) return;

        var cd = CosmeticDataManager.GetCosmeticById(cosmeticId);
        if (cd == null || cd.cosmeticMaterial == null) return;

        // Apply our material to the MeshRenderer
        var mf = GetMeshFilterForType(__instance, cd.cosmeticType);
        if (mf == null) return;

        var mr = mf.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = cd.cosmeticMaterial;
        }
    }

    private static MeshFilter GetMeshFilterForType(PlayerCustomization instance, CosmeticType type)
    {
        // Same logic as the game's GetMeshFilterForType
        var fieldName = type switch
        {
            CosmeticType.Hat => "hat",
            CosmeticType.Hair => "hair",
            CosmeticType.Mustache => "mustache",
            CosmeticType.Beard => "beard",
            CosmeticType.Neckwear => "neckwear",
            CosmeticType.Clothing => "clothing",
            CosmeticType.Facewear => "facewear",
            _ => null
        };

        if (fieldName == null) return null!;

        var field = typeof(PlayerCustomization).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(instance) as MeshFilter;
    }
}
