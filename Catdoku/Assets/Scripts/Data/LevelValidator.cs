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

        var cats = CollectSymbol(level, "Q");
        var mice = CollectSymbol(level, "M");

        if (mice.Count == 0)
            ValidateClassicQueens(level, cats, errors);
        else
            ValidateSymmetricCatMouse(level, cats, mice, errors);

        return errors;
    }

    // Classic single-animal Queens (existing 1500 levels).
    static void ValidateClassicQueens(LevelData level, List<(int row, int col)> queens, List<string> errors)
    {
        if (queens.Count != level.RowCount)
            errors.Add($"Board {level.RowCount}x{level.RowCount} requires {level.RowCount} cats, found {queens.Count}.");

        ValidateOneCatPerColor(level, queens, errors);
        ValidateUniqueRowsAndColumns(queens, errors, "cat");
        ValidateNoAdjacent(queens, level.RowCount, level.ColCount, errors, "cat");
    }

    // Symmetric cat + mouse: both one per region/row/col, same-species non-adjacent,
    // and cat-mouse non-adjacent (8 directions).
    static void ValidateSymmetricCatMouse(
        LevelData level,
        List<(int row, int col)> cats,
        List<(int row, int col)> mice,
        List<string> errors)
    {
        var rows = level.RowCount;
        var cols = level.ColCount;

        if (cats.Count != rows)
            errors.Add($"Board {rows}x{rows} requires {rows} cats, found {cats.Count}.");
        if (mice.Count != rows)
            errors.Add($"Board {rows}x{rows} requires {rows} mice, found {mice.Count}.");

        var colorsOnBoard = new HashSet<int>();
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                colorsOnBoard.Add(level.GetColorAt(r, c));

        if (colorsOnBoard.Count != rows)
            errors.Add($"Board needs {rows} distinct colors, found {colorsOnBoard.Count}.");

        var catsByColor = CountByColor(level, cats);
        var miceByColor = CountByColor(level, mice);
        foreach (var colorId in colorsOnBoard)
        {
            catsByColor.TryGetValue(colorId, out var nc);
            miceByColor.TryGetValue(colorId, out var nm);
            if (nc != 1)
                errors.Add($"Color {colorId} has {nc} cats (must be exactly 1).");
            if (nm != 1)
                errors.Add($"Color {colorId} has {nm} mice (must be exactly 1).");
        }

        ValidateUniqueRowsAndColumns(cats, errors, "cat");
        ValidateUniqueRowsAndColumns(mice, errors, "mouse");
        ValidateNoAdjacent(cats, rows, cols, errors, "cat");
        ValidateNoAdjacent(mice, rows, cols, errors, "mouse");
        ValidateNoCrossAdjacency(cats, mice, rows, cols, errors);
    }

    static List<(int row, int col)> CollectSymbol(LevelData level, string symbol)
    {
        var result = new List<(int row, int col)>();
        for (var row = 0; row < level.RowCount; row++)
        {
            for (var col = 0; col < level.ColCount; col++)
            {
                if (level.solution[row][col] == symbol)
                    result.Add((row, col));
            }
        }

        return result;
    }

    static Dictionary<int, int> CountByColor(LevelData level, List<(int row, int col)> cells)
    {
        var counts = new Dictionary<int, int>();
        foreach (var (row, col) in cells)
        {
            var colorId = level.GetColorAt(row, col);
            if (!counts.ContainsKey(colorId))
                counts[colorId] = 0;
            counts[colorId]++;
        }

        return counts;
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

    static void ValidateUniqueRowsAndColumns(List<(int row, int col)> cells, List<string> errors, string label)
    {
        var usedRows = new HashSet<int>();
        var usedCols = new HashSet<int>();

        foreach (var (row, col) in cells)
        {
            if (!usedRows.Add(row))
                errors.Add($"Row {row} has more than one {label}.");
            if (!usedCols.Add(col))
                errors.Add($"Column {col} has more than one {label}.");
        }
    }

    static void ValidateNoAdjacent(
        List<(int row, int col)> cells,
        int rows,
        int cols,
        List<string> errors,
        string label)
    {
        var cellSet = new HashSet<long>();
        foreach (var (row, col) in cells)
            cellSet.Add(CellKey(row, col));

        foreach (var (row, col) in cells)
        {
            for (var i = 0; i < RowOffsets.Length; i++)
            {
                var nr = row + RowOffsets[i];
                var nc = col + ColOffsets[i];
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                if (cellSet.Contains(CellKey(nr, nc)))
                {
                    errors.Add($"Two {label}s at ({row},{col}) and ({nr},{nc}) are adjacent.");
                    return;
                }
            }
        }
    }

    static void ValidateNoCrossAdjacency(
        List<(int row, int col)> cats,
        List<(int row, int col)> mice,
        int rows,
        int cols,
        List<string> errors)
    {
        var mouseSet = new HashSet<long>();
        foreach (var (row, col) in mice)
            mouseSet.Add(CellKey(row, col));

        foreach (var (row, col) in cats)
        {
            for (var i = 0; i < RowOffsets.Length; i++)
            {
                var nr = row + RowOffsets[i];
                var nc = col + ColOffsets[i];
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                if (mouseSet.Contains(CellKey(nr, nc)))
                {
                    errors.Add($"Cat at ({row},{col}) is adjacent to mouse at ({nr},{nc}).");
                    return;
                }
            }
        }
    }

    static long CellKey(int row, int col) => ((long)row << 32) | (uint)col;
}
