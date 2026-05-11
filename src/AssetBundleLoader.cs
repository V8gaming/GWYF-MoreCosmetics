using System.IO;
using UnityEngine;

namespace GWYF_NewClothing;

internal static class AssetBundleLoader
{
    public static GameObject LoadModel(string bundlePath, string assetName)
    {
        try
        {
            if (!File.Exists(bundlePath))
            {
                Debug.LogError($"[GWYF] Bundle file not found: {bundlePath}");
                return null!;
            }

            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogError($"[GWYF] Failed to load bundle: {bundlePath}");
                return null!;
            }

            var asset = bundle.LoadAsset<GameObject>(assetName);
            bundle.Unload(false);

            if (asset == null)
            {
                Debug.LogError($"[GWYF] Asset '{assetName}' not found in bundle: {bundlePath}");
                return null!;
            }

            var clone = UnityEngine.Object.Instantiate(asset);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(clone);

            return clone;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GWYF] Bundle load failed: {bundlePath} — {ex.Message}");
            return null!;
        }
    }
}
