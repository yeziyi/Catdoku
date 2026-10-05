using UnityEngine;

namespace ColorCubeShooter
{
    public static class TutorialProgressStore
    {
        const string CompletedKey = "tutorial_completed";

        public static bool IsCompleted => PlayerPrefs.GetInt(CompletedKey, 0) != 0;

        public static void MarkCompleted()
        {
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.Save();
        }
    }
}
