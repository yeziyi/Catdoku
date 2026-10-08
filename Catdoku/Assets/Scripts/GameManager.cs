using ColorCubeShooter;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public const int MaxLives = 3;
    public const int PointsPerCat = 100;

    [SerializeField] UIManager _uiManager;
    [SerializeField] BoardManager _boardManager;
    [SerializeField] TutorialManager _tutorialManager;
    [SerializeField] HintBooster _hintBooster;
    [SerializeField] FindCatQBooster _findCatQBooster;

    [Header("Level")]
    [SerializeField] int _startLevelNumber = 1;
    [SerializeField] bool _testMode;
    [SerializeField] int _levelTestIndex = 1;

    [Header("Win")]
    [SerializeField] float _winBoardHoldDelay = 0.45f;

    int _currentLevelNumber;
    int _activeLevelNumber;
    int _lives;
    int _score;
    int _catsCollected;
    int _totalCats;
    int _miceCollected;
    int _totalMice;
    bool _isPlaying;
    Tween _pendingWinTween;
    readonly BoosterChargeStore _boosterCharges = new();

    public BoardManager BoardManager => _boardManager;
    public bool TestMode => _testMode;
    public int CurrentLevelNumber => _activeLevelNumber;
    public int CampaignLevelNumber => _currentLevelNumber;
    public int Lives => _lives;
    public int Score => _score;
    public int CatsCollected => _catsCollected;
    public int TotalCats => _totalCats;

    void Awake()
    {
        Application.targetFrameRate = 60;
        if (_uiManager == null)
            _uiManager = FindAnyObjectByType<UIManager>();
        if (_boardManager == null)
            _boardManager = FindAnyObjectByType<BoardManager>();
        if (_hintBooster == null)
            _hintBooster = FindAnyObjectByType<HintBooster>();
        if (_findCatQBooster == null)
            _findCatQBooster = FindAnyObjectByType<FindCatQBooster>();
        if (_tutorialManager == null)
            _tutorialManager = FindAnyObjectByType<TutorialManager>();
    }

    void Update()
    {
        if (!_isPlaying) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.hKey.wasPressedThisFrame)
            TryUseHintBooster();

        if (keyboard.fKey.wasPressedThisFrame)
            TryUseFindCatBooster();
    }

    void Start()
    {
        _currentLevelNumber = _testMode
            ? Mathf.Max(1, _startLevelNumber)
            : CampaignProgressStore.GetCurrentLevel();
        BindUiEvents();
        BindTutorialEvents();
        _uiManager?.Initialize();
    }

    void BindTutorialEvents()
    {
        if (_tutorialManager == null) return;

        _tutorialManager.CampaignStartRequested -= HandleTutorialCampaignStartRequested;
        _tutorialManager.CampaignStartRequested += HandleTutorialCampaignStartRequested;
    }

    void UnbindTutorialEvents()
    {
        if (_tutorialManager == null) return;

        _tutorialManager.CampaignStartRequested -= HandleTutorialCampaignStartRequested;
    }

    void HandleTutorialCampaignStartRequested()
    {
        _currentLevelNumber = 1;
        if (!_testMode)
            CampaignProgressStore.SetCurrentLevel(1);
        StartLevel(1);
    }

    void OnDestroy()
    {
        KillPendingWinTween();
        UnbindUiEvents();
        UnbindTutorialEvents();
        if (_boardManager != null)
            _boardManager.CellPlacementResolved -= HandleCellPlacementResolved;
    }

    void BindUiEvents()
    {
        if (_uiManager == null) return;

        var home = _uiManager.HomePanel;
        if (home != null)
        {
            home.PlayClicked += HandlePlayClicked;
            home.SettingClicked += HandleSettingClicked;
        }

        var game = _uiManager.GamePanel;
        if (game != null)
        {
            game.BackToHomeClicked += HandleBackToHomeClicked;
            game.SettingClicked += HandleSettingClicked;
            game.HintBoosterUseRequested += HandleHintBoosterUseRequested;
            game.HintBoosterAdRequested += HandleHintBoosterAdRequested;
            game.FindCatBoosterUseRequested += HandleFindCatBoosterUseRequested;
            game.FindCatBoosterAdRequested += HandleFindCatBoosterAdRequested;
        }

        var win = _uiManager.WinPanel;
        if (win != null)
            win.NextLevelClicked += HandleNextLevelClicked;

        var lose = _uiManager.LosePanel;
        if (lose != null)
            lose.ReplayClicked += HandleReplayClicked;

        var setting = _uiManager.SettingPopup;
        if (setting != null)
        {
            setting.Closed += HandleSettingClosed;
            setting.ReplayClicked += HandleSettingReplayClicked;
        }
    }

    void UnbindUiEvents()
    {
        if (_uiManager == null) return;

        var home = _uiManager.HomePanel;
        if (home != null)
        {
            home.PlayClicked -= HandlePlayClicked;
            home.SettingClicked -= HandleSettingClicked;
        }

        var game = _uiManager.GamePanel;
        if (game != null)
        {
            game.BackToHomeClicked -= HandleBackToHomeClicked;
            game.SettingClicked -= HandleSettingClicked;
            game.HintBoosterUseRequested -= HandleHintBoosterUseRequested;
            game.HintBoosterAdRequested -= HandleHintBoosterAdRequested;
            game.FindCatBoosterUseRequested -= HandleFindCatBoosterUseRequested;
            game.FindCatBoosterAdRequested -= HandleFindCatBoosterAdRequested;
        }

        var win = _uiManager.WinPanel;
        if (win != null)
            win.NextLevelClicked -= HandleNextLevelClicked;

        var lose = _uiManager.LosePanel;
        if (lose != null)
            lose.ReplayClicked -= HandleReplayClicked;

        var setting = _uiManager.SettingPopup;
        if (setting != null)
        {
            setting.Closed -= HandleSettingClosed;
            setting.ReplayClicked -= HandleSettingReplayClicked;
        }
    }

    void HandlePlayClicked()
    {
        if (ShouldStartTutorial())
            StartTutorialLevel();
        else
            StartLevel(GetPlayLevelNumber());
    }

    void HandleBackToHomeClicked()
    {
        EndGameSession();
        _uiManager?.ShowHome();
    }

    void HandleNextLevelClicked()
    {
        if (_testMode)
        {
            StartLevel(GetPlayLevelNumber());
            return;
        }

        _currentLevelNumber++;
        if (!LevelLoader.LevelExists(LevelLoader.GetLevelNameByNumber(_currentLevelNumber)))
            _currentLevelNumber = 1;

        StartLevel(_currentLevelNumber);
    }

    void HandleReplayClicked()
    {
        if (ShouldStartTutorial())
            StartTutorialLevel();
        else
            StartLevel(GetPlayLevelNumber());
    }

    void HandleSettingClicked()
    {
        if (_uiManager == null) return;

        var fromGame = _isPlaying && _uiManager.GamePanel != null && _uiManager.GamePanel.IsVisible;
        if (fromGame)
            _boardManager?.SetInteractionEnabled(false);

        _uiManager.ShowSetting(fromGame);
    }

    void HandleSettingClosed()
    {
        if (_isPlaying)
            _boardManager?.SetInteractionEnabled(true);
    }

    void HandleSettingReplayClicked()
    {
        if (ShouldStartTutorial())
            StartTutorialLevel();
        else
            StartLevel(GetPlayLevelNumber());
    }

    void HandleHintBoosterUseRequested()
    {
        TryUseHintBooster();
    }

    void HandleHintBoosterAdRequested()
    {
        RewardAdsService.ShowRewardedAd(success =>
        {
            if (!success) return;

            _boosterCharges.AddHintCharge();
            RefreshBoosterUI();
        });
    }

    void HandleFindCatBoosterUseRequested()
    {
        TryUseFindCatBooster();
    }

    void HandleFindCatBoosterAdRequested()
    {
        RewardAdsService.ShowRewardedAd(success =>
        {
            if (!success) return;

            _boosterCharges.AddFindCatCharge();
            RefreshBoosterUI();
        });
    }

    void TryUseHintBooster()
    {
        if (!_isPlaying || _hintBooster == null) return;
        if (!_boosterCharges.TryConsumeHint()) return;

        if (_hintBooster.TryUseHint())
        {
            RefreshBoosterUI();
            return;
        }

        _boosterCharges.AddHintCharge();
        RefreshBoosterUI();
    }

    void TryUseFindCatBooster()
    {
        if (!_isPlaying || _findCatQBooster == null) return;
        if (!_boosterCharges.TryConsumeFindCat()) return;

        if (_findCatQBooster.TryRevealCat())
        {
            RefreshBoosterUI();
            return;
        }

        _boosterCharges.AddFindCatCharge();
        RefreshBoosterUI();
    }

    void RefreshBoosterUI()
    {
        _uiManager?.GamePanel?.RefreshBoosterCharges(
            _boosterCharges.GetHintCharges(),
            _boosterCharges.GetFindCatCharges());
    }

    int GetPlayLevelNumber()
    {
        if (_testMode)
            return Mathf.Max(1, _levelTestIndex);

        return _currentLevelNumber;
    }

    bool ShouldStartTutorial()
    {
        return _tutorialManager != null && _tutorialManager.ShouldPlayTutorial(_testMode);
    }

    void StartTutorialLevel()
    {
        KillPendingWinTween();
        _activeLevelNumber = 0;

        if (_boardManager == null)
        {
            Debug.LogError("GameManager: BoardManager is missing.");
            return;
        }

        _boardManager.CellPlacementResolved -= HandleCellPlacementResolved;
        _boardManager.ClearBoard();

        if (!_boardManager.LoadLevel(TutorialManager.TutorialLevelName))
            return;

        _boardManager.CellPlacementResolved += HandleCellPlacementResolved;

        _hintBooster?.Reset();
        _findCatQBooster?.Reset();
        ResetSessionStats();
        ApplyPreplacedCats();
        _isPlaying = true;

        _uiManager?.ShowTutorialBoard();
        RefreshGamePanel();
        RefreshBoosterUI();
        _tutorialManager?.Begin();
    }

    void StartLevel(int levelNumber)
    {
        KillPendingWinTween();
        _activeLevelNumber = Mathf.Max(1, levelNumber);
        var levelName = LevelLoader.GetLevelNameByNumber(_activeLevelNumber);

        if (_boardManager == null)
        {
            Debug.LogError("GameManager: BoardManager is missing.");
            return;
        }

        _boardManager.CellPlacementResolved -= HandleCellPlacementResolved;
        _boardManager.ClearBoard();

        if (!_boardManager.LoadLevel(levelName))
            return;

        _boardManager.CellPlacementResolved += HandleCellPlacementResolved;

        _hintBooster?.Reset();
        _findCatQBooster?.Reset();
        ResetSessionStats();
        ApplyPreplacedCats();
        _isPlaying = true;

        _uiManager?.ShowGame();
        RefreshGamePanel();
        RefreshBoosterUI();
    }

    void ResetSessionStats()
    {
        _lives = MaxLives;
        _score = 0;
        _catsCollected = 0;
        _totalCats = _boardManager.CurrentLevel?.QueenCount ?? 0;
        _miceCollected = 0;
        _totalMice = _boardManager.CurrentLevel?.MouseCount ?? 0;
    }

    void ApplyPreplacedCats()
    {
        var level = _boardManager.CurrentLevel;
        if (level?.catRevealed == null) return;

        for (var row = 0; row < level.RowCount; row++)
        {
            for (var col = 0; col < level.ColCount; col++)
            {
                if (!level.IsCatRevealedAt(row, col)) continue;
                if (level.HasMouseAt(row, col))
                    _miceCollected++;
                else
                    _catsCollected++;
            }
        }
    }

    void HandleCellPlacementResolved(CellView cell, bool isCorrectAnimal)
    {
        if (!_isPlaying) return;

        if (_tutorialManager != null && _tutorialManager.IsActive)
            _tutorialManager.HandleCellAction(cell, isCorrectAnimal);

        if (!isCorrectAnimal)
        {
            if (_tutorialManager != null && _tutorialManager.ShouldIgnoreWrongPlacementPenalty())
                return;

            _lives--;
            RefreshGamePanel();

            if (_lives <= 0)
                HandleLose();
            return;
        }

        RegisterAnimalCollected(cell);
    }

    void RegisterAnimalCollected(CellView cell)
    {
        if (cell.IsSolutionMouse)
            _miceCollected++;
        else
            _catsCollected++;

        _score += PointsPerCat;
        RefreshGamePanel();

        if (_catsCollected >= _totalCats && _miceCollected >= _totalMice && (_totalCats + _totalMice) > 0)
            HandleWin();
    }

    void HandleWin()
    {
        if (!_isPlaying) return;

        if (_tutorialManager != null && _tutorialManager.IsActive)
        {
            _tutorialManager.HandleLevelWon();
            if (_tutorialManager.ShouldSuppressWinPresentation())
                return;
        }

        if (!_testMode)
            CampaignProgressStore.SaveNextLevelAfterPass(_activeLevelNumber);

        _isPlaying = false;
        _boardManager.CellPlacementResolved -= HandleCellPlacementResolved;
        _boardManager.SetInteractionEnabled(false);

        KillPendingWinTween();
        _pendingWinTween = DOVirtual
            .DelayedCall(_winBoardHoldDelay, CompleteWinPresentation)
            .SetUpdate(true);
    }

    void CompleteWinPresentation()
    {
        _pendingWinTween = null;
        AdsControl.Instance?.ShowInterstital();
        SoundManager.Instance?.PlayLevelCompleteSound();
        _boardManager?.ClearBoard();
        _uiManager?.ShowWin();
    }

    void KillPendingWinTween()
    {
        if (_pendingWinTween == null)
            return;

        if (_pendingWinTween.IsActive())
            _pendingWinTween.Kill();

        _pendingWinTween = null;
    }

    void HandleLose()
    {
        if (!_isPlaying) return;
        _isPlaying = false;
        _boardManager.CellPlacementResolved -= HandleCellPlacementResolved;
        AdsControl.Instance?.ShowInterstital();
        SoundManager.Instance?.PlayOutOfSpaceSound();
        _boardManager.ClearBoard();
        _uiManager?.ShowLose();
    }

    void EndGameSession()
    {
        KillPendingWinTween();
        _isPlaying = false;
        _tutorialManager?.End();
        if (_boardManager != null)
            _boardManager.CellPlacementResolved -= HandleCellPlacementResolved;
        _boardManager?.ClearBoard();
    }

    void RefreshGamePanel()
    {
        _uiManager?.GamePanel?.Refresh(
            _activeLevelNumber,
            _score,
            _catsCollected,
            _totalCats,
            _miceCollected,
            _totalMice,
            _lives,
            MaxLives);
    }

}

