using System;

[Serializable]
public class LevelData
{
    public string[][] solution;
    public int[][] colorMap;
    public bool[][] catRevealed;
    public HintStep[] hintPlan;

    public int Size => RowCount;

    public int RowCount => colorMap?.Length ?? solution?.Length ?? 0;

    public int ColCount
    {
        get
        {
            if (colorMap != null && colorMap.Length > 0 && colorMap[0] != null)
                return colorMap[0].Length;
            if (solution != null && solution.Length > 0 && solution[0] != null)
                return solution[0].Length;
            return 0;
        }
    }

    public int ColorCount
    {
        get
        {
            if (colorMap == null) return 0;
            var max = 0;
            foreach (var row in colorMap)
            {
                if (row == null) continue;
                foreach (var c in row)
                    if (c > max) max = c;
            }
            return max;
        }
    }

    public int QueenCount
    {
        get
        {
            if (solution == null) return 0;
            var count = 0;
            foreach (var row in solution)
            {
                if (row == null) continue;
                foreach (var cell in row)
                    if (cell == "Q") count++;
            }
            return count;
        }
    }

    public int RevealedCatCount
    {
        get
        {
            if (catRevealed == null) return 0;
            var count = 0;
            foreach (var row in catRevealed)
            {
                if (row == null) continue;
                foreach (var revealed in row)
                    if (revealed) count++;
            }
            return count;
        }
    }

    public bool HasQueenAt(int row, int col)
    {
        if (solution == null || row < 0 || row >= solution.Length) return false;
        var r = solution[row];
        return r != null && col >= 0 && col < r.Length && r[col] == "Q";
    }

    public bool IsCatRevealedAt(int row, int col)
    {
        if (!HasQueenAt(row, col)) return false;
        if (catRevealed == null || row < 0 || row >= catRevealed.Length) return false;
        var r = catRevealed[row];
        return r != null && col >= 0 && col < r.Length && r[col];
    }

    public int GetColorAt(int row, int col)
    {
        if (colorMap == null || row < 0 || row >= colorMap.Length) return 0;
        var r = colorMap[row];
        return r != null && col >= 0 && col < r.Length ? r[col] : 0;
    }
}

[Serializable]
public class HintStep
{
    public string rule;
    public string action;
    public string reason;
    public int[][] cells;
    public int depth;
}
