using System.Collections.Generic;

public static class LevelValidator
{
    static readonly int[] RowOffsets = { -1, -1, -1, 0, 0, 1, 1, 1 };
    static readonly int[] ColOffsets = { -1, 0, 1, -1, 1, -1, 0, 1 };

    public static List<string> Validate(LevelData level)
    {
        var errors = new List<string>();
        if (level == null)
        {
            errors.Add("Level data is null.");
            return errors;
        }

        var rows = level.RowCount;
        var cols = level.ColCount;
        if (rows == 0 || cols == 0)
        {
            errors.Add("Board is empty.");
            return errors;
        }

        if (rows != cols)
            errors.Add($"Board must be square (current: {rows}x{cols}).");

        if (level.colorMap == null || level.solution == null)
        {
            errors.Add("colorMap and solution are required.");
            return errors;
        }

        for (var row = 0; row < rows; row++)
        {
            if (level.colorMap[row] == null || level.colorMap[row].Length != cols)
            {
                errors.Add($"colorMap row {row} has invalid length.");
                return errors;
            }

            if (level.solution[row] == null || level.solution[row].Length != cols)
            {
                errors.Add($"solution row {row} has invalid length.");
                return errors;
            }

            for (var col = 0; col < cols; col++)
            {
                if (level.colorMap[row][col] <= 0)
                    errors.Add($"Cell ({row},{col}) has no color painted.");
            }
        }

        var queens = CollectQueens(level);
        if (queens.Count != rows)
            errors.Add($"Board {rows}x{rows} requires {rows} cats, found {queens.Count}.");

        ValidateOneCatPerColor(level, queens, errors);
        ValidateUniqueRowsAndColumns(queens, errors);
        ValidateNoAdjacentQueens(queens, rows, cols, errors);

        return errors;
    }

    static List<(int row, int col)> CollectQueens(LevelData level)
    {
        var queens = new List<(int row, int col)>();
        for (var row = 0; row < level.RowCount; row++)
        {
            for (var col = 0; col < level.ColCount; col++)
            {
                if (level.solution[row][col] == "Q")
                    queens.Add((row, col));
            }
        }

        return queens;
    }

    static void ValidateOneCatPerColor(LevelData level, List<(int row, int col)> queens, List<string> errors)
    {
        var colorUsage = new Dictionary<int, int>();
        var colorsOnBoard = new HashSet<int>();

        for (var row = 0; row < level.RowCount; row++)
        {
            for (var col = 0; col < level.ColCount; col++)
                colorsOnBoard.Add(level.GetColorAt(row, col));
        }

        foreach (var (row, col) in queens)
        {
            var colorId = level.GetColorAt(row, col);
            if (!colorUsage.ContainsKey(colorId))
                colorUsage[colorId] = 0;
            colorUsage[colorId]++;
        }

        foreach (var colorId in colorsOnBoard)
        {
            colorUsage.TryGetValue(colorId, out var count);
            if (count == 0)
                errors.Add($"Color {colorId} has no cat.");
            else if (count > 1)
                errors.Add($"Color {colorId} has {count} cats (must be exactly 1).");
        }

        if (colorsOnBoard.Count != level.RowCount)
            errors.Add($"Board needs {level.RowCount} distinct colors, found {colorsOnBoard.Count}.");
    }

    static void ValidateUniqueRowsAndColumns(List<(int row, int col)> queens, List<string> errors)
    {
        var usedRows = new HashSet<int>();
        var usedCols = new HashSet<int>();

        foreach (var (row, col) in queens)
        {
            if (!usedRows.Add(row))
                errors.Add($"Row {row} has more than one cat.");
            if (!usedCols.Add(col))
                errors.Add($"Column {col} has more than one cat.");
        }
    }

    static void ValidateNoAdjacentQueens(
        List<(int row, int col)> queens,
        int rows,
        int cols,
        List<string> errors)
    {
        var queenSet = new HashSet<long>();
        foreach (var (row, col) in queens)
            queenSet.Add(CellKey(row, col));

        foreach (var (row, col) in queens)
        {
            for (var i = 0; i < RowOffsets.Length; i++)
            {
                var nr = row + RowOffsets[i];
                var nc = col + ColOffsets[i];
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                if (queenSet.Contains(CellKey(nr, nc)))
                {
                    errors.Add($"Cats at ({row},{col}) and ({nr},{nc}) are adjacent.");
                    return;
                }
            }
        }
    }

    static long CellKey(int row, int col) => ((long)row << 32) | (uint)col;
}
