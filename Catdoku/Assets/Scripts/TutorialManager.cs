using System;
using System.Collections;
using UnityEngine;

namespace ColorCubeShooter
{
    public class TutorialManager : MonoBehaviour
    {
        public const string TutorialLevelName = "Tutorial";
        public const int MaskSortingOrder = 100;
        public const int HighlightSortingBoost = 200;

        [SerializeField] TutorialConfig _config;
        [SerializeField] TutorialPanel _panel;
        [SerializeField] SpriteRenderer _maskRenderer;
        [SerializeField] TutorialHandAnimator _handAnimator;
        [SerializeField] BoardManager _boardManager;
        [SerializeField] UIManager _uiManager;

        [Header("Timing")]
        [SerializeField] float _boardReadyDelay = 0.55f;

        int _currentStepIndex = -1;
        bool _isActive;
        Coroutine _beginRoutine;

        public bool IsActive => _isActive;
        public bool IsCompleted => TutorialProgressStore.IsCompleted;
        public int CurrentStepIndex => _currentStepIndex;
        public TutorialConfig Config => _config;

        public event Action Started;
        public event Action<int> StepChanged;
        public event Action Completed;
        public event Action CampaignStartRequested;

        void Awake()
        {
            if (_boardManager == null)
                _boardManager = FindAnyObjectByType<BoardManager>();

            if (_uiManager == null)
                _uiManager = FindAnyObjectByType<UIManager>();

            if (_panel == null)
                _panel = FindAnyObjectByType<TutorialPanel>(FindObjectsInactive.Include);

            if (_config == null)
                _config = Resources.Load<TutorialConfig>(TutorialConfig.DefaultResourcePath);

            if (_maskRenderer == null)
            {
                var mask = GameObject.Find("TutorialMask");
                if (mask != null)
                    _maskRenderer = mask.GetComponent<SpriteRenderer>();
            }

            if (_handAnimator == null)
            {
                var hand = GameObject.Find("TutorialHand");
                if (hand != null)
                    _handAnimator = hand.GetComponent<TutorialHandAnimator>();
                if (_handAnimator == null && hand != null)
                    _handAnimator = hand.AddComponent<TutorialHandAnimator>();
            }

            if (_panel != null)
                _panel.SetVisibleImmediate(false);

            BindBoardEvents();
        }

        void OnEnable()
        {
            if (_panel != null)
                _panel.UnderstandClicked += HandleUnderstandClicked;

            BindBoardEvents();
        }

        void OnDisable()
        {
            if (_panel != null)
                _panel.UnderstandClicked -= HandleUnderstandClicked;

            UnbindBoardEvents();
        }

        void BindBoardEvents()
        {
            if (_boardManager == null) return;

            _boardManager.TutorialMarkPlaced -= HandleTutorialMarkPlaced;
            _boardManager.TutorialMarkPlaced += HandleTutorialMarkPlaced;
        }

        void UnbindBoardEvents()
        {
            if (_boardManager == null) return;

            _boardManager.TutorialMarkPlaced -= HandleTutorialMarkPlaced;
        }

        public bool ShouldPlayTutorial(bool testMode)
        {
            return !testMode && !IsCompleted;
        }

        public void Begin()
        {
            if (_config == null || _config.steps == null || _config.steps.Length == 0)
            {
                Debug.LogWarning("TutorialManager: TutorialConfig has no steps.");
                CompleteTutorial();
                return;
            }

            StopBeginRoutine();
            _isActive = true;
            _currentStepIndex = -1;
            _uiManager?.SetGamePanelVisible(false);
            Started?.Invoke();
            _beginRoutine = StartCoroutine(BeginAfterBoardReady());
        }

        public void HandleCellAction(CellView cell, bool isCorrectQueen)
        {
            if (!_isActive || cell == null) return;

            var step = GetCurrentStep();
            if (step == null) return;

            if (step.stepType == TutorialStepType.Step1PlaceFirstCat)
            {
                if (!isCorrectQueen) return;
                if (cell.Row != step.targetCell.Row || cell.Col != step.targetCell.Col) return;

                _handAnimator?.StopAnimation();
                AdvanceStep();
                return;
            }

            if (step.stepType == TutorialStepType.Step4PlaceLastCat)
            {
                if (!isCorrectQueen) return;
                if (cell.Row != step.targetCell.Row || cell.Col != step.targetCell.Col) return;

                _handAnimator?.StopAnimation();
                AdvanceStep();
                return;
            }

            if (step.stepType == TutorialStepType.Step6FindLastCat)
            {
                if (!isCorrectQueen) return;
                if (cell.Row != step.targetCell.Row || cell.Col != step.targetCell.Col) return;

                AdvanceStep();
                return;
            }

            if (step.advanceMode != TutorialAdvanceMode.OnCellInteraction) return;
            if (!IsCellAllowed(step, cell.Row, cell.Col)) return;

            AdvanceStep();
        }

        void HandleTutorialMarkPlaced(CellView cell)
        {
            if (!_isActive || cell == null) return;

            var step = GetCurrentStep();
            if (step == null) return;

            if (step.stepType == TutorialStepType.Step3RowColumnRule)
            {
                if (_boardManager.IsTutorialStep3CrossMarked(step.targetCell.Col, step.targetCell.Row))
                    AdvanceStep();
                return;
            }

            if (step.stepType == TutorialStepType.Step5ExcludeAdjacent)
            {
                if (_boardManager.AreTutorialCellsMarked(step.highlightCells))
                {
                    _handAnimator?.StopAnimation();
                    AdvanceStep();
                }
            }
        }

        public bool ShouldIgnoreWrongPlacementPenalty()
        {
            if (!_isActive) return false;
            return GetCurrentStep()?.stepType == TutorialStepType.Step6FindLastCat;
        }

        public bool ShouldSuppressWinPresentation()
        {
            return _isActive;
        }

        public void HandleLevelWon()
        {
            if (!_isActive) return;

            var step = GetCurrentStep();
            if (step != null && step.advanceMode != TutorialAdvanceMode.OnLevelComplete)
                return;

            CompleteTutorial();
        }

        public void End()
        {
            StopBeginRoutine();
            _isActive = false;
            _currentStepIndex = -1;
            HideTutorialUi();
            _boardManager?.ResetTutorialVisuals();
            _boardManager?.SetInteractionEnabled(true);
            _uiManager?.SetGamePanelVisible(true);
        }

        void HandleUnderstandClicked()
        {
            if (!_isActive) return;

            var step = GetCurrentStep();
            if (step == null)
            {
                FinishTutorialAndStartCampaign();
                return;
            }

            if (step.stepType == TutorialStepType.TutorialComplete)
            {
                FinishTutorialAndStartCampaign();
                return;
            }

            if (step.advanceMode == TutorialAdvanceMode.OnUnderstandClicked
                || step.advanceMode == TutorialAdvanceMode.OnCellInteraction)
            {
                AdvanceStep();
            }
        }

        void AdvanceStep()
        {
            var nextIndex = _currentStepIndex + 1;
            if (_config.steps == null || nextIndex >= _config.steps.Length)
            {
                CompleteTutorial();
                return;
            }

            ShowStep(nextIndex);
        }

        void CompleteTutorial()
        {
            if (IsCompleted && !_isActive)
                return;

            TutorialProgressStore.MarkCompleted();
            End();
            Completed?.Invoke();
        }

        void FinishTutorialAndStartCampaign()
        {
            StopBeginRoutine();
            _isActive = false;
            _currentStepIndex = -1;
            HideTutorialUi();
            _boardManager?.ResetTutorialVisuals();
            CampaignStartRequested?.Invoke();
        }

        IEnumerator BeginAfterBoardReady()
        {
            HideTutorialUi();

            if (_boardReadyDelay > 0f)
                yield return new WaitForSeconds(_boardReadyDelay);

            _beginRoutine = null;
            AdvanceStep();
        }

        void ShowStep(int stepIndex)
        {
            if (_currentStepIndex >= 0)
                CleanupStepPresentation();

            _currentStepIndex = stepIndex;
            _uiManager?.SetGamePanelVisible(false);

            var step = GetCurrentStep();
            if (step == null)
            {
                CompleteTutorial();
                return;
            }

            if (step.stepType == TutorialStepType.Step1PlaceFirstCat)
            {
                ShowStep1PlaceFirstCat(step);
                StepChanged?.Invoke(_currentStepIndex);
                return;
            }

            if (step.stepType == TutorialStepType.Step2ColorRule)
            {
                ShowStep2ColorRule(step);
                StepChanged?.Invoke(_currentStepIndex);
                return;
            }

            if (step.stepType == TutorialStepType.Step3RowColumnRule)
            {
                ShowStep3RowColumnRule(step);
                StepChanged?.Invoke(_currentStepIndex);
                return;
            }

            if (step.stepType == TutorialStepType.Step4PlaceLastCat)
            {
                ShowStep4PlaceLastCat(step);
                StepChanged?.Invoke(_currentStepIndex);
                return;
            }

            if (step.stepType == TutorialStepType.Step5ExcludeAdjacent)
            {
                ShowStep5ExcludeAdjacent(step);
                StepChanged?.Invoke(_currentStepIndex);
                return;
            }

            if (step.stepType == TutorialStepType.Step6FindLastCat)
            {
                ShowStep6FindLastCat(step);
                StepChanged?.Invoke(_currentStepIndex);
                return;
            }

            if (step.stepType == TutorialStepType.TutorialComplete)
            {
                ShowTutorialComplete(step);
                StepChanged?.Invoke(_currentStepIndex);
                return;
            }

            ApplyMask(step.showMask);
            ApplyCellInteraction(step);
            ShowStandardPanel(step);
            StepChanged?.Invoke(_currentStepIndex);
        }

        void ShowStep1PlaceFirstCat(TutorialStep step)
        {
            _uiManager?.SetGamePanelVisible(false);
            _boardManager?.ResetTutorialVisuals();
            ApplyMask(true, MaskSortingOrder);
            _boardManager?.ApplyTutorialStep1State(
                step.targetCell.Col,
                step.targetCell.Row,
                HighlightSortingBoost);

            var message = string.IsNullOrEmpty(step.boardMessage)
                ? "Double tap to place a cat on the cell."
                : step.boardMessage;

            _panel?.ShowTutorialStep(message, showTextBoard: true, showUnderstandButton: false);

            var handPosition = _boardManager.GetCellWorldPosition(step.targetCell.Row, step.targetCell.Col);
            _handAnimator?.PlayAt(handPosition);
        }

        void ShowStep2ColorRule(TutorialStep step)
        {
            _uiManager?.SetGamePanelVisible(false);
            ApplyMask(false);
            _handAnimator?.StopAnimation();
            _boardManager?.ApplyTutorialStep2State();

            var message = string.IsNullOrEmpty(step.boardMessage)
                ? "Well done!!!\nEach <color=#8B3A2A>color</color> has only one cat."
                : step.boardMessage;

            var label = string.IsNullOrEmpty(step.understandLabel)
                ? _config.defaultUnderstandLabel
                : step.understandLabel;

            _panel?.ShowTutorialStep(message, showTextBoard: true, showUnderstandButton: true, label);
        }

        void ShowStep3RowColumnRule(TutorialStep step)
        {
            _uiManager?.SetGamePanelVisible(false);
            _handAnimator?.StopAnimation();
            ApplyMask(true, MaskSortingOrder);
            _boardManager?.ApplyTutorialStep3State(
                step.targetCell.Col,
                step.targetCell.Row,
                HighlightSortingBoost);

            var message = string.IsNullOrEmpty(step.boardMessage)
                ? "Great job!\nCats cannot be in the <color=#8B3A2A>same row or column</color>.\nTap empty cells to mark them with an X."
                : step.boardMessage;

            _panel?.ShowTutorialStep(message, showTextBoard: true, showUnderstandButton: false);
        }

        void ShowStep4PlaceLastCat(TutorialStep step)
        {
            _uiManager?.SetGamePanelVisible(false);
            ApplyMask(true, MaskSortingOrder);
            _boardManager?.ApplyTutorialStep4State(
                step.highlightCells,
                step.targetCell.Col,
                step.targetCell.Row,
                HighlightSortingBoost);

            var message = string.IsNullOrEmpty(step.boardMessage)
                ? "Only one <color=#3A9E4F>green cell</color> left.\n<color=#8B3A2A>Double tap to place the cat.</color>"
                : step.boardMessage;

            _panel?.ShowTutorialStep(message, showTextBoard: true, showUnderstandButton: false);

            var handPosition = _boardManager.GetCellWorldPosition(step.targetCell.Row, step.targetCell.Col);
            _handAnimator?.PlayAt(handPosition);
        }

        void ShowStep5ExcludeAdjacent(TutorialStep step)
        {
            _uiManager?.SetGamePanelVisible(false);
            ApplyMask(true, MaskSortingOrder);
            _boardManager?.ApplyTutorialStep5State(step.highlightCells, HighlightSortingBoost);

            var message = string.IsNullOrEmpty(step.boardMessage)
                ? "Cats cannot touch each other's <color=#8B3A2A>edges or corners</color>.\nSwipe through all these cells to exclude them."
                : step.boardMessage;

            _panel?.ShowTutorialStep(message, showTextBoard: true, showUnderstandButton: false);

            var handPosition = _boardManager.GetCellWorldPosition(step.handCell.Row, step.handCell.Col);
            _handAnimator?.PlaySingleTapAt(handPosition);
        }

        void ShowStep6FindLastCat(TutorialStep step)
        {
            _uiManager?.SetGamePanelVisible(false);
            _handAnimator?.StopAnimation();
            ApplyMask(false);

            var targetCol = step.targetCell.Col;
            var targetRow = step.targetCell.Row;
            var level = _boardManager.CurrentLevel;
            var lastColorId = level != null ? level.GetColorAt(targetRow, targetCol) : 0;

            _boardManager?.RevealTutorialCatsExcept(targetCol, targetRow);
            _boardManager?.ApplyTutorialStep6State(lastColorId);

            var message = string.IsNullOrEmpty(step.boardMessage)
                ? "Find the <color=#8B3A2A>last cat</color>!"
                : step.boardMessage;

            _panel?.ShowTutorialStep(message, showTextBoard: true, showUnderstandButton: false);
        }

        void ShowTutorialComplete(TutorialStep step)
        {
            TutorialProgressStore.MarkCompleted();

            _uiManager?.SetGamePanelVisible(false);
            _handAnimator?.StopAnimation();
            ApplyMask(false);
            _boardManager?.ApplyTutorialStep2State();

            var message = string.IsNullOrEmpty(step.boardMessage)
                ? "You understand the rules now."
                : step.boardMessage;

            var label = string.IsNullOrEmpty(step.understandLabel)
                ? "Let's Play"
                : step.understandLabel;

            _panel?.ShowTutorialStep(message, showTextBoard: true, showUnderstandButton: true, label);
        }

        void ShowStandardPanel(TutorialStep step)
        {
            if (_panel == null) return;

            ApplyMask(false);
            _handAnimator?.StopAnimation();
            _boardManager?.ResetTutorialVisuals();
            _boardManager?.SetInteractionEnabled(true);

            var label = string.IsNullOrEmpty(step.understandLabel)
                ? _config.defaultUnderstandLabel
                : step.understandLabel;

            _panel.SetContent(step.boardMessage, label, step.showUnderstandButton);
            _panel.Show();
        }

        void ApplyMask(bool visible, int sortingOrder = MaskSortingOrder)
        {
            if (_maskRenderer == null) return;

            _maskRenderer.gameObject.SetActive(visible);
            if (visible)
                _maskRenderer.sortingOrder = sortingOrder;
        }

        void ApplyCellInteraction(TutorialStep step)
        {
            if (_boardManager == null) return;

            if (step.interactableCells == null || step.interactableCells.Length == 0)
            {
                _boardManager.SetInteractionEnabled(true);
                return;
            }

            var allowed = new Vector2Int[step.interactableCells.Length];
            for (var i = 0; i < step.interactableCells.Length; i++)
                allowed[i] = step.interactableCells[i].ToVector2Int();

            _boardManager.SetOnlyCellsInteractable(allowed);
        }

        bool IsCellAllowed(TutorialStep step, int row, int col)
        {
            if (step.interactableCells == null || step.interactableCells.Length == 0)
                return true;

            for (var i = 0; i < step.interactableCells.Length; i++)
            {
                var cell = step.interactableCells[i];
                if (cell.Row == row && cell.Col == col)
                    return true;
            }

            return false;
        }

        TutorialStep GetCurrentStep()
        {
            if (_config?.steps == null) return null;
            if (_currentStepIndex < 0 || _currentStepIndex >= _config.steps.Length)
                return null;

            return _config.steps[_currentStepIndex];
        }

        void HideTutorialUi()
        {
            CleanupStepPresentation();
            _panel?.Hide();
        }

        void CleanupStepPresentation()
        {
            ApplyMask(false);
            _handAnimator?.StopAnimation();
        }

        void StopBeginRoutine()
        {
            if (_beginRoutine == null) return;

            StopCoroutine(_beginRoutine);
            _beginRoutine = null;
        }
    }
}
