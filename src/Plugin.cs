using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace GWYF_NewClothing;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.morecosmetics.injector";
    public const string PluginName = "More Cosmetics";
    public const string PluginVersion = "0.2.0";

    internal static Plugin Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;

        try
        {
            PluginConfig.Bind(Config);

            if (!PluginConfig.Enabled.Value) return;

            var selfDir = Path.GetDirectoryName(Info.Location)
                ?? Path.Combine(Paths.PluginPath, "More-Cosmetics");

            // Gather all directories to scan
            var dirs = new HashSet<string> { selfDir };
            if (PluginConfig.AutoDiscover.Value)
            {
                // Scan all plugin subdirectories for cosmetics.json
                var pluginRoot = Paths.PluginPath;
                if (Directory.Exists(pluginRoot))
                {
                    foreach (var sub in Directory.GetDirectories(pluginRoot))
                    {
                        if (File.Exists(Path.Combine(sub, "cosmetics.json")))
                            dirs.Add(sub);
                    }
                }
            }

            Debug.Log($"[MoreCosmetics] Scanning {dirs.Count} directories for cosmetics...");

            foreach (var dir in dirs)
            {
                if (File.Exists(Path.Combine(dir, "cosmetics.json")))
                {
                    CosmeticRegistry.RegisterFrom(dir);
                }
            }

            Harmony.CreateAndPatchAll(typeof(Plugin).Assembly);
            Patches.CosmeticDataManagerPatches.InjectCustomCosmeticsOnce();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"{PluginName} failed: {ex}");
        }
    }
}
