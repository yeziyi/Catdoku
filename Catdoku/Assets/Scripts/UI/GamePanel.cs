using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public class GamePanel : BasePanel
    {
        const int DefaultMaxLives = 3;

        [Header("Texts")]
        [SerializeField] Text levelText;
        [SerializeField] Text scoreText;
        [SerializeField] Text catValueText;

        [Header("Buttons")]
        [SerializeField] Button backToHomeButton;
        [SerializeField] Button settingButton;

        [Header("Boosters")]
        [SerializeField] BoosterButtonView hintBoosterButton;
        [SerializeField] BoosterButtonView findCatBoosterButton;

        [Header("Lives")]
        [SerializeField] Image[] lifeIcons;
        [SerializeField] Color activeLifeColor = new(0.18f, 0.18f, 0.22f, 1f);
        [SerializeField] Color inactiveLifeColor = new(0.78f, 0.78f, 0.82f, 1f);

        public event Action BackToHomeClicked;
        public event Action SettingClicked;
        public event Action HintBoosterUseRequested;
        public event Action HintBoosterAdRequested;
        public event Action FindCatBoosterUseRequested;
        public event Action FindCatBoosterAdRequested;

        protected override void Awake()
        {
            base.Awake();
            if (backToHomeButton != null)
                backToHomeButton.onClick.AddListener(OnBackToHomeClicked);
            if (settingButton != null)
                settingButton.onClick.AddListener(OnSettingClicked);

            BindBoosterButtons();
        }

        void BindBoosterButtons()
        {
            if (hintBoosterButton != null)
            {
                hintBoosterButton.UseRequested += OnHintBoosterUseRequested;
                hintBoosterButton.AdRequested += OnHintBoosterAdRequested;
            }

            if (findCatBoosterButton != null)
            {
                findCatBoosterButton.UseRequested += OnFindCatBoosterUseRequested;
                findCatBoosterButton.AdRequested += OnFindCatBoosterAdRequested;
            }
        }

        void OnBackToHomeClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            BackToHomeClicked?.Invoke();
        }

        void OnSettingClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            SettingClicked?.Invoke();
        }

        void OnHintBoosterUseRequested() => HintBoosterUseRequested?.Invoke();
        void OnHintBoosterAdRequested() => HintBoosterAdRequested?.Invoke();
        void OnFindCatBoosterUseRequested() => FindCatBoosterUseRequested?.Invoke();
        void OnFindCatBoosterAdRequested() => FindCatBoosterAdRequested?.Invoke();

        protected override void OnDestroy()
        {
            if (backToHomeButton != null) backToHomeButton.onClick.RemoveAllListeners();
            if (settingButton != null) settingButton.onClick.RemoveAllListeners();

            if (hintBoosterButton != null)
            {
                hintBoosterButton.UseRequested -= OnHintBoosterUseRequested;
                hintBoosterButton.AdRequested -= OnHintBoosterAdRequested;
            }

            if (findCatBoosterButton != null)
            {
                findCatBoosterButton.UseRequested -= OnFindCatBoosterUseRequested;
                findCatBoosterButton.AdRequested -= OnFindCatBoosterAdRequested;
            }

            base.OnDestroy();
        }

        public void Refresh(int levelNumber, int score, int catsCollected, int totalCats, int lives, int maxLives = DefaultMaxLives)
        {
            if (levelText != null)
                levelText.text = $"{levelNumber}";

            if (scoreText != null)
                scoreText.text = score.ToString();

            if (catValueText != null)
                catValueText.text = $"{catsCollected}/{Mathf.Max(totalCats, 0)}";

            UpdateLifeIcons(lives, maxLives);
        }

        public void RefreshBoosterCharges(int hintCharges, int findCatCharges)
        {
            hintBoosterButton?.SetCharges(hintCharges);
            findCatBoosterButton?.SetCharges(findCatCharges);
        }

        void UpdateLifeIcons(int lives, int maxLives)
        {
            if (lifeIcons == null || lifeIcons.Length == 0) return;

            var clampedLives = Mathf.Clamp(lives, 0, maxLives);
            for (var i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] == null) continue;
                var isActive = i < clampedLives;
                lifeIcons[i].color = isActive ? activeLifeColor : inactiveLifeColor;
            }
        }
    }
}
