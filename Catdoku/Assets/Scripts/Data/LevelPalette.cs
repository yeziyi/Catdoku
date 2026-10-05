using UnityEngine;

public static class LevelPalette
{
    public const string ConfigResourcePath = "Config/LevelColorConfig";

    static LevelColorConfig _config;

    public static LevelColorConfig Config => _config ??= LoadConfig();

    public static Color GetColor(int colorId) => Config.GetCellColor(colorId);

    public static Color GetHintHighlightColor(string action) => Config.GetHintHighlightColor(action);

    public static Color GetContrastingTextColor(Color background) => Config.GetContrastingTextColor(background);

    public static void Reload()
    {
        _config = null;
    }

    static LevelColorConfig LoadConfig()
    {
        var config = Resources.Load<LevelColorConfig>(ConfigResourcePath);
        if (config != null) return config;

        var fallback = ScriptableObject.CreateInstance<LevelColorConfig>();
        fallback.ResetToGeneratedDefaults();
        Debug.LogWarning($"LevelPalette: Missing Resources/{ConfigResourcePath}.asset — using generated fallback colors.");
        return fallback;
    }
}
