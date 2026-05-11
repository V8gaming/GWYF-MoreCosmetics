using BepInEx.Configuration;

namespace GWYF_NewClothing;

internal static class PluginConfig
{
    public static ConfigEntry<bool> Enabled { get; private set; } = null!;
    public static ConfigEntry<bool> AutoDiscover { get; private set; } = null!;
    public static ConfigEntry<bool> AutoUnlockModCosmetics { get; private set; } = null!;
    public static ConfigEntry<int> CosmeticIdStart { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        Enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Globally enables or disables the library and all loaded cosmetics.");

        AutoDiscover = config.Bind(
            "General",
            "AutoDiscover",
            true,
            "When true, automatically scans all plugin folders for cosmetics.json files.");

        AutoUnlockModCosmetics = config.Bind(
            "General",
            "AutoUnlockModCosmetics",
            true,
            "When true, all loaded cosmetics are automatically unlocked on game start.");

        CosmeticIdStart = config.Bind(
            "General",
            "CosmeticIdStart",
            10000,
            "Starting ID for auto-assigned cosmetic IDs.");
    }
}
