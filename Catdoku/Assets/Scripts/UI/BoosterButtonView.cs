using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public class BoosterButtonView : MonoBehaviour
    {
        [SerializeField] GameObject valueObject;
        [SerializeField] Text valueText;
        [SerializeField] GameObject adsObject;
        [SerializeField] Button button;

        bool _showingAds;

        public event Action UseRequested;
        public event Action AdRequested;

        void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
                if (button == null)
                    button = gameObject.AddComponent<Button>();

                var image = GetComponent<Image>();
                if (image != null)
                {
                    button.transition = Selectable.Transition.ColorTint;
                    button.targetGraphic = image;
                }
            }

            button.onClick.AddListener(HandleClick);
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void SetCharges(int charges)
        {
            _showingAds = charges <= 0;

            if (valueObject != null)
                valueObject.SetActive(!_showingAds);

            if (adsObject != null)
                adsObject.SetActive(_showingAds);

            if (valueText != null && !_showingAds)
                valueText.text = charges.ToString();
        }

        void HandleClick()
        {
            SoundManager.Instance?.PlayButtonClickSound();

            if (_showingAds)
                AdRequested?.Invoke();
            else
                UseRequested?.Invoke();
        }
    }
}
