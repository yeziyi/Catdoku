using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public class WinPanel : ResultPanelBase
    {
        [Header("Win")]
        [SerializeField] Image levelCompleteImage;
        [SerializeField] Image catWinImage;
        [SerializeField] Button nextLevelButton;

        public event Action NextLevelClicked;

        protected override void Awake()
        {
            titleImage = levelCompleteImage;
            mascotImage = catWinImage;
            actionButton = nextLevelButton;
            base.Awake();

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        void OnNextLevelClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            NextLevelClicked?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}
