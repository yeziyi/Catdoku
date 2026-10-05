using UnityEditor;
using UnityEngine;

public static class CellPrefabBuilder
{
    const string PrefabPath = "Assets/Resources/GameObjects/Cell.prefab";
    const string BgSpritePath = "Assets/Resources/Images/GameView/BgTile.png";
    const string CrossSpritePath = "Assets/Resources/Images/GameView/IconCross.png";
    const string CatSpritePath = "Assets/Resources/Images/GameView/IconCat.png";

    [MenuItem("Meowdoku/Create Cell Prefab")]
    public static void CreatePrefabFromMenu()
    {
        CreatePrefab();
    }

    public static void CreatePrefab()
    {
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgSpritePath);
        var crossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrossSpritePath);
        var catSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CatSpritePath);

        if (bgSprite == null || crossSprite == null || catSprite == null)
        {
            Debug.LogError("CellPrefabBuilder: Missing sprite assets. Check GameView image paths.");
            return;
        }

        var root = new GameObject("Cell");

        var background = CreateSpriteChild(root.transform, "background", bgSprite, 0, true);
        var iconX = CreateSpriteChild(root.transform, "iconX", crossSprite, 1, false);
        var iconCat = CreateSpriteChild(root.transform, "iconCat", catSprite, 2, false);

        iconX.transform.localScale = Vector3.one * 0.65f;
        iconCat.transform.localScale = Vector3.one * 0.75f;

        var collider = root.AddComponent<BoxCollider2D>();
        collider.size = background.bounds.size;

        var cellView = root.AddComponent<CellView>();
        var serializedCell = new SerializedObject(cellView);
        serializedCell.FindProperty("_background").objectReferenceValue = background;
        serializedCell.FindProperty("_iconX").objectReferenceValue = iconX;
        serializedCell.FindProperty("_iconCat").objectReferenceValue = iconCat;
        serializedCell.ApplyModifiedPropertiesWithoutUndo();

        EnsureFolder("Assets/Resources/GameObjects");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Cell prefab created at {PrefabPath}");
    }

    static SpriteRenderer CreateSpriteChild(Transform parent, string name, Sprite sprite, int sortingOrder, bool enabled)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = enabled;
        return renderer;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
