using UnityEngine;

namespace ColorCubeShooter
{
    public static class CampaignProgressStore
    {
        const string CurrentLevelKey = "campaign_current_level";
        const int DefaultLevel = 1;

        public static int GetCurrentLevel()
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(CurrentLevelKey, DefaultLevel));
        }

        public static void SetCurrentLevel(int levelNumber)
        {
            PlayerPrefs.SetInt(CurrentLevelKey, Mathf.Max(1, levelNumber));
            PlayerPrefs.Save();
        }

        public static void SaveNextLevelAfterPass(int passedLevelNumber)
        {
            var nextLevel = passedLevelNumber + 1;
            if (!LevelLoader.LevelExists(LevelLoader.GetLevelNameByNumber(nextLevel)))
                nextLevel = 1;

            SetCurrentLevel(nextLevel);
        }
    }
}
