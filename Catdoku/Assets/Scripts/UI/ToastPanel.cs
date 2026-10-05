using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    /// <summary>
    /// Toast overlay: fade in from <see cref="BasePanel"/>, show message, then fade out after a delay.
    /// </summary>
    [DisallowMultipleComponent]
    public class ToastPanel : BasePanel
    {
        [Header("Toast")]
        [SerializeField] Text messageText;
        [SerializeField, Min(0.05f)] float defaultVisibleDuration = 2f;

        Tween _scheduledHide;

        protected override void Awake()
        {
            base.Awake();
            if (messageText == null)
                messageText = GetComponentInChildren<Text>(true);
            EnsureStartsHidden();
        }

        void Reset()
        {
            if (messageText == null)
                messageText = GetComponentInChildren<Text>(true);
        }

        void EnsureStartsHidden()
        {
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _isVisible = false;
        }

        protected override void OnDestroy()
        {
            KillScheduledHide();
            base.OnDestroy();
        }

        /// <summary>
        /// Toast should not block taps on the game UI.
        /// </summary>
        protected override void OnShown()
        {
            if (canvasGroup == null)
                return;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Shows the toast with <paramref name="content"/> for <paramref name="visibleDuration"/> seconds
        /// after the fade-in finishes, then fades out. If already fully visible, only updates text and resets the timer.
        /// </summary>
        public void ShowToast(string content, float visibleDuration)
        {
            KillScheduledHide();

            if (messageText != null)
                messageText.text = content ?? string.Empty;

            float wait = Mathf.Max(0.05f, visibleDuration);

            if (IsVisible)
            {
                ScheduleHide(wait);
                return;
            }

            Show();
            ScheduleHideAfterFadeIn(wait);
        }

        public void ShowToast(string content) => ShowToast(content, defaultVisibleDuration);

        void ScheduleHideAfterFadeIn(float visibleDuration)
        {
            _scheduledHide = DOVirtual.DelayedCall(fadeDuration + visibleDuration, Hide, false).SetUpdate(true);
        }

        void ScheduleHide(float visibleDuration)
        {
            _scheduledHide = DOVirtual.DelayedCall(visibleDuration, Hide, false).SetUpdate(true);
        }

        void KillScheduledHide()
        {
            if (_scheduledHide != null && _scheduledHide.IsActive())
                _scheduledHide.Kill();
            _scheduledHide = null;
        }
    }
}
