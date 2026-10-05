using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public class TutorialPanel : BasePanel
    {
        [Header("Content")]
        [SerializeField] GameObject tutorialTextBoardRoot;
        [SerializeField] Text boardText;
        [SerializeField] Text understandButtonText;
        [SerializeField] Button understandButton;

        public event Action UnderstandClicked;

        protected override void Awake()
        {
            base.Awake();

            if (understandButton == null)
            {
                var buttonRoot = transform.Find("UnderstandButton");
                if (buttonRoot != null)
                {
                    understandButton = buttonRoot.GetComponent<Button>();
                    if (understandButton == null)
                        understandButton = buttonRoot.gameObject.AddComponent<Button>();
                }
            }

            if (understandButton == null)
                Debug.LogWarning("TutorialPanel: UnderstandButton is not assigned.");
            else if (understandButton.targetGraphic == null)
                understandButton.targetGraphic = understandButton.GetComponent<Image>();

            if (boardText == null)
            {
                var boardRoot = transform.Find("TutorialTextBoard/TutorialText");
                if (boardRoot != null)
                    boardText = boardRoot.GetComponent<Text>();
            }

            if (tutorialTextBoardRoot == null)
            {
                var board = transform.Find("TutorialTextBoard");
                if (board != null)
                    tutorialTextBoardRoot = board.gameObject;
            }

            if (understandButtonText == null && understandButton != null)
            {
                var label = understandButton.transform.Find("TutorialText");
                if (label != null)
                    understandButtonText = label.GetComponent<Text>();
            }

            if (understandButton != null)
                understandButton.onClick.AddListener(OnUnderstandClicked);
        }

        public void SetContent(string message, string understandLabel, bool showUnderstandButton)
        {
            if (boardText != null)
            {
                boardText.supportRichText = true;
                boardText.text = message ?? string.Empty;
            }

            if (understandButtonText != null)
                understandButtonText.text = understandLabel ?? string.Empty;

            if (understandButton != null)
                understandButton.gameObject.SetActive(showUnderstandButton);
        }

        public void ShowTutorialStep(
            string message,
            bool showTextBoard,
            bool showUnderstandButton,
            string understandLabel = null)
        {
            if (tutorialTextBoardRoot != null)
                tutorialTextBoardRoot.SetActive(showTextBoard);

            SetContent(message, understandLabel ?? string.Empty, showUnderstandButton);
            SetVisibleImmediate(true);
        }

        void OnUnderstandClicked()
        {
            SoundManager.Instance?.PlayButtonClickSound();
            UnderstandClicked?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (understandButton != null)
                understandButton.onClick.RemoveAllListeners();

            base.OnDestroy();
        }
    }
}
