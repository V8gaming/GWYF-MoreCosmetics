using System.IO;
using UnityEngine;

namespace GWYF_NewClothing;

internal static class TextureLoader
{
    public static Texture2D Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"[GWYF] Texture file not found: {path}");
            return null!;
        }

        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Debug.LogError($"[GWYF] Failed to load texture: {path}");
            UnityEngine.Object.Destroy(tex);
            return null!;
        }

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }
}
