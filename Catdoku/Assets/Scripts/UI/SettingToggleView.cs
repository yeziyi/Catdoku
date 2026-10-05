using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCubeShooter
{
    public class SettingToggleView : MonoBehaviour
    {
        [SerializeField] Image backgroundImage;
        [SerializeField] RectTransform dot;
        [SerializeField] Button button;

        [Header("Colors")]
        [SerializeField] Color onColor = new(0.21f, 0.84f, 0.32f, 1f);
        [SerializeField] Color offColor = new(0.75f, 0.75f, 0.75f, 1f);

        [Header("Dot Slide")]
        [SerializeField] float dotOnX = 36.4f;
        [SerializeField] float dotOffX = -36.4f;
        [SerializeField] float slideDuration = 0.15f;

        bool _isOn;
        Action<bool> _onValueChanged;

        public bool IsOn => _isOn;

        void Awake()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (dot == null)
            {
                var dotTransform = transform.Find("ToogleDot");
                if (dotTransform != null)
                    dot = dotTransform as RectTransform;
            }

            if (button == null)
            {
                button = GetComponent<Button>();
                if (button == null)
                    button = gameObject.AddComponent<Button>();

                button.transition = Selectable.Transition.None;
                button.targetGraphic = backgroundImage;
            }
        }

        void OnDestroy()
        {
            dot?.DOKill();
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(Action<bool> onValueChanged)
        {
            _onValueChanged = onValueChanged;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        public void SetValue(bool isOn, bool animate = false)
        {
            _isOn = isOn;
            ApplyVisual(!animate);
        }

        void HandleClick()
        {
            SetValue(!_isOn, animate: true);
            SoundManager.Instance?.PlayButtonClickSound();
            _onValueChanged?.Invoke(_isOn);
        }

        void ApplyVisual(bool immediate)
        {
            if (backgroundImage != null)
                backgroundImage.color = _isOn ? onColor : offColor;

            if (dot == null) return;

            var targetX = _isOn ? dotOnX : dotOffX;
            dot.DOKill();

            if (immediate)
            {
                var pos = dot.anchoredPosition;
                dot.anchoredPosition = new Vector2(targetX, pos.y);
                return;
            }

            dot.DOAnchorPosX(targetX, slideDuration).SetUpdate(true);
        }
    }
}
