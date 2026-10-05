using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelColorConfigUtility
{
    public const string AssetPath = "Assets/Resources/Config/LevelColorConfig.asset";

    [InitializeOnLoadMethod]
    static void AutoEnsureConfigOnLoad()
    {
        EditorApplication.delayCall += () => EnsureConfigAssetExists();
    }

    [MenuItem("Meowdoku/Config/Create Level Color Config")]
    public static void CreateConfigAsset()
    {
        EnsureConfigAssetExists(forceReset: true);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelColorConfig>(AssetPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    public static LevelColorConfig EnsureConfigAssetExists(bool forceReset = false)
    {
        var existing = AssetDatabase.LoadAssetAtPath<LevelColorConfig>(AssetPath);
        if (existing != null && !forceReset)
            return existing;

        Directory.CreateDirectory(Path.GetDirectoryName(AssetPath) ?? "Assets/Resources/Config");

        LevelColorConfig config;
        if (existing != null)
        {
            config = existing;
        }
        else
        {
            config = ScriptableObject.CreateInstance<LevelColorConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
        }

        config.ResetToGeneratedDefaults();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        LevelPalette.Reload();
        return config;
    }
}
