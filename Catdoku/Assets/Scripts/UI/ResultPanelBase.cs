using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public abstract class ResultPanelBase : BasePanel
    {
        [Header("Content")]
        [SerializeField] protected Image titleImage;
        [SerializeField] protected Image mascotImage;
        [SerializeField] protected Button actionButton;

        [Header("Show Animation")]
        [SerializeField] float showDelay = 0.5f;
        [SerializeField] float titleFadeDuration = 0.35f;
        [SerializeField] float mascotScaleDuration = 0.4f;
        [SerializeField] float buttonShowDuration = 0.25f;
        [SerializeField] Ease mascotScaleEase = Ease.OutBack;

        Sequence _showSequence;
        Vector3 _mascotBaseScale = Vector3.one;
        CanvasGroup _actionButtonCanvasGroup;

        protected override void Awake()
        {
            base.Awake();
            CacheMascotBaseScale();
            CacheActionButtonCanvasGroup();
            PrepareHiddenState();
        }

        protected override void OnDestroy()
        {
            KillShowSequence();
            KillContentTweens();
            base.OnDestroy();
        }

        public override void Show()
        {
            KillShowSequence();
            KillContentTweens();

            if (canvasGroup == null)
                return;

            PrepareHiddenState();

            canvasGroup.DOKill();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .AppendInterval(showDelay);

            if (titleImage != null)
                _showSequence.Append(titleImage.DOFade(1f, titleFadeDuration));
            else
                _showSequence.AppendInterval(titleFadeDuration);

            if (mascotImage != null)
            {
                mascotImage.transform.localScale = Vector3.zero;
                _showSequence.Append(
                    mascotImage.transform
                        .DOScale(_mascotBaseScale, mascotScaleDuration)
                        .SetEase(mascotScaleEase));
            }
            else
            {
                _showSequence.AppendInterval(mascotScaleDuration);
            }

            if (_actionButtonCanvasGroup != null)
            {
                _actionButtonCanvasGroup.alpha = 0f;
                _actionButtonCanvasGroup.blocksRaycasts = false;
                if (actionButton != null)
                    actionButton.interactable = false;

                _showSequence.Append(
                    _actionButtonCanvasGroup
                        .DOFade(1f, buttonShowDuration)
                        .OnComplete(EnableActionButton));
            }

            _showSequence.OnComplete(() =>
            {
                _isVisible = true;
                canvasGroup.interactable = true;
                OnShown();
            });
        }

        public override void Hide()
        {
            KillShowSequence();
            KillContentTweens();
            _isVisible = false;
            PrepareHiddenState();
            PlayHideFade();
        }

        public override void SetVisibleImmediate(bool visible)
        {
            KillShowSequence();
            KillContentTweens();

            if (!visible)
            {
                PrepareHiddenState();
                base.SetVisibleImmediate(false);
                return;
            }

            base.SetVisibleImmediate(true);
            ShowContentImmediate();
        }

        void CacheMascotBaseScale()
        {
            if (mascotImage != null)
                _mascotBaseScale = mascotImage.transform.localScale;
        }

        void CacheActionButtonCanvasGroup()
        {
            if (actionButton == null)
                return;

            _actionButtonCanvasGroup = actionButton.GetComponent<CanvasGroup>();
            if (_actionButtonCanvasGroup == null)
                _actionButtonCanvasGroup = actionButton.gameObject.AddComponent<CanvasGroup>();
        }

        void PrepareHiddenState()
        {
            if (titleImage != null)
            {
                titleImage.DOKill();
                var c = titleImage.color;
                titleImage.color = new Color(c.r, c.g, c.b, 0f);
            }

            if (mascotImage != null)
            {
                mascotImage.transform.DOKill();
                mascotImage.transform.localScale = Vector3.zero;
            }

            if (_actionButtonCanvasGroup != null)
            {
                _actionButtonCanvasGroup.DOKill();
                _actionButtonCanvasGroup.alpha = 0f;
                _actionButtonCanvasGroup.blocksRaycasts = false;
            }

            if (actionButton != null)
                actionButton.interactable = false;
        }

        void ShowContentImmediate()
        {
            if (titleImage != null)
            {
                var c = titleImage.color;
                titleImage.color = new Color(c.r, c.g, c.b, 1f);
            }

            if (mascotImage != null)
                mascotImage.transform.localScale = _mascotBaseScale;

            if (_actionButtonCanvasGroup != null)
            {
                _actionButtonCanvasGroup.alpha = 1f;
                _actionButtonCanvasGroup.blocksRaycasts = true;
            }

            EnableActionButton();
        }

        void EnableActionButton()
        {
            if (actionButton == null)
                return;

            actionButton.interactable = true;
            if (_actionButtonCanvasGroup != null)
                _actionButtonCanvasGroup.blocksRaycasts = true;
        }

        void KillShowSequence()
        {
            if (_showSequence == null)
                return;

            if (_showSequence.IsActive())
                _showSequence.Kill();

            _showSequence = null;
        }

        void KillContentTweens()
        {
            titleImage?.DOKill();
            mascotImage?.transform.DOKill();
            _actionButtonCanvasGroup?.DOKill();
        }
    }
}
