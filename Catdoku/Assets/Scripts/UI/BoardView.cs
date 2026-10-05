using ColorCubeShooter;
using DG.Tweening;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    const string DefaultCellPrefabPath = "GameObjects/Cell";
    const string DefaultBoardBgSpritePath = "Images/GameView/Bg9SlicedTipsTxt";

    [SerializeField] Transform _cellsRoot;
    [SerializeField] CellView _cellPrefab;
    [SerializeField] Sprite _boardBackgroundSprite;
    [SerializeField] float _cellSize = 1f;
    [SerializeField] float _cellGap = 0.02f;
    [SerializeField] float _boardPadding = 0.38f;
    [SerializeField] Color _boardBackgroundColor = new(0.9f, 0.91f, 0.94f, 1f);
    [SerializeField] int _boardBackgroundSortingOrder = -5;
    [SerializeField] bool _fitCamera = true;
    [SerializeField] float _cameraPadding = 0.75f;
    [SerializeField] bool _playSpawnAnimation = true;
    [SerializeField] float _cellSpawnDuration = 0.22f;
    [SerializeField] float _cellSpawnStagger = 0.028f;
    [SerializeField] float _boardSpawnDuration = 0.28f;

    SpriteRenderer _boardBackgroundRenderer;
    Tween _boardSpawnTween;
    CellView[,] _cells;
    LevelData _level;

    public int RowCount => _level?.RowCount ?? 0;
    public int ColCount => _level?.ColCount ?? 0;

    public CellView GetCell(int row, int col)
    {
        if (_cells == null || row < 0 || row >= _cells.GetLength(0)) return null;
        if (col < 0 || col >= _cells.GetLength(1)) return null;
        return _cells[row, col];
    }

    public void Build(LevelData level)
    {
        if (level == null || level.RowCount == 0 || level.ColCount == 0)
        {
            Debug.LogWarning("BoardView: Invalid level data.");
            return;
        }

        EnsureReferences();
        Clear();

        _level = level;
        _cells = new CellView[level.RowCount, level.ColCount];

        var step = _cellSize + _cellGap;
        var targetCellScale = Vector3.one * _cellSize;

        for (var row = 0; row < level.RowCount; row++)
        {
            for (var col = 0; col < level.ColCount; col++)
            {
                var cell = Instantiate(_cellPrefab, _cellsRoot);
                cell.name = $"Cell_{row}_{col}";
                cell.transform.localPosition = GetCellLocalPosition(row, col, level.RowCount, level.ColCount, step);
                cell.transform.localScale = _playSpawnAnimation ? Vector3.zero : targetCellScale;

                var colorId = level.GetColorAt(row, col);
                var isSolutionQueen = level.HasQueenAt(row, col);
                var catRevealed = level.IsCatRevealedAt(row, col);
                cell.Initialize(row, col, LevelPalette.GetColor(colorId), isSolutionQueen, catRevealed);
                _cells[row, col] = cell;
            }
        }

        UpdateBoardBackground(level.RowCount, level.ColCount, step);

        if (_playSpawnAnimation)
            PlayDominoSpawnAnimation(level.RowCount, level.ColCount, targetCellScale);
        else
            SetAllCellsInteraction(true);

        if (_fitCamera)
            FitCamera(level.RowCount, level.ColCount, step);
    }

    public void Clear()
    {
        KillSpawnTweens();

        if (_cellsRoot != null)
        {
            for (var i = _cellsRoot.childCount - 1; i >= 0; i--)
                Destroy(_cellsRoot.GetChild(i).gameObject);
        }

        if (_boardBackgroundRenderer != null)
            _boardBackgroundRenderer.enabled = false;

        _cells = null;
        _level = null;
    }

    void EnsureReferences()
    {
        if (_cellsRoot == null)
        {
            var cellsGo = transform.Find("Cells");
            if (cellsGo == null)
            {
                cellsGo = new GameObject("Cells").transform;
                cellsGo.SetParent(transform, false);
            }
            _cellsRoot = cellsGo;
        }

        if (_cellPrefab == null)
        {
            var prefabGo = Resources.Load<GameObject>(DefaultCellPrefabPath);
            if (prefabGo != null)
                _cellPrefab = prefabGo.GetComponent<CellView>();
        }

        if (_cellPrefab == null)
            Debug.LogError("BoardView: Cell prefab is not assigned.");

        EnsureBoardBackground();
    }

    void EnsureBoardBackground()
    {
        if (_boardBackgroundRenderer != null) return;

        var existing = transform.Find("BoardBackground");
        if (existing != null)
        {
            _boardBackgroundRenderer = existing.GetComponent<SpriteRenderer>();
            if (_boardBackgroundRenderer != null) return;
        }

        var backgroundGo = new GameObject("BoardBackground");
        backgroundGo.transform.SetParent(transform, false);
        backgroundGo.transform.SetSiblingIndex(0);

        _boardBackgroundRenderer = backgroundGo.AddComponent<SpriteRenderer>();
        if (_boardBackgroundSprite == null)
            _boardBackgroundSprite = Resources.Load<Sprite>(DefaultBoardBgSpritePath);
        _boardBackgroundRenderer.sprite = _boardBackgroundSprite;
        _boardBackgroundRenderer.sortingOrder = _boardBackgroundSortingOrder;
    }

    void UpdateBoardBackground(int rowCount, int colCount, float step)
    {
        if (_boardBackgroundRenderer == null || _boardBackgroundRenderer.sprite == null)
            return;

        var contentSize = GetBoardContentSize(rowCount, colCount, step);
        var paddedSize = contentSize + Vector2.one * (_boardPadding * 2f);

        _boardBackgroundRenderer.enabled = true;
        _boardBackgroundRenderer.color = _boardBackgroundColor;
        _boardBackgroundRenderer.drawMode = SpriteDrawMode.Sliced;
        _boardBackgroundRenderer.size = paddedSize;
        _boardBackgroundRenderer.transform.localPosition = Vector3.zero;

        if (_playSpawnAnimation)
            PlayBoardBackgroundSpawn();
        else
            _boardBackgroundRenderer.transform.localScale = Vector3.one;
    }

    void PlayBoardBackgroundSpawn()
    {
        var backgroundTransform = _boardBackgroundRenderer.transform;
        backgroundTransform.DOKill();
        backgroundTransform.localScale = Vector3.zero;
        _boardSpawnTween = backgroundTransform
            .DOScale(Vector3.one, _boardSpawnDuration)
            .SetEase(Ease.OutQuad);
    }

    void PlayDominoSpawnAnimation(int rowCount, int colCount, Vector3 targetScale)
    {
        SetAllCellsInteraction(false);

        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                var cell = _cells[row, col];
                if (cell == null) continue;

                var dominoOrder = GetDominoOrder(row, col, rowCount);
                var delay = dominoOrder * _cellSpawnStagger;
                var cellTransform = cell.transform;
                var collider = cell.GetComponent<BoxCollider2D>();

                cellTransform.DOKill();
                cellTransform.localScale = Vector3.zero;
                cellTransform
                    .DOScale(targetScale, _cellSpawnDuration)
                    .SetDelay(delay)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        if (collider != null)
                            collider.enabled = true;
                    });
            }
        }
    }

    static int GetDominoOrder(int row, int col, int rowCount)
    {
        // Bottom-left (max row, col 0) first → top-right (row 0, max col) last.
        return rowCount - 1 - row + col;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        SetAllCellsInteraction(enabled);
    }

    public void SetCellInteractionEnabled(int row, int col, bool enabled)
    {
        var cell = GetCell(row, col);
        if (cell == null) return;

        var collider = cell.GetComponent<BoxCollider2D>();
        if (collider != null)
            collider.enabled = enabled;
    }

    public void SetOnlyCellsInteractable(System.Collections.Generic.IReadOnlyList<Vector2Int> allowedCells)
    {
        if (_cells == null) return;

        var allowed = allowedCells != null && allowedCells.Count > 0;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                var isAllowed = !allowed;

                if (allowed)
                {
                    for (var i = 0; i < allowedCells.Count; i++)
                    {
                        if (allowedCells[i].y == row && allowedCells[i].x == col)
                        {
                            isAllowed = true;
                            break;
                        }
                    }
                }

                SetCellInteractionEnabled(row, col, isAllowed);
            }
        }
    }

    public void ResetTutorialVisuals()
    {
        if (_cells == null) return;

        foreach (var cell in _cells)
            cell?.ResetTutorialState();
    }

    public void ApplyTutorialStep1State(int targetCol, int targetRow, int highlightSortingBoost)
    {
        if (_cells == null) return;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                var cell = _cells[row, col];
                if (cell == null) continue;

                var isTarget = col == targetCol && row == targetRow;
                cell.SetTutorialLocked(!isTarget);
                cell.SetInputMode(isTarget ? CellInputMode.DoubleClickOnly : CellInputMode.Normal);
                cell.SetSortingOrderBoost(isTarget ? highlightSortingBoost : 0);
            }
        }
    }

    public void ApplyTutorialStep2State()
    {
        if (_cells == null) return;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                var cell = _cells[row, col];
                if (cell == null) continue;

                cell.SetSortingOrderBoost(0);
                cell.SetInputMode(CellInputMode.Normal);
                cell.SetTutorialLocked(true);
            }
        }
    }

    public void ApplyTutorialStep3State(int anchorCol, int anchorRow, int highlightSortingBoost)
    {
        if (_cells == null) return;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                var cell = _cells[row, col];
                if (cell == null) continue;

                var inCross = row == anchorRow || col == anchorCol;
                var isCatCell = row == anchorRow && col == anchorCol;

                if (!inCross)
                {
                    cell.SetSortingOrderBoost(0);
                    cell.SetInputMode(CellInputMode.Normal);
                    cell.SetTutorialLocked(true);
                    continue;
                }

                cell.SetSortingOrderBoost(highlightSortingBoost);

                if (isCatCell)
                {
                    cell.SetInputMode(CellInputMode.Normal);
                    cell.SetTutorialLocked(true);
                    continue;
                }

                cell.SetInputMode(CellInputMode.TutorialMarkOnly);
                cell.SetTutorialLocked(false);
            }
        }
    }

    public bool IsTutorialStep3CrossMarked(int anchorCol, int anchorRow)
    {
        if (_cells == null) return false;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                if (row != anchorRow && col != anchorCol) continue;

                var cell = _cells[row, col];
                if (cell == null) continue;
                if (row == anchorRow && col == anchorCol) continue;
                if (cell.IsCatVisible) continue;
                if (cell.State != CellMarkState.CrossWhite)
                    return false;
            }
        }

        return true;
    }

    public void ApplyTutorialStep4State(
        TutorialCell[] visibleCells,
        int targetCol,
        int targetRow,
        int highlightSortingBoost)
    {
        if (_cells == null) return;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                var cell = _cells[row, col];
                if (cell == null) continue;

                if (!IsTutorialCellListed(visibleCells, col, row))
                {
                    cell.SetSortingOrderBoost(0);
                    cell.SetInputMode(CellInputMode.Normal);
                    cell.SetTutorialLocked(true);
                    continue;
                }

                cell.SetSortingOrderBoost(highlightSortingBoost);

                var isTarget = col == targetCol && row == targetRow;
                if (isTarget)
                {
                    cell.SetInputMode(CellInputMode.DoubleClickOnly);
                    cell.SetTutorialLocked(false);
                    continue;
                }

                cell.SetInputMode(CellInputMode.Normal);
                cell.SetTutorialLocked(true);
            }
        }
    }

    static bool IsTutorialCellListed(TutorialCell[] cells, int col, int row)
    {
        if (cells == null) return false;

        for (var i = 0; i < cells.Length; i++)
        {
            if (cells[i].Col == col && cells[i].Row == row)
                return true;
        }

        return false;
    }

    public void ApplyTutorialStep5State(TutorialCell[] markableCells, int highlightSortingBoost)
    {
        if (_cells == null) return;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                var cell = _cells[row, col];
                if (cell == null) continue;

                if (!IsTutorialCellListed(markableCells, col, row))
                {
                    cell.SetSortingOrderBoost(0);
                    cell.SetInputMode(CellInputMode.Normal);
                    cell.SetTutorialLocked(true);
                    continue;
                }

                cell.SetSortingOrderBoost(highlightSortingBoost);
                cell.SetInputMode(CellInputMode.TutorialMarkOnly);
                cell.SetTutorialLocked(false);
            }
        }
    }

    public bool AreTutorialCellsMarked(TutorialCell[] cells)
    {
        if (_cells == null || cells == null) return false;

        for (var i = 0; i < cells.Length; i++)
        {
            var cell = GetCell(cells[i].Row, cells[i].Col);
            if (cell == null || cell.State != CellMarkState.CrossWhite)
                return false;
        }

        return cells.Length > 0;
    }

    public void RevealTutorialCatsExcept(LevelData level, int exceptCol, int exceptRow)
    {
        if (_cells == null || level == null) return;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                if (col == exceptCol && row == exceptRow) continue;
                if (!level.HasQueenAt(row, col)) continue;

                _cells[row, col]?.RevealCatSilently();
            }
        }
    }

    public void ApplyTutorialStep6State(LevelData level, int lastColorId)
    {
        if (_cells == null || level == null) return;

        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var col = 0; col < _cells.GetLength(1); col++)
            {
                var cell = _cells[row, col];
                if (cell == null) continue;

                cell.SetSortingOrderBoost(0);

                if (cell.IsCatVisible)
                {
                    cell.SetInputMode(CellInputMode.Normal);
                    cell.SetTutorialLocked(true);
                    continue;
                }

                if (cell.State == CellMarkState.CrossWhite || cell.State == CellMarkState.CrossWrong)
                {
                    cell.SetInputMode(CellInputMode.Normal);
                    cell.SetTutorialLocked(true);
                    continue;
                }

                if (level.GetColorAt(row, col) == lastColorId)
                {
                    cell.SetInputMode(CellInputMode.Step6LastColor);
                    cell.SetTutorialLocked(false);
                    continue;
                }

                cell.SetInputMode(CellInputMode.TutorialMarkOnly);
                cell.SetTutorialLocked(false);
            }
        }
    }

    public Vector3 GetCellWorldPosition(int row, int col)
    {
        var cell = GetCell(row, col);
        return cell != null ? cell.transform.position : transform.position;
    }

    void SetAllCellsInteraction(bool enabled)
    {
        if (_cells == null) return;

        foreach (var cell in _cells)
        {
            if (cell == null) continue;
            var collider = cell.GetComponent<BoxCollider2D>();
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    void KillSpawnTweens()
    {
        _boardSpawnTween?.Kill();
        _boardSpawnTween = null;

        if (_boardBackgroundRenderer != null)
            _boardBackgroundRenderer.transform.DOKill();

        if (_cellsRoot == null) return;

        for (var i = 0; i < _cellsRoot.childCount; i++)
            _cellsRoot.GetChild(i).DOKill();
    }

    static Vector3 GetCellLocalPosition(int row, int col, int rowCount, int colCount, float step)
    {
        var totalWidth = (colCount - 1) * step;
        var totalHeight = (rowCount - 1) * step;
        var x = col * step - totalWidth * 0.5f;
        var y = (rowCount - 1 - row) * step - totalHeight * 0.5f;
        return new Vector3(x, y, 0f);
    }

    Vector2 GetBoardContentSize(int rowCount, int colCount, float step)
    {
        var width = colCount > 0 ? (colCount - 1) * step + _cellSize : 0f;
        var height = rowCount > 0 ? (rowCount - 1) * step + _cellSize : 0f;
        return new Vector2(width, height);
    }

    void FitCamera(int rowCount, int colCount, float step)
    {
        var camera = Camera.main;
        if (camera == null || !camera.orthographic) return;

        var contentSize = GetBoardContentSize(rowCount, colCount, step);
        var boardWidth = contentSize.x + _boardPadding * 2f;
        var boardHeight = contentSize.y + _boardPadding * 2f;
        var sizeByHeight = boardHeight * 0.5f + _cameraPadding;
        var sizeByWidth = boardWidth / (2f * camera.aspect) + _cameraPadding;
        camera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}
