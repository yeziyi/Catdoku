using System;
using System.Collections.Generic;
using UnityEngine;

public enum HintBoosterMode
{
    HintPlan,
    Algorithm
}

public class HintBooster : MonoBehaviour
{
    [SerializeField] BoardManager _boardManager;
    [SerializeField] HintBoosterMode _mode = HintBoosterMode.Algorithm;

    int _nextHintIndex;
    int _algorithmHintUses;

    public HintBoosterMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public int UsedHintCount => _mode == HintBoosterMode.HintPlan ? _nextHintIndex : _algorithmHintUses;

    public int RemainingHintCount
    {
        get
        {
            if (_mode != HintBoosterMode.HintPlan) return -1;

            var hints = _boardManager?.CurrentLevel?.hintPlan;
            if (hints == null) return 0;
            return Mathf.Max(0, hints.Length - _nextHintIndex);
        }
    }

    public event Action<HintStep> HintApplied;
    public event Action HintsExhausted;

    void Awake()
    {
        if (_boardManager == null)
            _boardManager = GetComponent<BoardManager>();
    }

    public void Reset()
    {
        _nextHintIndex = 0;
        _algorithmHintUses = 0;
    }

    public bool TryUseHint()
    {
        var boardView = _boardManager?.BoardView;
        if (boardView == null)
        {
            Debug.LogWarning("HintBooster: BoardView is missing.");
            return false;
        }

        return _mode == HintBoosterMode.HintPlan
            ? TryUseHintPlan(boardView)
            : TryUseAlgorithmHint(boardView);
    }

    bool TryUseHintPlan(BoardView boardView)
    {
        var level = _boardManager?.CurrentLevel;
        var hints = level?.hintPlan;
        if (hints == null || hints.Length == 0 || _nextHintIndex >= hints.Length)
        {
            HintsExhausted?.Invoke();
            Debug.Log("HintBooster: No hints remaining in hintPlan.");
            return false;
        }

        var step = hints[_nextHintIndex++];
        ApplyHintPlanStep(boardView, step);
        HintApplied?.Invoke(step);
        Debug.Log($"HintBooster[Plan]: [{step.rule}] {step.action} — {step.reason} ({RemainingHintCount} left)");
        return true;
    }

    bool TryUseAlgorithmHint(BoardView boardView)
    {
        var level = _boardManager?.CurrentLevel;
        if (level == null) return false;

        var revealedQueens = CollectRevealedQueens(boardView);
        if (revealedQueens.Count == 0)
            return ApplyAlgorithmPlaceQueen(boardView, level, "No cat on board");

        var missingEliminations = CollectMissingEliminations(boardView, revealedQueens);
        if (missingEliminations.Count > 0)
        {
            foreach (var cell in missingEliminations)
                cell.ApplyHintEliminate();

            _algorithmHintUses++;
            Debug.Log($"HintBooster[Algo]: Marked {missingEliminations.Count} cell(s) with X.");
            return true;
        }

        return ApplyAlgorithmPlaceQueen(boardView, level, "Eliminations complete for current cats");
    }

    bool ApplyAlgorithmPlaceQueen(BoardView boardView, LevelData level, string reason)
    {
        var forced = FindForcedQueenByOpenColor(boardView, level);
        if (forced != null)
        {
            forced.ApplyHintPlaceQueen();
            _algorithmHintUses++;
            var colorId = level.GetColorAt(forced.Row, forced.Col);
            Debug.Log($"HintBooster[Algo]: Forced cat at ({forced.Row},{forced.Col}) — color {colorId} has only 1 open cell.");
            return true;
        }

        var target = FindPriorityUnrevealedQueen(boardView, level);
        if (target == null)
        {
            HintsExhausted?.Invoke();
            Debug.Log("HintBooster[Algo]: No more queens to place.");
            return false;
        }

        target.ApplyHintPlaceQueen();
        _algorithmHintUses++;
        Debug.Log($"HintBooster[Algo]: Placed cat at ({target.Row},{target.Col}) — {reason}.");
        return true;
    }

    static List<CellView> CollectRevealedQueens(BoardView boardView)
    {
        var queens = new List<CellView>();
        for (var row = 0; row < boardView.RowCount; row++)
        {
            for (var col = 0; col < boardView.ColCount; col++)
            {
                var cell = boardView.GetCell(row, col);
                if (cell != null && cell.IsSolutionQueen && cell.IsCatVisible)
                    queens.Add(cell);
            }
        }

        return queens;
    }

    static List<CellView> CollectMissingEliminations(BoardView boardView, List<CellView> revealedQueens)
    {
        var targets = new List<CellView>();
        var seen = new HashSet<CellView>();

        foreach (var queen in revealedQueens)
        {
            foreach (var cell in GetEliminationTargets(boardView, queen.Row, queen.Col))
            {
                if (!cell.NeedsEliminationMark || !seen.Add(cell)) continue;
                targets.Add(cell);
            }
        }

        return targets;
    }

    static IEnumerable<CellView> GetEliminationTargets(BoardView boardView, int queenRow, int queenCol)
    {
        for (var col = 0; col < boardView.ColCount; col++)
        {
            if (col == queenCol) continue;
            var cell = boardView.GetCell(queenRow, col);
            if (cell != null) yield return cell;
        }

        for (var row = 0; row < boardView.RowCount; row++)
        {
            if (row == queenRow) continue;
            var cell = boardView.GetCell(row, queenCol);
            if (cell != null) yield return cell;
        }

        for (var dr = -1; dr <= 1; dr++)
        {
            for (var dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;

                var row = queenRow + dr;
                var col = queenCol + dc;
                if (row < 0 || row >= boardView.RowCount || col < 0 || col >= boardView.ColCount) continue;

                var cell = boardView.GetCell(row, col);
                if (cell != null) yield return cell;
            }
        }
    }

    static CellView FindPriorityUnrevealedQueen(BoardView boardView, LevelData level)
    {
        var unique = FindUniqueColorUnrevealedQueen(boardView, level);
        if (unique != null) return unique;
        return FindAnyUnrevealedQueen(boardView);
    }

    public static CellView FindUniqueColorUnrevealedQueen(BoardView boardView, LevelData level)
    {
        var colorCounts = BuildColorCounts(level);

        for (var row = 0; row < boardView.RowCount; row++)
        {
            for (var col = 0; col < boardView.ColCount; col++)
            {
                var cell = boardView.GetCell(row, col);
                if (cell == null || !cell.IsSolutionQueen || cell.IsCatVisible) continue;

                var colorId = level.GetColorAt(row, col);
                if (colorCounts.TryGetValue(colorId, out var count) && count == 1)
                    return cell;
            }
        }

        return null;
    }

    public static CellView FindForcedQueenByOpenColor(BoardView boardView, LevelData level)
    {
        var openByColor = new Dictionary<int, List<CellView>>();

        for (var row = 0; row < boardView.RowCount; row++)
        {
            for (var col = 0; col < boardView.ColCount; col++)
            {
                var cell = boardView.GetCell(row, col);
                if (cell == null || !IsOpenForColorDeduction(cell)) continue;

                var colorId = level.GetColorAt(row, col);
                if (!openByColor.TryGetValue(colorId, out var cells))
                {
                    cells = new List<CellView>();
                    openByColor[colorId] = cells;
                }

                cells.Add(cell);
            }
        }

        foreach (var pair in openByColor)
        {
            if (pair.Value.Count != 1) continue;

            var onlyOpenCell = pair.Value[0];
            if (onlyOpenCell.IsSolutionQueen && !onlyOpenCell.IsCatVisible)
                return onlyOpenCell;
        }

        return null;
    }

    public static bool IsOpenForColorDeduction(CellView cell)
    {
        if (cell.IsCatVisible) return false;

        return cell.State != CellMarkState.CrossWhite && cell.State != CellMarkState.CrossWrong;
    }

    public static CellView FindAnyUnrevealedQueen(BoardView boardView)
    {
        for (var row = 0; row < boardView.RowCount; row++)
        {
            for (var col = 0; col < boardView.ColCount; col++)
            {
                var cell = boardView.GetCell(row, col);
                if (cell != null && cell.IsSolutionQueen && !cell.IsCatVisible)
                    return cell;
            }
        }

        return null;
    }

    static Dictionary<int, int> BuildColorCounts(LevelData level)
    {
        var counts = new Dictionary<int, int>();
        for (var row = 0; row < level.RowCount; row++)
        {
            for (var col = 0; col < level.ColCount; col++)
            {
                var colorId = level.GetColorAt(row, col);
                counts.TryGetValue(colorId, out var count);
                counts[colorId] = count + 1;
            }
        }

        return counts;
    }

    static void ApplyHintPlanStep(BoardView boardView, HintStep step)
    {
        if (step?.cells == null || step.cells.Length == 0) return;

        switch (step.action)
        {
            case "place_queen":
                foreach (var pair in step.cells)
                {
                    if (pair == null || pair.Length < 2) continue;
                    boardView.GetCell(pair[0], pair[1])?.ApplyHintPlaceQueen();
                }
                break;

            case "eliminate":
                foreach (var pair in step.cells)
                {
                    if (pair == null || pair.Length < 2) continue;
                    boardView.GetCell(pair[0], pair[1])?.ApplyHintEliminate();
                }
                break;

            default:
                Debug.LogWarning($"HintBooster: Unsupported action '{step.action}'.");
                break;
        }
    }
}
