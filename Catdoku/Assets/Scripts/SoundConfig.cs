using UnityEngine;

namespace ColorCubeShooter
{

    /// <summary>
    /// Shared audio clip configuration for UI and gameplay.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundConfig", menuName = "PixelFlow/Sound Config")]
    public class SoundConfig : ScriptableObject
    {
        [Header("Background Music")]
        [Tooltip("Background music on HomePanel.")]
        public AudioClip homePanelBackgroundMusic;

        [Tooltip("Background music during gameplay.")]
        public AudioClip gameBackgroundMusic;

        [Header("UI SFX")]
        [Tooltip("Button click sound.")]
        public AudioClip buttonClickSound;

        [Header("Game SFX")]
        [Tooltip("Sound on click X")]
        public AudioClip clickXSound;

        [Tooltip("Sound on unClick X")]
        public AudioClip unClickXSound;

        [Tooltip("Sound on Double Click X correct")]
        public AudioClip doubleClickXCorrectSound;
        [Tooltip("Sound on Double Click X incorrect")]
        public AudioClip doubleClickXIncorrectSound;

        [Tooltip("Sound on level complete.")]
        public AudioClip levelCompleteSound;

        [Tooltip("Sound on out of space.")]
        public AudioClip outOfSpaceSound;
    }
}
