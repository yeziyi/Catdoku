using System.Collections.Generic;

public static class LevelCatSuggester
{
    static readonly int[] RowOffsets = { -1, -1, -1, 0, 0, 1, 1, 1 };
    static readonly int[] ColOffsets = { -1, 0, 1, -1, 1, -1, 0, 1 };

    public static HashSet<long> GetSuggestions(int[][] colorMap, string[][] solution, int boardSize)
    {
        var suggestions = new HashSet<long>();
        if (colorMap == null || solution == null || boardSize <= 0) return suggestions;

        var queens = new List<(int row, int col)>();
        var usedRows = new HashSet<int>();
        var usedCols = new HashSet<int>();
        var colorsWithCat = new HashSet<int>();

        for (var row = 0; row < boardSize; row++)
        {
            for (var col = 0; col < boardSize; col++)
            {
                if (solution[row][col] != "Q") continue;
                queens.Add((row, col));
                usedRows.Add(row);
                usedCols.Add(col);
                colorsWithCat.Add(colorMap[row][col]);
            }
        }

        for (var colorId = 1; colorId <= boardSize; colorId++)
        {
            if (colorsWithCat.Contains(colorId)) continue;

            for (var row = 0; row < boardSize; row++)
            {
                for (var col = 0; col < boardSize; col++)
                {
                    if (colorMap[row][col] != colorId) continue;
                    if (solution[row][col] == "Q") continue;
                    if (usedRows.Contains(row) || usedCols.Contains(col)) continue;
                    if (IsAdjacentToQueen(row, col, queens)) continue;

                    suggestions.Add(CellKey(row, col));
                }
            }
        }

        return suggestions;
    }

    static bool IsAdjacentToQueen(int row, int col, List<(int row, int col)> queens)
    {
        foreach (var (qr, qc) in queens)
        {
            var dr = row - qr;
            var dc = col - qc;
            if (dr >= -1 && dr <= 1 && dc >= -1 && dc <= 1 && (dr != 0 || dc != 0))
                return true;
        }

        return false;
    }

    static long CellKey(int row, int col) => ((long)row << 32) | (uint)col;
}
