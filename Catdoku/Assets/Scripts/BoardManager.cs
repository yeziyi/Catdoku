using System;
using System.Collections.Generic;
using ColorCubeShooter;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] BoardView _boardView;

    LevelData _currentLevel;

    public LevelData CurrentLevel => _currentLevel;
    public BoardView BoardView => _boardView;
    public event Action<CellView, bool> CellPlacementResolved;
    public event Action<CellView> TutorialMarkPlaced;

    void Awake()
    {
        if (_boardView == null)
            _boardView = GetComponent<BoardView>();
    }

    public bool LoadLevel(string levelName)
    {
        UnsubscribeCells();
        _currentLevel = LevelLoader.LoadFromResources(levelName);

        if (_currentLevel == null || _currentLevel.RowCount == 0 || _currentLevel.ColCount == 0)
        {
            Debug.LogError($"BoardManager: Failed to load level '{levelName}'.");
            return false;
        }

        if (_boardView == null)
        {
            Debug.LogError("BoardManager: BoardView is not assigned.");
            return false;
        }

        _boardView.Build(_currentLevel);
        SubscribeCells();
        return true;
    }

    public void ClearBoard()
    {
        UnsubscribeCells();
        _boardView?.Clear();
        _currentLevel = null;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        _boardView?.SetInteractionEnabled(enabled);
    }

    public void SetOnlyCellsInteractable(IReadOnlyList<Vector2Int> allowedCells)
    {
        _boardView?.SetOnlyCellsInteractable(allowedCells);
    }

    public void ResetTutorialVisuals()
    {
        CellView.ResetTutorialSwipe();
        _boardView?.ResetTutorialVisuals();
    }

    public void ApplyTutorialStep1State(int targetCol, int targetRow, int highlightSortingBoost)
    {
        _boardView?.ApplyTutorialStep1State(targetCol, targetRow, highlightSortingBoost);
    }

    public void ApplyTutorialStep2State()
    {
        _boardView?.ApplyTutorialStep2State();
    }

    public void ApplyTutorialStep3State(int anchorCol, int anchorRow, int highlightSortingBoost)
    {
        _boardView?.ApplyTutorialStep3State(anchorCol, anchorRow, highlightSortingBoost);
    }

    public bool IsTutorialStep3CrossMarked(int anchorCol, int anchorRow)
    {
        return _boardView != null && _boardView.IsTutorialStep3CrossMarked(anchorCol, anchorRow);
    }

    public void ApplyTutorialStep4State(
        TutorialCell[] visibleCells,
        int targetCol,
        int targetRow,
        int highlightSortingBoost)
    {
        _boardView?.ApplyTutorialStep4State(visibleCells, targetCol, targetRow, highlightSortingBoost);
    }

    public void ApplyTutorialStep5State(TutorialCell[] markableCells, int highlightSortingBoost)
    {
        _boardView?.ApplyTutorialStep5State(markableCells, highlightSortingBoost);
    }

    public bool AreTutorialCellsMarked(TutorialCell[] cells)
    {
        return _boardView != null && _boardView.AreTutorialCellsMarked(cells);
    }

    public void RevealTutorialCatsExcept(int exceptCol, int exceptRow)
    {
        _boardView?.RevealTutorialCatsExcept(CurrentLevel, exceptCol, exceptRow);
    }

    public void ApplyTutorialStep6State(int lastColorId)
    {
        _boardView?.ApplyTutorialStep6State(CurrentLevel, lastColorId);
    }

    public Vector3 GetCellWorldPosition(int row, int col)
    {
        return _boardView != null
            ? _boardView.GetCellWorldPosition(row, col)
            : Vector3.zero;
    }

    void SubscribeCells()
    {
        if (_boardView == null) return;

        for (var row = 0; row < _boardView.RowCount; row++)
        {
            for (var col = 0; col < _boardView.ColCount; col++)
            {
                var cell = _boardView.GetCell(row, col);
                if (cell == null) continue;
                cell.PlacementResolved += HandleCellPlacementResolved;
                cell.TutorialMarkPlaced += HandleTutorialMarkPlaced;
            }
        }
    }

    void UnsubscribeCells()
    {
        if (_boardView == null) return;

        for (var row = 0; row < _boardView.RowCount; row++)
        {
            for (var col = 0; col < _boardView.ColCount; col++)
            {
                var cell = _boardView.GetCell(row, col);
                if (cell == null) continue;
                cell.PlacementResolved -= HandleCellPlacementResolved;
                cell.TutorialMarkPlaced -= HandleTutorialMarkPlaced;
            }
        }
    }

    void HandleCellPlacementResolved(CellView cell, bool isCorrectQueen)
    {
        CellPlacementResolved?.Invoke(cell, isCorrectQueen);
    }

    void HandleTutorialMarkPlaced(CellView cell)
    {
        TutorialMarkPlaced?.Invoke(cell);
    }
}
