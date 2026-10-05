using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public class LosePanel : ResultPanelBase
    {
        [Header("Lose")]
        [SerializeField] Image loseImage;
        [SerializeField] Image catLoseImage;
        [SerializeField] Button replayButton;

        public event Action ReplayClicked;

        protected override void Awake()
        {
            titleImage = loseImage;
            mascotImage = catLoseImage;
            actionButton = replayButton;
            base.Awake();

            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);
        }

        void OnReplayClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            ReplayClicked?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (replayButton != null)
                replayButton.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}
