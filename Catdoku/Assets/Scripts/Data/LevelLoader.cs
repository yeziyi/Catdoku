using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class LevelLoader
{
    public const string LevelsResourcePath = "Levels";
    public const string LevelsFolderPath = "Assets/Resources/Levels";

    public static LevelData LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        var solution = ParseStringMatrix(ExtractArray(json, "solution"));
        var colorMapJson = ExtractArray(json, "colorMap");
        var catRevealedJson = ExtractArray(json, "catRevealed");

        int[][] colorMap;
        bool[][] catRevealed;

        if (!string.IsNullOrEmpty(catRevealedJson))
        {
            colorMap = ParseIntMatrix(colorMapJson);
            catRevealed = ParseBoolMatrix(catRevealedJson);
            EnsureCatRevealedSize(catRevealed, colorMap);
            SyncCatRevealedWithSolution(catRevealed, solution);
        }
        else
        {
            (colorMap, catRevealed) = ParseColorMap(colorMapJson, solution);
        }

        return new LevelData
        {
            solution = solution,
            colorMap = colorMap,
            catRevealed = catRevealed,
            hintPlan = ParseHintPlan(ExtractArray(json, "hintPlan"))
        };
    }

    public static LevelData LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        return LoadFromJson(File.ReadAllText(filePath));
    }

    public static LevelData LoadFromResources(string levelName)
    {
        var asset = Resources.Load<TextAsset>($"{LevelsResourcePath}/{levelName}");
        return asset != null ? LoadFromJson(asset.text) : null;
    }

    public static List<string> GetAllLevelFileNames()
    {
        var dir = Path.Combine(Application.dataPath, "Resources", "Levels");
        if (!Directory.Exists(dir)) return new List<string>();

        return Directory.GetFiles(dir, "level_*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(name =>
            {
                var numPart = name.Replace("level_", "");
                return int.TryParse(numPart, out var n) ? n : int.MaxValue;
            })
            .ThenBy(name => name)
            .ToList();
    }

    public static string GetLevelNameByNumber(int levelNumber) => $"level_{levelNumber}";

    public static bool LevelExists(string levelName)
    {
        return Resources.Load<TextAsset>($"{LevelsResourcePath}/{levelName}") != null;
    }

    public static int GetNextLevelNumber()
    {
        var max = 0;
        foreach (var name in GetAllLevelFileNames())
        {
            if (!name.StartsWith("level_", StringComparison.Ordinal)) continue;
            if (int.TryParse(name.Substring("level_".Length), out var n))
                max = Math.Max(max, n);
        }

        return max + 1;
    }

    public static string GetLevelFilePath(string levelName)
    {
        return Path.Combine(LevelsFolderPath, levelName + ".json");
    }

    public static string GetLevelFileAbsolutePath(string levelName)
    {
        return Path.Combine(Application.dataPath, "Resources", "Levels", levelName + ".json");
    }

    public static LevelData LoadLevelByName(string levelName)
    {
        return LoadFromFile(GetLevelFileAbsolutePath(levelName));
    }

    public static bool SaveLevel(string levelName, LevelData level)
    {
        if (level == null || string.IsNullOrEmpty(levelName)) return false;

        var path = GetLevelFileAbsolutePath(levelName);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, SerializeLevel(level));
        return true;
    }

    public static string SerializeLevel(LevelData level)
    {
        var sb = new StringBuilder();
        sb.Append("{\"solution\":");
        sb.Append(SerializeStringMatrix(level.solution));
        sb.Append(",\"colorMap\":");
        sb.Append(SerializeIntMatrix(level.colorMap));
        sb.Append(",\"catRevealed\":");
        sb.Append(SerializeBoolMatrix(level.catRevealed));
        sb.Append(",\"hintPlan\":");
        sb.Append(SerializeHintPlan(level.hintPlan));
        sb.Append('}');
        return sb.ToString();
    }

    static string SerializeStringMatrix(string[][] matrix)
    {
        if (matrix == null) return "[]";

        var sb = new StringBuilder("[");
        for (var row = 0; row < matrix.Length; row++)
        {
            if (row > 0) sb.Append(',');
            sb.Append('[');
            var cells = matrix[row];
            if (cells != null)
            {
                for (var col = 0; col < cells.Length; col++)
                {
                    if (col > 0) sb.Append(',');
                    sb.Append('"').Append(EscapeJson(cells[col] ?? ".")).Append('"');
                }
            }
            sb.Append(']');
        }
        sb.Append(']');
        return sb.ToString();
    }

    public static void PrepareLevelForSave(LevelData level)
    {
        if (level?.colorMap == null) return;

        var rows = level.RowCount;
        var cols = level.ColCount;
        if (rows == 0 || cols == 0) return;

        if (level.catRevealed == null || level.catRevealed.Length != rows)
        {
            level.catRevealed = new bool[rows][];
            for (var row = 0; row < rows; row++)
                level.catRevealed[row] = new bool[cols];
        }

        for (var row = 0; row < rows; row++)
        {
            if (level.catRevealed[row] == null || level.catRevealed[row].Length != cols)
                level.catRevealed[row] = new bool[cols];
        }

        SyncCatRevealedWithSolution(level.catRevealed, level.solution);
    }

    static string SerializeBoolMatrix(bool[][] matrix)
    {
        if (matrix == null) return "[]";

        var sb = new StringBuilder("[");
        for (var row = 0; row < matrix.Length; row++)
        {
            if (row > 0) sb.Append(',');
            sb.Append('[');
            var cells = matrix[row];
            if (cells != null)
            {
                for (var col = 0; col < cells.Length; col++)
                {
                    if (col > 0) sb.Append(',');
                    sb.Append(cells[col] ? "true" : "false");
                }
            }
            sb.Append(']');
        }
        sb.Append(']');
        return sb.ToString();
    }

    static bool[][] ParseBoolMatrix(string arrayJson)
    {
        if (string.IsNullOrEmpty(arrayJson)) return null;

        var rows = SplitTopLevelElements(arrayJson);
        var result = new bool[rows.Count][];
        for (var i = 0; i < rows.Count; i++)
            result[i] = ParseBoolRow(rows[i]);
        return result;
    }

    static bool[] ParseBoolRow(string rowJson)
    {
        var items = new List<bool>();
        if (string.IsNullOrEmpty(rowJson) || rowJson[0] != '[') return items.ToArray();

        var i = 1;
        while (i < rowJson.Length - 1)
        {
            while (i < rowJson.Length && (rowJson[i] == ',' || char.IsWhiteSpace(rowJson[i]))) i++;
            if (i >= rowJson.Length - 1) break;

            if (i + 4 <= rowJson.Length && rowJson.Substring(i, 4) == "true")
            {
                items.Add(true);
                i += 4;
            }
            else if (i + 5 <= rowJson.Length && rowJson.Substring(i, 5) == "false")
            {
                items.Add(false);
                i += 5;
            }
            else
            {
                i++;
            }
        }

        return items.ToArray();
    }

    static void EnsureCatRevealedSize(bool[][] catRevealed, int[][] colorMap)
    {
        if (catRevealed == null || colorMap == null) return;

        for (var row = 0; row < colorMap.Length; row++)
        {
            if (row >= catRevealed.Length || catRevealed[row] == null) continue;
            var cols = colorMap[row]?.Length ?? 0;
            if (catRevealed[row].Length < cols)
            {
                var resized = new bool[cols];
                for (var col = 0; col < catRevealed[row].Length; col++)
                    resized[col] = catRevealed[row][col];
                catRevealed[row] = resized;
            }
        }
    }

    static string SerializeIntMatrix(int[][] matrix)
    {
        if (matrix == null) return "[]";

        var sb = new StringBuilder("[");
        for (var row = 0; row < matrix.Length; row++)
        {
            if (row > 0) sb.Append(',');
            sb.Append('[');
            var cells = matrix[row];
            if (cells != null)
            {
                for (var col = 0; col < cells.Length; col++)
                {
                    if (col > 0) sb.Append(',');
                    sb.Append(cells[col]);
                }
            }
            sb.Append(']');
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string SerializeHintPlan(HintStep[] hints)
    {
        if (hints == null || hints.Length == 0) return "[]";

        var sb = new StringBuilder("[");
        for (var i = 0; i < hints.Length; i++)
        {
            if (i > 0) sb.Append(',');
            var hint = hints[i];
            sb.Append("{\"rule\":\"").Append(EscapeJson(hint.rule)).Append('"');
            sb.Append(",\"action\":\"").Append(EscapeJson(hint.action)).Append('"');
            sb.Append(",\"reason\":\"").Append(EscapeJson(hint.reason)).Append('"');
            sb.Append(",\"cells\":");
            sb.Append(SerializeCellPairs(hint.cells));
            sb.Append(",\"depth\":").Append(hint.depth).Append('}');
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string SerializeCellPairs(int[][] cells)
    {
        if (cells == null || cells.Length == 0) return "[]";

        var sb = new StringBuilder("[");
        for (var i = 0; i < cells.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('[');
            var pair = cells[i];
            if (pair != null && pair.Length >= 2)
                sb.Append(pair[0]).Append(',').Append(pair[1]);
            sb.Append(']');
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    static string ExtractArray(string json, string key)
    {
        var marker = $"\"{key}\":";
        var start = json.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return null;

        start += marker.Length;
        while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
        if (start >= json.Length || json[start] != '[') return null;

        var depth = 0;
        for (var i = start; i < json.Length; i++)
        {
            if (json[i] == '[') depth++;
            else if (json[i] == ']')
            {
                depth--;
                if (depth == 0) return json.Substring(start, i - start + 1);
            }
        }

        return null;
    }

    static string[][] ParseStringMatrix(string arrayJson)
    {
        if (string.IsNullOrEmpty(arrayJson)) return null;

        var rows = SplitTopLevelElements(arrayJson);
        var result = new string[rows.Count][];
        for (var i = 0; i < rows.Count; i++)
            result[i] = ParseStringRow(rows[i]);
        return result;
    }

    static int[][] ParseIntMatrix(string arrayJson)
    {
        if (string.IsNullOrEmpty(arrayJson)) return null;

        var rows = SplitTopLevelElements(arrayJson);
        var result = new int[rows.Count][];
        for (var i = 0; i < rows.Count; i++)
            result[i] = ParseIntValues(rows[i]);
        return result;
    }

    static (int[][] colorMap, bool[][] catRevealed) ParseColorMap(string arrayJson, string[][] solution)
    {
        if (string.IsNullOrEmpty(arrayJson)) return (null, null);

        var rows = SplitTopLevelElements(arrayJson);
        var colorMap = new int[rows.Count][];
        var catRevealed = new bool[rows.Count][];

        for (var r = 0; r < rows.Count; r++)
        {
            var tokens = ParseColorMapRowValues(rows[r]);
            colorMap[r] = new int[tokens.Length];
            catRevealed[r] = new bool[tokens.Length];

            for (var c = 0; c < tokens.Length; c++)
            {
                if (tokens[c] == "Q")
                {
                    catRevealed[r][c] = true;
                    colorMap[r][c] = 0;
                }
                else if (int.TryParse(tokens[c], out var color))
                {
                    colorMap[r][c] = color;
                    catRevealed[r][c] = false;
                }
            }
        }

        InferMissingColorMapValues(colorMap);
        SyncCatRevealedWithSolution(catRevealed, solution);
        return (colorMap, catRevealed);
    }

    static string[] ParseColorMapRowValues(string rowJson)
    {
        var tokens = new List<string>();
        if (string.IsNullOrEmpty(rowJson) || rowJson[0] != '[') return tokens.ToArray();

        var i = 1;
        while (i < rowJson.Length - 1)
        {
            while (i < rowJson.Length && (rowJson[i] == ',' || char.IsWhiteSpace(rowJson[i]))) i++;
            if (i >= rowJson.Length - 1) break;

            if (rowJson[i] == '"')
            {
                var j = i + 1;
                while (j < rowJson.Length && rowJson[j] != '"') j++;
                tokens.Add(rowJson.Substring(i + 1, j - i - 1));
                i = j + 1;
            }
            else
            {
                var j = i;
                while (j < rowJson.Length && rowJson[j] != ',' && rowJson[j] != ']') j++;
                tokens.Add(rowJson.Substring(i, j - i).Trim());
                i = j;
            }
        }

        return tokens.ToArray();
    }

    static void InferMissingColorMapValues(int[][] colorMap)
    {
        if (colorMap == null || colorMap.Length == 0) return;

        var rows = colorMap.Length;
        var cols = colorMap[0]?.Length ?? 0;
        var changed = true;

        while (changed)
        {
            changed = false;
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    if (colorMap[r][c] != 0) continue;
                    var inferred = InferColorFromNeighbors(colorMap, r, c, rows, cols);
                    if (inferred <= 0) continue;
                    colorMap[r][c] = inferred;
                    changed = true;
                }
            }
        }

        for (var r = 0; r < rows; r++)
        for (var c = 0; c < cols; c++)
            if (colorMap[r][c] == 0)
                colorMap[r][c] = 1;
    }

    static int InferColorFromNeighbors(int[][] colorMap, int row, int col, int rows, int cols)
    {
        var counts = new Dictionary<int, int>();
        if (row > 0 && colorMap[row - 1][col] > 0)
            AddColorCount(counts, colorMap[row - 1][col]);
        if (row < rows - 1 && colorMap[row + 1][col] > 0)
            AddColorCount(counts, colorMap[row + 1][col]);
        if (col > 0 && colorMap[row][col - 1] > 0)
            AddColorCount(counts, colorMap[row][col - 1]);
        if (col < cols - 1 && colorMap[row][col + 1] > 0)
            AddColorCount(counts, colorMap[row][col + 1]);

        var bestColor = 0;
        var bestCount = 0;
        foreach (var pair in counts)
        {
            if (pair.Value <= bestCount) continue;
            bestCount = pair.Value;
            bestColor = pair.Key;
        }

        return bestColor;
    }

    static void AddColorCount(Dictionary<int, int> counts, int colorId)
    {
        if (!counts.ContainsKey(colorId))
            counts[colorId] = 0;
        counts[colorId]++;
    }

    static void SyncCatRevealedWithSolution(bool[][] catRevealed, string[][] solution)
    {
        if (catRevealed == null || solution == null) return;

        for (var r = 0; r < catRevealed.Length && r < solution.Length; r++)
        {
            if (catRevealed[r] == null || solution[r] == null) continue;
            for (var c = 0; c < catRevealed[r].Length && c < solution[r].Length; c++)
                if (solution[r][c] != "Q" && solution[r][c] != "M")
                    catRevealed[r][c] = false;
        }
    }

    static HintStep[] ParseHintPlan(string arrayJson)
    {
        if (string.IsNullOrEmpty(arrayJson)) return null;

        var objects = SplitTopLevelObjects(arrayJson);
        var result = new HintStep[objects.Count];
        for (var i = 0; i < objects.Count; i++)
            result[i] = ParseHintObject(objects[i]);
        return result;
    }

    static HintStep ParseHintObject(string objJson)
    {
        return new HintStep
        {
            rule = ExtractStringField(objJson, "rule"),
            action = ExtractStringField(objJson, "action"),
            reason = ExtractStringField(objJson, "reason"),
            depth = ExtractIntField(objJson, "depth"),
            cells = ParseCellPairs(ExtractArray(objJson, "cells"))
        };
    }

    static int[][] ParseCellPairs(string arrayJson)
    {
        if (string.IsNullOrEmpty(arrayJson)) return null;

        var pairs = SplitTopLevelElements(arrayJson);
        var result = new int[pairs.Count][];
        for (var i = 0; i < pairs.Count; i++)
            result[i] = ParseIntValues(pairs[i]);
        return result;
    }

    static List<string> SplitTopLevelElements(string arrayJson)
    {
        var elements = new List<string>();
        if (string.IsNullOrEmpty(arrayJson) || arrayJson[0] != '[') return elements;

        var depth = 0;
        var start = -1;
        for (var i = 0; i < arrayJson.Length; i++)
        {
            if (arrayJson[i] == '[')
            {
                depth++;
                if (depth == 2) start = i;
            }
            else if (arrayJson[i] == ']')
            {
                if (depth == 2 && start >= 0)
                {
                    elements.Add(arrayJson.Substring(start, i - start + 1));
                    start = -1;
                }
                depth--;
            }
        }

        return elements;
    }

    static List<string> SplitTopLevelObjects(string arrayJson)
    {
        var elements = new List<string>();
        if (string.IsNullOrEmpty(arrayJson) || arrayJson[0] != '[') return elements;

        var depth = 0;
        var start = -1;
        for (var i = 0; i < arrayJson.Length; i++)
        {
            if (arrayJson[i] == '{')
            {
                if (depth == 0) start = i;
                depth++;
            }
            else if (arrayJson[i] == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    elements.Add(arrayJson.Substring(start, i - start + 1));
                    start = -1;
                }
            }
        }

        return elements;
    }

    static string[] ParseStringRow(string rowJson)
    {
        var items = new List<string>();
        for (var i = 0; i < rowJson.Length; i++)
        {
            if (rowJson[i] != '"') continue;

            var j = i + 1;
            while (j < rowJson.Length && rowJson[j] != '"') j++;
            items.Add(rowJson.Substring(i + 1, j - i - 1));
            i = j;
        }

        return items.ToArray();
    }

    static int[] ParseIntValues(string rowJson)
    {
        var items = new List<int>();
        for (var i = 0; i < rowJson.Length; i++)
        {
            if (!char.IsDigit(rowJson[i]) && rowJson[i] != '-') continue;

            var j = i + 1;
            while (j < rowJson.Length && (char.IsDigit(rowJson[j]) || rowJson[j] == '-')) j++;
            if (int.TryParse(rowJson.Substring(i, j - i), out var value))
                items.Add(value);
            i = j - 1;
        }

        return items.ToArray();
    }

    static string ExtractStringField(string json, string key)
    {
        var marker = $"\"{key}\":\"";
        var start = json.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return null;

        start += marker.Length;
        var end = json.IndexOf('"', start);
        return end < 0 ? null : json.Substring(start, end - start);
    }

    static int ExtractIntField(string json, string key)
    {
        var marker = $"\"{key}\":";
        var start = json.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return 0;

        start += marker.Length;
        while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

        var end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;

        return int.TryParse(json.Substring(start, end - start), out var value) ? value : 0;
    }
}
