using System;
using ColorCubeShooter;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public enum CellCrossMode
{
    None,
    White,
    Wrong
}

public enum CellMarkState
{
    Empty,
    CrossWhite,
    CrossWrong,
    Queen
}

public enum CellInputMode
{
    Normal,
    DoubleClickOnly,
    TutorialMarkOnly,
    Step6LastColor
}

enum SwipeMarkMode
{
    None,
    AddWhiteCross,
    RemoveWhiteCross
}

enum SingleClickDelta
{
    None,
    AddedWhiteCross,
    RemovedWhiteCross
}

[RequireComponent(typeof(BoxCollider2D))]
public class CellView : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler
{
    public event Action<CellView, bool> PlacementResolved;
    public event Action<CellView> TutorialMarkPlaced;

    static bool _markSwipeActive;
    static bool _swipeDragOccurred;
    static bool _swipeOriginApplied;
    static CellView _swipeOriginCell;
    static SwipeMarkMode _swipeMarkMode;

    static readonly Color WrongCrossColor = new(0.38f, 0.22f, 0.12f, 1f);

    [SerializeField] SpriteRenderer _background;
    [SerializeField] SpriteRenderer _iconX;
    [SerializeField] SpriteRenderer _iconCat;
    [SerializeField] Transform _starParticlesRoot;
    [SerializeField] Transform _heartParticlesRoot;
    [SerializeField] float _animDuration = 0.16f;

    const float DoubleTapMaxDelay = 0.4f;
    const float DoubleTapMaxDistance = 50f;

    BoxCollider2D _collider;
    Vector3 _iconXBaseScale;
    Vector3 _iconCatBaseScale;

    float _lastTapTime = -1f;
    Vector2 _lastTapScreenPosition;
    bool _queuedDoubleTap;

    CellCrossMode _crossMode = CellCrossMode.None;
    SingleClickDelta _lastSingleClickDelta = SingleClickDelta.None;
    bool _catVisible;
    bool _isLocked;
    bool _tutorialLocked;
    bool _tutorialHidden;
    CellInputMode _inputMode = CellInputMode.Normal;
    int _sortingOrderBoost;
    int _row;
    int _col;
    bool _isSolutionQueen;

    public int Row => _row;
    public int Col => _col;
    public bool IsSolutionQueen => _isSolutionQueen;
    public bool IsLocked => _isLocked;
    public bool IsCatVisible => _catVisible;
    public bool NeedsEliminationMark => !IsCatVisible && _crossMode == CellCrossMode.None;
    public float AnimDuration => _animDuration;

    public CellMarkState State
    {
        get
        {
            if (_catVisible) return CellMarkState.Queen;
            return _crossMode switch
            {
                CellCrossMode.White => CellMarkState.CrossWhite,
                CellCrossMode.Wrong => CellMarkState.CrossWrong,
                _ => CellMarkState.Empty
            };
        }
    }

    void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        if (_iconX != null) _iconXBaseScale = _iconX.transform.localScale;
        if (_iconCat != null) _iconCatBaseScale = _iconCat.transform.localScale;

        if (_starParticlesRoot == null)
            _starParticlesRoot = transform.Find("StarExplosion");
        if (_heartParticlesRoot == null)
            _heartParticlesRoot = transform.Find("HeartBreak");

        StopParticleRoot(_starParticlesRoot);
        StopParticleRoot(_heartParticlesRoot);
    }

    void OnDestroy()
    {
        KillTweens();
    }

    public void Initialize(int row, int col, Color backgroundColor, bool isSolutionQueen, bool catRevealedAtStart = false)
    {
        _row = row;
        _col = col;
        _isSolutionQueen = isSolutionQueen;
        ResetVisuals();
        SetBackgroundColor(backgroundColor);

        if (isSolutionQueen && catRevealedAtStart)
            ShowPreplacedCat();
    }

    public void SetBackgroundColor(Color color)
    {
        if (_background != null)
            _background.color = color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isLocked || _tutorialLocked) return;

        if (_inputMode == CellInputMode.TutorialMarkOnly)
        {
            TryPlaceTutorialMark();
            return;
        }

        if (_inputMode == CellInputMode.Step6LastColor)
        {
            if (IsDoubleTap(eventData))
                ApplyStep6DoubleClick();
            else
                TryPlaceTutorialMark();

            return;
        }

        if (IsDoubleTap(eventData))
        {
            if (_inputMode == CellInputMode.DoubleClickOnly || _inputMode == CellInputMode.Normal)
            {
                UndoLastSingleClick();
                ApplyDoubleClick();
            }

            return;
        }

        if (_inputMode == CellInputMode.DoubleClickOnly)
            return;

        if (_swipeDragOccurred)
        {
            _swipeDragOccurred = false;
            return;
        }

        ApplySingleClick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isLocked || _tutorialLocked) return;

        if (_inputMode == CellInputMode.TutorialMarkOnly)
        {
            BeginMarkSwipe();
            TryPlaceTutorialMark();
            return;
        }

        if (_inputMode != CellInputMode.Normal) return;

        var mode = GetSwipeMarkMode();
        if (mode == SwipeMarkMode.None) return;

        BeginMarkSwipe();
        _swipeMarkMode = mode;
        _swipeOriginCell = this;
        _swipeOriginApplied = false;
        _swipeDragOccurred = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isLocked && !_tutorialLocked && SupportsDoubleTap() && !_swipeDragOccurred && WasSecondTap(eventData))
            _queuedDoubleTap = true;

        EndMarkSwipe();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_markSwipeActive) return;
        if (_isLocked || _tutorialLocked) return;

        if (_inputMode == CellInputMode.TutorialMarkOnly)
        {
            TryPlaceTutorialMark();
            return;
        }

        if (_inputMode != CellInputMode.Normal) return;

        _swipeDragOccurred = true;

        if (!_swipeOriginApplied && _swipeOriginCell != null)
        {
            _swipeOriginCell.ApplyNormalSwipeMark(_swipeMarkMode);
            _swipeOriginApplied = true;
        }

        ApplyNormalSwipeMark(_swipeMarkMode);
    }

    bool SupportsDoubleTap()
    {
        return _inputMode == CellInputMode.Normal
            || _inputMode == CellInputMode.DoubleClickOnly
            || _inputMode == CellInputMode.Step6LastColor;
    }

    bool IsDoubleTap(PointerEventData eventData)
    {
        if (_queuedDoubleTap)
        {
            _queuedDoubleTap = false;
            ResetTapTracking();
            return true;
        }

        if (eventData.clickCount >= 2)
        {
            ResetTapTracking();
            return true;
        }

        return false;
    }

    bool WasSecondTap(PointerEventData eventData)
    {
        var now = Time.unscaledTime;
        var position = eventData.position;

        if (_lastTapTime >= 0f
            && now - _lastTapTime <= DoubleTapMaxDelay
            && Vector2.Distance(position, _lastTapScreenPosition) <= DoubleTapMaxDistance)
        {
            ResetTapTracking();
            return true;
        }

        _lastTapTime = now;
        _lastTapScreenPosition = position;
        return false;
    }

    void ResetTapTracking()
    {
        _lastTapTime = -1f;
        _queuedDoubleTap = false;
    }

    void TryPlaceTutorialMark()
    {
        if (_isLocked || _tutorialLocked) return;
        if (_isSolutionQueen && _catVisible) return;
        if (_crossMode != CellCrossMode.None) return;

        ShowCross(CellCrossMode.White);
        SoundManager.Instance?.PlayClickXSound();
        TutorialMarkPlaced?.Invoke(this);
    }

    SwipeMarkMode GetSwipeMarkMode()
    {
        if (_isSolutionQueen && _catVisible) return SwipeMarkMode.None;
        if (_crossMode == CellCrossMode.White) return SwipeMarkMode.RemoveWhiteCross;
        if (_crossMode == CellCrossMode.None) return SwipeMarkMode.AddWhiteCross;
        return SwipeMarkMode.None;
    }

    void ApplyNormalSwipeMark(SwipeMarkMode mode)
    {
        if (_isLocked || _tutorialLocked) return;
        if (_isSolutionQueen && _catVisible) return;

        if (mode == SwipeMarkMode.AddWhiteCross && _crossMode == CellCrossMode.None)
        {
            ShowCross(CellCrossMode.White);
            SoundManager.Instance?.PlayClickXSound();
            return;
        }

        if (mode == SwipeMarkMode.RemoveWhiteCross && _crossMode == CellCrossMode.White)
        {
            HideCross();
            SoundManager.Instance?.PlayUnClickXSound();
        }
    }

    static void BeginMarkSwipe()
    {
        _markSwipeActive = true;
    }

    static void EndMarkSwipe()
    {
        _markSwipeActive = false;
        _swipeOriginCell = null;
        _swipeMarkMode = SwipeMarkMode.None;
        _swipeOriginApplied = false;
    }

    void ApplyStep6DoubleClick()
    {
        if (_isLocked || _tutorialLocked) return;

        if (_isSolutionQueen && !_catVisible)
        {
            HideCrossImmediate();
            LockCell();
            SoundManager.Instance?.PlayDoubleClickCorrectSound();
            PlayStarParticles();
            ShowCat(() => PlacementResolved?.Invoke(this, true));
            return;
        }

        TryPlaceTutorialMark();
    }

    public void RevealCatSilently()
    {
        if (!_isSolutionQueen || _catVisible) return;

        HideCrossImmediate();
        _catVisible = true;
        if (_iconCat != null)
        {
            _iconCat.DOKill();
            _iconCat.transform.DOKill();
            _iconCat.enabled = true;
            _iconCat.color = Color.white;
            _iconCat.transform.localScale = _iconCatBaseScale;
        }

        LockCell();
    }

    void ApplySingleClick()
    {
        if (_isLocked) return;
        if (_isSolutionQueen && _catVisible) return;

        if (_inputMode == CellInputMode.TutorialMarkOnly)
        {
            TryPlaceTutorialMark();
            return;
        }

        _lastSingleClickDelta = SingleClickDelta.None;

        if (_crossMode == CellCrossMode.White)
        {
            _lastSingleClickDelta = SingleClickDelta.RemovedWhiteCross;
            HideCross();
            SoundManager.Instance?.PlayUnClickXSound();
        }
        else if (_crossMode == CellCrossMode.None)
        {
            _lastSingleClickDelta = SingleClickDelta.AddedWhiteCross;
            ShowCross(CellCrossMode.White);
            SoundManager.Instance?.PlayClickXSound();
        }
    }

    void UndoLastSingleClick()
    {
        if (_lastSingleClickDelta == SingleClickDelta.AddedWhiteCross)
            HideCrossImmediate();

        _lastSingleClickDelta = SingleClickDelta.None;
    }

    void ApplyDoubleClick()
    {
        if (_isLocked) return;

        if (_isSolutionQueen)
        {
            if (_catVisible) return;
            HideCrossImmediate();
            LockCell();
            SoundManager.Instance?.PlayDoubleClickCorrectSound();
            PlayStarParticles();
            ShowCat(() => PlacementResolved?.Invoke(this, true));
            return;
        }

        HideCrossImmediate();
        ShowCross(CellCrossMode.Wrong);
        LockCell();
        SoundManager.Instance?.PlayDoubleClickIncorrectSound();
        PlayHeartParticles();
        PlacementResolved?.Invoke(this, false);
    }

    public void ApplyHintPlaceQueen()
    {
        if (_isLocked || _catVisible || !_isSolutionQueen) return;

        HideCrossImmediate();
        LockCell();
        SoundManager.Instance?.PlayDoubleClickCorrectSound();
        PlayStarParticles();
        ShowCat(() => PlacementResolved?.Invoke(this, true));
    }

    public void ApplyHintEliminate()
    {
        if (_isLocked || _catVisible) return;

        HideCrossImmediate();
        ShowCross(CellCrossMode.White);
        LockCell();
        SoundManager.Instance?.PlayClickXSound();
    }

    void ShowCross(CellCrossMode mode)
    {
        _crossMode = mode;
        var targetColor = mode == CellCrossMode.White ? Color.white : WrongCrossColor;
        PlayShowTween(_iconX, targetColor, _iconXBaseScale);
    }

    void HideCross()
    {
        if (_crossMode != CellCrossMode.White) return;

        PlayHideTween(_iconX, () =>
        {
            _crossMode = CellCrossMode.None;
            _iconX.enabled = false;
        });
    }

    void HideCrossImmediate()
    {
        if (_iconX == null || _crossMode == CellCrossMode.None) return;

        _iconX.DOKill();
        _iconX.transform.DOKill();
        _iconX.enabled = false;
        _crossMode = CellCrossMode.None;
    }

    void ShowCat(TweenCallback onComplete = null)
    {
        _catVisible = true;
        PlayShowTween(_iconCat, Color.white, _iconCatBaseScale, onComplete);
    }

    void ShowPreplacedCat()
    {
        _catVisible = true;
        if (_iconCat != null)
        {
            _iconCat.DOKill();
            _iconCat.transform.DOKill();
            _iconCat.enabled = true;
            _iconCat.color = Color.white;
            _iconCat.transform.localScale = _iconCatBaseScale;
        }

        LockCell();
    }

    void PlayShowTween(SpriteRenderer renderer, Color rgb, Vector3 targetScale, TweenCallback onComplete = null)
    {
        if (renderer == null) return;

        renderer.DOKill();
        renderer.transform.DOKill();
        renderer.enabled = true;
        renderer.color = new Color(rgb.r, rgb.g, rgb.b, 0f);
        renderer.transform.localScale = Vector3.zero;

        var sequence = DOTween.Sequence();
        sequence.Join(renderer.DOFade(1f, _animDuration));
        sequence.Join(renderer.transform.DOScale(targetScale, _animDuration).SetEase(Ease.OutBack));
        if (onComplete != null)
            sequence.OnComplete(onComplete);
    }

    void PlayHideTween(SpriteRenderer renderer, TweenCallback onComplete)
    {
        if (renderer == null) return;

        renderer.DOKill();
        renderer.transform.DOKill();
        renderer.DOFade(0f, _animDuration);
        renderer.transform.DOScale(Vector3.zero, _animDuration)
            .SetEase(Ease.InBack)
            .OnComplete(onComplete);
    }

    void LockCell()
    {
        _isLocked = true;
        UpdateColliderState();
    }

    public void SetTutorialLocked(bool locked)
    {
        _tutorialLocked = locked;
        UpdateColliderState();
    }

    public void SetTutorialHidden(bool hidden)
    {
        _tutorialHidden = hidden;
        ApplyTutorialVisibility();
        UpdateColliderState();
    }

    public void SetInputMode(CellInputMode mode)
    {
        _inputMode = mode;
    }

    public void SetSortingOrderBoost(int boost)
    {
        _sortingOrderBoost = boost;
        ApplySortingOrderBoost();
    }

    public void ResetTutorialState()
    {
        _tutorialLocked = false;
        _tutorialHidden = false;
        _inputMode = CellInputMode.Normal;
        _sortingOrderBoost = 0;
        EndMarkSwipe();
        _swipeDragOccurred = false;
        ApplyTutorialVisibility();
        ApplySortingOrderBoost();
        UpdateColliderState();
    }

    public static void ResetMarkSwipe()
    {
        EndMarkSwipe();
        _swipeDragOccurred = false;
    }

    public static void ResetTutorialSwipe() => ResetMarkSwipe();

    void UpdateColliderState()
    {
        if (_collider == null) return;

        var canInteract = !_isLocked && !_tutorialLocked && !_tutorialHidden;
        _collider.enabled = canInteract;
    }

    void ApplyTutorialVisibility()
    {
        SetRendererVisible(_background, !_tutorialHidden);
        SetRendererVisible(_iconX, !_tutorialHidden && _crossMode != CellCrossMode.None);
        SetRendererVisible(_iconCat, !_tutorialHidden && _catVisible);
    }

    static void SetRendererVisible(SpriteRenderer renderer, bool visible)
    {
        if (renderer == null) return;

        var color = renderer.color;
        color.a = visible ? 1f : 0f;
        renderer.color = color;
    }

    void ApplySortingOrderBoost()
    {
        ApplyBoostToRenderer(_background, 0);
        ApplyBoostToRenderer(_iconX, 1);
        ApplyBoostToRenderer(_iconCat, 2);
    }

    void ApplyBoostToRenderer(SpriteRenderer renderer, int baseOrder)
    {
        if (renderer == null) return;
        renderer.sortingOrder = baseOrder + _sortingOrderBoost;
    }

    void ResetVisuals()
    {
        KillTweens();

        _isLocked = false;
        _tutorialLocked = false;
        _tutorialHidden = false;
        _inputMode = CellInputMode.Normal;
        _sortingOrderBoost = 0;
        _catVisible = false;
        _crossMode = CellCrossMode.None;
        _lastSingleClickDelta = SingleClickDelta.None;
        ResetTapTracking();

        if (_collider != null)
            _collider.enabled = true;

        if (_background != null)
        {
            _background.sortingOrder = 0;
            _background.color = Color.white;
        }

        if (_iconX != null)
        {
            _iconX.enabled = false;
            _iconX.color = Color.white;
            _iconX.transform.localScale = _iconXBaseScale;
            _iconX.sortingOrder = 1;
        }

        if (_iconCat != null)
        {
            _iconCat.enabled = false;
            _iconCat.color = Color.white;
            _iconCat.transform.localScale = _iconCatBaseScale;
            _iconCat.sortingOrder = 2;
        }

        ApplySortingOrderBoost();

        StopParticleRoot(_starParticlesRoot);
        StopParticleRoot(_heartParticlesRoot);
    }

    void PlayStarParticles()
    {
        PlayParticleRoot(_starParticlesRoot);
    }

    void PlayHeartParticles()
    {
        PlayParticleRoot(_heartParticlesRoot);
    }

    static void PlayParticleRoot(Transform root)
    {
        if (root == null) return;

        foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            particle.Clear(true);
            particle.Play(true);
        }
    }

    static void StopParticleRoot(Transform root)
    {
        if (root == null) return;

        foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void KillTweens()
    {
        _iconX?.DOKill();
        _iconX?.transform.DOKill();
        _iconCat?.DOKill();
        _iconCat?.transform.DOKill();
    }
}
