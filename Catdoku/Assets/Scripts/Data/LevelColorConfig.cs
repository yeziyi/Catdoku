using UnityEngine;

[CreateAssetMenu(fileName = "LevelColorConfig", menuName = "Meowdoku/Level Color Config")]
public class LevelColorConfig : ScriptableObject
{
    [Header("Cell Colors (index 0 = color ID 1)")]
    public Color emptyCellColor = new(0.2f, 0.2f, 0.2f, 1f);
    public Color[] cellColors =
    {
        Hsv(0.618f, 0.55f, 0.92f),
        Hsv(0.236f, 0.55f, 0.92f),
        Hsv(0.854f, 0.55f, 0.92f),
        Hsv(0.472f, 0.55f, 0.92f),
        Hsv(0.090f, 0.55f, 0.92f),
        Hsv(0.708f, 0.55f, 0.92f),
        Hsv(0.326f, 0.55f, 0.92f),
        Hsv(0.944f, 0.55f, 0.92f),
        Hsv(0.562f, 0.55f, 0.92f),
        Hsv(0.180f, 0.55f, 0.92f),
        Hsv(0.798f, 0.55f, 0.92f),
        Hsv(0.416f, 0.55f, 0.92f)
    };

    [Header("Hint Highlights (Level Editor)")]
    public Color hintPlaceQueen = new(1f, 0.85f, 0.1f, 0.85f);
    public Color hintEliminate = new(1f, 0.35f, 0.35f, 0.75f);
    public Color hintDefault = new(0.4f, 0.7f, 1f, 0.75f);

    [Header("Cell Labels")]
    public Color darkTextColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    public Color lightTextColor = new(1f, 1f, 1f, 0.95f);
    [Range(0f, 1f)] public float contrastLuminanceThreshold = 0.62f;

    public Color GetCellColor(int colorId)
    {
        if (colorId <= 0) return emptyCellColor;
        if (cellColors != null && colorId <= cellColors.Length)
            return cellColors[colorId - 1];
        return GenerateColor(colorId);
    }

    public Color GetContrastingTextColor(Color background)
    {
        var luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
        return luminance > contrastLuminanceThreshold ? darkTextColor : lightTextColor;
    }

    public Color GetHintHighlightColor(string action)
    {
        return action switch
        {
            "place_queen" => hintPlaceQueen,
            "eliminate" => hintEliminate,
            _ => hintDefault
        };
    }

    public void ResetToGeneratedDefaults(int count = 12)
    {
        cellColors = new Color[count];
        for (var i = 0; i < count; i++)
            cellColors[i] = GenerateColor(i + 1);
    }

    static Color GenerateColor(int colorId)
    {
        var hue = colorId * 0.618033988749895f % 1f;
        return Hsv(hue, 0.55f, 0.92f);
    }

    static Color Hsv(float h, float s, float v)
    {
        var color = Color.HSVToRGB(h, s, v);
        color.a = 1f;
        return color;
    }
}
