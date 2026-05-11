using System;
using UnityEngine;

namespace GWYF_NewClothing;

[Serializable]
public class CosmeticEntry
{
    public string name;
    public string type;
    public string rarity;
    public string description;
    public ModelRef model;
    public string texture;
    public float[] tint;
    public string shader;
}

[Serializable]
public class ModelRef
{
    public string vanilla;
    public string obj;
    public string bundle;
    public string asset;

    public bool IsVanilla => !string.IsNullOrEmpty(vanilla);
    public bool IsObj => !string.IsNullOrEmpty(obj);
    public bool IsBundle => !string.IsNullOrEmpty(bundle);
}
