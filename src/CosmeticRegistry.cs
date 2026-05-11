using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace GWYF_NewClothing;

/// <summary>
/// Public API for registering custom cosmetics. Called automatically by the library
/// for any plugin folder containing a cosmetics.json, or manually by other plugins.
/// </summary>
public static class CosmeticRegistry
{
    private static CosmeticData[] _allCosmetics = Array.Empty<CosmeticData>();
    private static readonly List<CosmeticEntry> _pendingVanilla = new();
    private static int _nextId;
    private static readonly List<string> _registeredDirs = new();

    public static CosmeticData[] AllCosmetics
    {
        get
        {
            ResolvePending();
            return _allCosmetics;
        }
    }

    /// <summary>
    /// Register cosmetics from a plugin directory. Looks for cosmetics.json + models/ + textures/ + bundles/.
    /// Safe to call multiple times from different plugins.
    /// </summary>
    public static void RegisterFrom(string pluginDir)
    {
        if (string.IsNullOrEmpty(pluginDir)) return;
        if (_registeredDirs.Contains(pluginDir)) return;
        _registeredDirs.Add(pluginDir);

        var jsonPath = Path.Combine(pluginDir, "cosmetics.json");
        if (!File.Exists(jsonPath))
        {
            Debug.Log($"[MoreCosmetics] No cosmetics.json in {pluginDir}, skipping.");
            return;
        }

        try
        {
            var json = File.ReadAllText(jsonPath);
            var entries = ParseFromJson(json);

            var modelsDir = Path.Combine(pluginDir, "models");
            var texturesDir = Path.Combine(pluginDir, "textures");
            var bundlesDir = Path.Combine(pluginDir, "bundles");

            // Ensure sub-folders exist
            try { Directory.CreateDirectory(modelsDir); } catch { }
            try { Directory.CreateDirectory(texturesDir); } catch { }
            try { Directory.CreateDirectory(bundlesDir); } catch { }

            var newCosmetics = new List<CosmeticData>();

            foreach (var entry in entries)
            {
                var cosmetic = CreateCosmetic(entry, newCosmetics.Count, modelsDir, texturesDir, bundlesDir);
                if (cosmetic != null)
                    newCosmetics.Add(cosmetic);
                else if (entry?.model?.IsVanilla == true)
                    _pendingVanilla.Add(entry); // Defer until cache ready
            }

            if (_allCosmetics.Length == 0)
            {
                _allCosmetics = newCosmetics.ToArray();
            }
            else
            {
                var merged = new List<CosmeticData>(_allCosmetics);
                merged.AddRange(newCosmetics);
                _allCosmetics = merged.ToArray();
            }

            Debug.Log($"[MoreCosmetics] Registered {newCosmetics.Count} cosmetics from {Path.GetFileName(pluginDir)}");
            if (_pendingVanilla.Count > 0)
                Debug.Log($"[MoreCosmetics]   {_pendingVanilla.Count} vanilla-model entries deferred (not in cache yet)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MoreCosmetics] Failed to register cosmetics from {pluginDir}: {ex.Message}");
        }
    }

    internal static void ResolvePending()
    {
        if (_pendingVanilla.Count == 0) return;
        if (!IsCacheReady()) return;

        var resolved = new List<CosmeticData>();
        var stillPending = new List<CosmeticEntry>();

        foreach (var entry in _pendingVanilla)
        {
            var cosmetic = CreateCosmetic(entry, resolved.Count, _lastModelsDir, _lastTexDir, _lastBundlesDir);
            if (cosmetic != null)
                resolved.Add(cosmetic);
            else
                stillPending.Add(entry);
        }

        if (resolved.Count > 0)
        {
            var merged = new List<CosmeticData>(_allCosmetics);
            merged.AddRange(resolved);
            _allCosmetics = merged.ToArray();
        }

        _pendingVanilla.Clear();
        _pendingVanilla.AddRange(stillPending);
    }

    private static string _lastModelsDir = "";
    private static string _lastTexDir = "";
    private static string _lastBundlesDir = "";

    private static bool IsCacheReady()
    {
        try
        {
            var f = typeof(CosmeticDataManager).GetField("_isInitialized",
                BindingFlags.NonPublic | BindingFlags.Static);
            return f?.GetValue(null) is true;
        }
        catch { return false; }
    }

    private static CosmeticData? CreateCosmetic(CosmeticEntry entry, int index, string modelsDir, string texturesDir, string bundlesDir)
    {
        _lastModelsDir = modelsDir;
        _lastTexDir = texturesDir;
        _lastBundlesDir = bundlesDir;

        if (string.IsNullOrEmpty(entry.name) || string.IsNullOrEmpty(entry.type))
            return null;

        if (!Enum.TryParse<CosmeticType>(entry.type, true, out var cosmeticType))
            return null;

        var rarity = CosmeticRarity.Common;
        if (!string.IsNullOrEmpty(entry.rarity))
            Enum.TryParse(entry.rarity, true, out rarity);

        if (entry.model == null) return null;

        var model = ResolveModel(entry, modelsDir, bundlesDir);
        if (model == null) return null;

        var shader = Shader.Find(!string.IsNullOrEmpty(entry.shader) ? entry.shader : "01_GWYF/GlobalShader")
                     ?? Shader.Find("Standard")
                     ?? Shader.Find("Diffuse")!;

        var material = new Material(shader);
        material.name = entry.name + "_Mat";

        if (!string.IsNullOrEmpty(entry.texture))
        {
            var tex = TextureLoader.Load(Path.Combine(texturesDir, entry.texture));
            if (tex != null)
            {
                material.SetTexture("_MainTex", tex);
                material.SetTexture("_BaseMap", tex);
                material.color = Color.white;
            }
            else
            {
                material.color = GetTintColor(entry.tint);
            }
        }
        else
        {
            material.color = GetTintColor(entry.tint);
        }

        var cosmetic = ScriptableObject.CreateInstance<CosmeticData>();
        cosmetic.name = entry.name;
        cosmetic.cosmeticId = PluginConfig.CosmeticIdStart.Value + _nextId++;
        cosmetic.cosmeticName = entry.name;
        cosmetic.description = entry.description ?? "";
        cosmetic.cosmeticType = cosmeticType;
        cosmetic.rarity = rarity;
        cosmetic.cosmeticModel = model;
        cosmetic.cosmeticMaterial = material;

        return cosmetic;
    }

    private static GameObject? ResolveModel(CosmeticEntry entry, string modelsDir, string bundlesDir)
    {
        if (entry.model.IsVanilla)
            return CloneVanillaModel(entry.model.vanilla);

        if (entry.model.IsObj)
        {
            var path = Path.Combine(modelsDir, entry.model.obj);
            var mesh = ObjImporter.Import(path);
            if (mesh == null) return null;

            var go = new GameObject(entry.name + "_Model");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            return go;
        }

        if (entry.model.IsBundle)
        {
            var path = Path.Combine(bundlesDir, entry.model.bundle);
            return AssetBundleLoader.LoadModel(path, entry.model.asset);
        }

        return null;
    }

    private static GameObject? CloneVanillaModel(string cosmeticName)
    {
        try
        {
            var cacheField = typeof(CosmeticDataManager).GetField("_cosmeticCache",
                BindingFlags.NonPublic | BindingFlags.Static);
            var cache = cacheField?.GetValue(null) as Dictionary<int, CosmeticData>;
            if (cache == null) return null;

            foreach (var kvp in cache)
            {
                var cd = kvp.Value;
                if (cd == null || cd.cosmeticModel == null) continue;
                if (!string.Equals(cd.cosmeticName, cosmeticName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var mf = cd.cosmeticModel.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                var go = new GameObject(cosmeticName + "_Clone");
                go.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
                var cmf = go.AddComponent<MeshFilter>();
                cmf.sharedMesh = UnityEngine.Object.Instantiate(mf.sharedMesh);
                go.AddComponent<MeshRenderer>();
                return go;
            }
            return null;
        }
        catch { return null; }
    }

    private static Color GetTintColor(float[] tint)
    {
        if (tint == null || tint.Length < 3) return Color.white;
        return new Color(Mathf.Clamp01(tint[0]), Mathf.Clamp01(tint[1]), Mathf.Clamp01(tint[2]));
    }

    // ---- JSON parsing (reused from old CustomCosmeticRegistry) ----

    private static List<CosmeticEntry> ParseFromJson(string json)
    {
        var entries = new List<CosmeticEntry>();
        try
        {
            json = json.Trim();
            if (json.StartsWith("{"))
            {
                var dict = TinyJson.ParseObject(json);
                if (dict.TryGetValue("cosmetics", out var val) && val is string arr)
                    json = arr.Trim();
                else
                    return entries;
            }

            if (!json.StartsWith("[")) return entries;

            foreach (var item in TinyJson.ParseArray(json))
            {
                var entry = new CosmeticEntry();
                if (item.TryGetValue("name", out var n) && n is string s) entry.name = s;
                if (item.TryGetValue("type", out var t) && t is string ts) entry.type = ts;
                if (item.TryGetValue("rarity", out var r) && r is string rs) entry.rarity = rs;
                if (item.TryGetValue("description", out var d) && d is string ds) entry.description = ds;
                if (item.TryGetValue("texture", out var tx) && tx is string txs) entry.texture = txs;
                if (item.TryGetValue("shader", out var sh) && sh is string shs) entry.shader = shs;

                if (item.TryGetValue("tint", out var to))
                {
                    if (to is float[] fa) entry.tint = fa;
                    else if (to is List<float> fl) entry.tint = fl.ToArray();
                }

                if (item.TryGetValue("model", out var mv))
                {
                    entry.model = new ModelRef();
                    if (mv is string ms) entry.model.vanilla = ms;
                    else if (mv is Dictionary<string, object> md)
                    {
                        if (md.TryGetValue("vanilla", out var v) && v is string vs) entry.model.vanilla = vs;
                        if (md.TryGetValue("obj", out var o) && o is string os) entry.model.obj = os;
                        if (md.TryGetValue("bundle", out var b) && b is string bs) entry.model.bundle = bs;
                        if (md.TryGetValue("asset", out var a) && a is string asms) entry.model.asset = asms;
                    }
                }

                if (!string.IsNullOrEmpty(entry.name))
                    entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MoreCosmetics] JSON parse error: {ex.Message}");
        }
        return entries;
    }
}
