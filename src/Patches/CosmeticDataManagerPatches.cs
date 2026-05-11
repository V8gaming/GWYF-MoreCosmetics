using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GWYF_NewClothing.Patches;

[HarmonyPatch]
internal static class CosmeticDataManagerPatches
{
    private static bool IsEnabled => PluginConfig.Enabled.Value;
    private static bool _injected;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CosmeticDataManager), nameof(CosmeticDataManager.Initialize))]
    private static void Initialize_Postfix()
    {
        if (!IsEnabled) return;
        CosmeticRegistry.ResolvePending();
        InjectCustomCosmeticsOnce();
    }

    internal static void InjectCustomCosmeticsOnce()
    {
        if (_injected) return;

        var cacheField = AccessTools.Field(typeof(CosmeticDataManager), "_cosmeticCache");
        var cache = cacheField.GetValue(null) as Dictionary<int, CosmeticData>;

        if (cache == null)
        {
            Debug.LogWarning("[MoreCosmetics] _cosmeticCache is null. Aborting injection.");
            return;
        }

        var cosmetics = CosmeticRegistry.AllCosmetics;
        if (cosmetics.Length == 0) return;

        int injected = 0;
        foreach (var cosmetic in cosmetics)
        {
            if (cache.ContainsKey(cosmetic.cosmeticId))
                continue;
            cache[cosmetic.cosmeticId] = cosmetic;
            injected++;
        }

        if (injected > 0)
        {
            AccessTools.Method(typeof(CosmeticDataManager), "RebuildSortedCosmeticIds").Invoke(null, null);
            Debug.Log($"[MoreCosmetics] Injected {injected} cosmetics. Cache has {cache.Count} total.");
        }

        _injected = true;
    }
}
