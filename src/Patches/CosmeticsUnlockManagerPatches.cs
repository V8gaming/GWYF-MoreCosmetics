using HarmonyLib;
using UnityEngine;

namespace GWYF_NewClothing.Patches;

[HarmonyPatch]
internal static class CosmeticsUnlockManagerPatches
{
    private static bool IsEnabled => PluginConfig.Enabled.Value;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CosmeticsUnlockManager), "OnAwake")]
    private static void OnAwake_Postfix(CosmeticsUnlockManager __instance)
    {
        if (!IsEnabled) return;
        if (!PluginConfig.AutoUnlockModCosmetics.Value) return;

        foreach (var cosmetic in CosmeticRegistry.AllCosmetics)
        {
            if (__instance.UnlockCosmetic(cosmetic.cosmeticId))
            {
                Debug.Log($"[MoreCosmetics] Unlocked: {cosmetic.cosmeticName} (ID:{cosmetic.cosmeticId})");
            }
        }
    }
}
