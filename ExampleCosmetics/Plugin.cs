// Example cosmetic pack plugin.
// The More Cosmetics library auto-discovers the cosmetics.json in this plugin's folder.
// No code needed — this DLL just exists so BepInEx loads the folder and its assets.

using BepInEx;

namespace ExampleCosmetics;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("com.morecosmetics.injector", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.example.cosmetics";
    public const string PluginName = "Example Cosmetics";
    public const string PluginVersion = "0.1.0";

    private void Awake()
    {
        Logger.LogInfo($"{PluginName} loaded.");
    }
}
