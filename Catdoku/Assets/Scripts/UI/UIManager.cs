using UnityEngine;

namespace ColorCubeShooter
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] HomePanel homePanel;
        [SerializeField] GamePanel gamePanel;
        [SerializeField] WinPanel winPanel;
        [SerializeField] LosePanel losePanel;
        [SerializeField] SettingPopup settingPopup;

        public HomePanel HomePanel => homePanel;
        public GamePanel GamePanel => gamePanel;
        public WinPanel WinPanel => winPanel;
        public LosePanel LosePanel => losePanel;
        public SettingPopup SettingPopup => settingPopup;

        public void Initialize()
        {
            homePanel?.SetVisibleImmediate(false);
            gamePanel?.SetVisibleImmediate(false);
            winPanel?.SetVisibleImmediate(false);
            losePanel?.SetVisibleImmediate(false);
            settingPopup?.SetVisibleImmediate(false);
            ShowHome();
        }

        public void ShowHome()
        {
            HideSetting();
            gamePanel?.Hide();
            winPanel?.Hide();
            losePanel?.Hide();
            homePanel?.Show();
            SoundManager.Instance?.PlayHomePanelMusic();
        }

        public void ShowGame()
        {
            HideSetting();
            homePanel?.Hide();
            winPanel?.Hide();
            losePanel?.Hide();
            gamePanel?.Show();
            SoundManager.Instance?.PlayGameMusic();
        }

        public void ShowTutorialBoard()
        {
            HideSetting();
            homePanel?.Hide();
            winPanel?.Hide();
            losePanel?.Hide();
            gamePanel?.SetVisibleImmediate(false);
            SoundManager.Instance?.PlayGameMusic();
        }

        public void SetGamePanelVisible(bool visible)
        {
            if (gamePanel == null) return;

            if (visible)
                gamePanel.Show();
            else
                gamePanel.SetVisibleImmediate(false);
        }

        public void ShowWin()
        {
            HideSetting();
            homePanel?.Hide();
            gamePanel?.Hide();
            losePanel?.Hide();
            winPanel?.Show();
        }

        public void ShowLose()
        {
            HideSetting();
            homePanel?.Hide();
            gamePanel?.Hide();
            winPanel?.Hide();
            losePanel?.Show();
        }

        public void ShowSetting(bool showReplayButton)
        {
            settingPopup?.Show(showReplayButton);
        }

        public void HideSetting()
        {
            if (settingPopup != null && settingPopup.IsVisible)
                settingPopup.Hide();
        }
    }
}
