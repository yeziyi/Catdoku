using UnityEngine;

public class FindCatQBooster : MonoBehaviour
{
    [SerializeField] BoardManager _boardManager;

    int _usedCount;

    public int UsedCount => _usedCount;

    void Awake()
    {
        if (_boardManager == null)
            _boardManager = GetComponent<BoardManager>();
    }

    public void Reset()
    {
        _usedCount = 0;
    }

    public bool TryRevealCat()
    {
        var boardView = _boardManager?.BoardView;
        var level = _boardManager?.CurrentLevel;
        if (boardView == null)
        {
            Debug.LogWarning("FindCatQBooster: BoardView is missing.");
            return false;
        }

        if (level == null)
        {
            Debug.LogWarning("FindCatQBooster: Level data is missing.");
            return false;
        }

        var target = FindBestQueenTarget(boardView, level, out var reason);
        if (target == null)
        {
            Debug.Log("FindCatQBooster: No unrevealed cat Q on board.");
            return false;
        }

        target.ApplyHintPlaceQueen();
        _usedCount++;
        Debug.Log($"FindCatQBooster: Revealed cat at ({target.Row},{target.Col}) — {reason}. Uses: {_usedCount}");
        return true;
    }

    static CellView FindBestQueenTarget(BoardView boardView, LevelData level, out string reason)
    {
        var uniqueColorQueen = HintBooster.FindUniqueColorUnrevealedQueen(boardView, level);
        if (uniqueColorQueen != null)
        {
            reason = $"unique color {level.GetColorAt(uniqueColorQueen.Row, uniqueColorQueen.Col)} on board";
            return uniqueColorQueen;
        }

        var forcedQueen = HintBooster.FindForcedQueenByOpenColor(boardView, level);
        if (forcedQueen != null)
        {
            reason = $"only 1 open cell left for color {level.GetColorAt(forcedQueen.Row, forcedQueen.Col)}";
            return forcedQueen;
        }

        var fallbackQueen = HintBooster.FindAnyUnrevealedQueen(boardView);
        if (fallbackQueen != null)
        {
            reason = "fallback unrevealed queen";
            return fallbackQueen;
        }

        reason = null;
        return null;
    }
}
