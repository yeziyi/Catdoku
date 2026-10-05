using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public class HomePanel : BasePanel
    {
        [Header("Buttons")]
        [SerializeField] Button settingButton;
        [SerializeField] Button playButton;

        public event Action SettingClicked;
        public event Action PlayClicked;

        protected override void Awake()
        {
            base.Awake();
            if (settingButton != null)
                settingButton.onClick.AddListener(OnSettingClicked);
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
        }

        void OnSettingClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            SettingClicked?.Invoke();
        }

        void OnPlayClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            PlayClicked?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (settingButton != null) settingButton.onClick.RemoveAllListeners();
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}
