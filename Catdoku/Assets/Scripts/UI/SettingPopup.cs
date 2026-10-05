using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public class SettingPopup : BasePopup
    {
        const string MusicTogglePath = "Root/PopupBG/Music/Toogle";
        const string SoundTogglePath = "Root/PopupBG/Sound/Toogle";
        const string ReplayButtonPath = "Root/PopupBG/Replay";
        const string CloseButtonPath = "Root/PopupBG/Close";

        [Header("Setting")]
        [SerializeField] SettingToggleView musicToggle;
        [SerializeField] SettingToggleView soundToggle;
        [SerializeField] Button replayButton;
        [SerializeField] Button closeButton;

        [Header("Modal Blocker")]
        [SerializeField] Image modalBlocker;
        [SerializeField] Color modalBlockerColor = new(0f, 0f, 0f, 0.45f);

        public event Action Closed;
        public event Action ReplayClicked;

        protected override void Awake()
        {
            base.Awake();
            EnsureReferences();
            EnsureModalBlocker();
            BindButtons();
            SetVisibleImmediate(false);
        }

        protected override void OnDestroy()
        {
            UnbindButtons();
            base.OnDestroy();
        }

        public void Show(bool showReplayButton)
        {
            if (replayButton != null)
                replayButton.gameObject.SetActive(showReplayButton);

            RefreshToggleStates();
            ShowModal();
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            Closed?.Invoke();
        }

        void ShowModal()
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (rootContent != null)
                rootContent.localScale = Vector3.zero;

            canvasGroup
                .DOFade(1f, fadeDuration)
                .SetUpdate(true);

            if (rootContent != null)
            {
                rootContent
                    .DOScale(Vector3.one, scaleDuration)
                    .SetEase(scaleEase)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        _isVisible = true;
                        OnShown();
                    });
            }
            else
            {
                canvasGroup
                    .DOFade(1f, fadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        _isVisible = true;
                        OnShown();
                    });
            }
        }

        void EnsureReferences()
        {
            if (musicToggle == null)
                musicToggle = transform.Find(MusicTogglePath)?.GetComponent<SettingToggleView>();
            if (musicToggle == null)
                musicToggle = transform.Find(MusicTogglePath)?.gameObject.AddComponent<SettingToggleView>();

            if (soundToggle == null)
                soundToggle = transform.Find(SoundTogglePath)?.GetComponent<SettingToggleView>();
            if (soundToggle == null)
                soundToggle = transform.Find(SoundTogglePath)?.gameObject.AddComponent<SettingToggleView>();

            if (replayButton == null)
                replayButton = transform.Find(ReplayButtonPath)?.GetComponent<Button>();

            if (closeButton == null)
            {
                var closeTransform = transform.Find(CloseButtonPath);
                if (closeTransform != null)
                {
                    closeButton = closeTransform.GetComponent<Button>();
                    if (closeButton == null)
                    {
                        closeButton = closeTransform.gameObject.AddComponent<Button>();
                        closeButton.transition = Selectable.Transition.None;
                        closeButton.targetGraphic = closeTransform.GetComponent<Image>();
                    }
                }
            }
        }

        void EnsureModalBlocker()
        {
            if (modalBlocker != null) return;

            var blockerObject = new GameObject("ModalBlocker", typeof(RectTransform), typeof(Image));
            blockerObject.transform.SetParent(transform, false);
            blockerObject.transform.SetAsFirstSibling();

            var blockerRect = blockerObject.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            modalBlocker = blockerObject.GetComponent<Image>();
            modalBlocker.raycastTarget = true;
            modalBlocker.color = modalBlockerColor;
        }

        void BindButtons()
        {
            musicToggle?.Bind(HandleMusicToggleChanged);
            soundToggle?.Bind(HandleSoundToggleChanged);

            if (replayButton != null)
                replayButton.onClick.AddListener(HandleReplayClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClicked);
        }

        void UnbindButtons()
        {
            if (replayButton != null)
                replayButton.onClick.RemoveListener(HandleReplayClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        void RefreshToggleStates()
        {
            var soundManager = SoundManager.Instance;
            if (soundManager == null) return;

            musicToggle?.SetValue(soundManager.IsMusicEnabled);
            soundToggle?.SetValue(soundManager.IsSoundEnabled);
        }

        void HandleMusicToggleChanged(bool enabled)
        {
            SoundManager.Instance?.SetMusicEnabled(enabled);
        }

        void HandleSoundToggleChanged(bool enabled)
        {
            SoundManager.Instance?.SetSoundEnabled(enabled);
        }

        void HandleReplayClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            Hide();
            ReplayClicked?.Invoke();
        }

        void HandleCloseClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            Hide();
        }
    }
}
