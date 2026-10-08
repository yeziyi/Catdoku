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

        Text mouseValueText;
        GameObject _mouseBadgeRoot;
        static Sprite _sharedMouseSprite;

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
            EnsureMouseBadge();
        }

        void EnsureMouseBadge()
        {
            if (catValueText == null || mouseValueText != null) return;

            var catBadge = catValueText.transform.parent as RectTransform;
            if (catBadge == null) return;

            var mouseBadge = Instantiate(catBadge, catBadge.parent);
            mouseBadge.name = "MouseTotal";
            _mouseBadgeRoot = mouseBadge.gameObject;
            var catRt = catBadge;
            var mouseRt = mouseBadge;

            // stack vertically on the left: cat on top, mouse directly below
            mouseRt.anchoredPosition = catRt.anchoredPosition + new Vector2(0f, -(catRt.sizeDelta.y + 8f));

            // set the cloned icon (child image, not the badge background) to the mouse sprite
            foreach (var img in mouseBadge.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject == mouseBadge.gameObject) continue;
                img.sprite = GetMouseSprite();
            }

            // the cloned text becomes the mouse counter
            mouseValueText = mouseBadge.GetComponentInChildren<Text>(true);
            if (mouseValueText != null)
                mouseValueText.text = "0/0";
        }

        static Sprite GetMouseSprite()
        {
            if (_sharedMouseSprite == null)
            {
                var tex = Resources.Load<Texture2D>("Images/GameView/IconMouse");
                if (tex != null)
                    _sharedMouseSprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
            }

            return _sharedMouseSprite;
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

        public void Refresh(int levelNumber, int score, int catsCollected, int totalCats, int miceCollected, int totalMice, int lives, int maxLives = DefaultMaxLives)
        {
            if (levelText != null)
                levelText.text = $"{levelNumber}";

            if (scoreText != null)
                scoreText.text = score.ToString();

            if (catValueText != null)
                catValueText.text = $"{catsCollected}/{Mathf.Max(totalCats, 0)}";

            if (mouseValueText != null)
                mouseValueText.text = $"{miceCollected}/{Mathf.Max(totalMice, 0)}";

            if (_mouseBadgeRoot != null)
                _mouseBadgeRoot.SetActive(totalMice > 0);

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
