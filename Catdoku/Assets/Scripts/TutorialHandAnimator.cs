using DG.Tweening;
using UnityEngine;

namespace ColorCubeShooter
{
    /// <summary>
    /// Loops a fake double-tap animation on the tutorial hand sprite.
    /// </summary>
    public class TutorialHandAnimator : MonoBehaviour
    {
        [SerializeField] Transform _handRoot;
        [SerializeField] Vector3 _baseScale = new(0.2f, 0.2f, 1f);
        [SerializeField] float _pressScale = 0.82f;
        [SerializeField] float _pressDuration = 0.12f;
        [SerializeField] float _betweenTapsDuration = 0.1f;
        [SerializeField] float _loopPause = 0.55f;
        [SerializeField] int _sortingOrder = 320;

        SpriteRenderer _renderer;
        Tween _loopTween;
        bool _initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        void OnDestroy()
        {
            StopAnimation();
        }

        public void PlayAt(Vector3 worldPosition)
        {
            _loopTween?.Kill();
            _loopTween = null;

            gameObject.SetActive(true);
            EnsureInitialized();

            _handRoot.DOKill();
            _handRoot.position = worldPosition;
            _handRoot.localScale = _baseScale;

            if (_renderer != null)
                _renderer.sortingOrder = _sortingOrder;

            _loopTween = DOTween.Sequence()
                .Append(_handRoot.DOScale(_baseScale * _pressScale, _pressDuration).SetEase(Ease.InQuad))
                .Append(_handRoot.DOScale(_baseScale, _pressDuration).SetEase(Ease.OutQuad))
                .AppendInterval(_betweenTapsDuration)
                .Append(_handRoot.DOScale(_baseScale * _pressScale, _pressDuration).SetEase(Ease.InQuad))
                .Append(_handRoot.DOScale(_baseScale, _pressDuration).SetEase(Ease.OutQuad))
                .AppendInterval(_loopPause)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetTarget(_handRoot);
        }

        public void PlaySingleTapAt(Vector3 worldPosition)
        {
            _loopTween?.Kill();
            _loopTween = null;

            gameObject.SetActive(true);
            EnsureInitialized();

            _handRoot.DOKill();
            _handRoot.position = worldPosition;
            _handRoot.localScale = _baseScale;

            if (_renderer != null)
                _renderer.sortingOrder = _sortingOrder;

            _loopTween = DOTween.Sequence()
                .Append(_handRoot.DOScale(_baseScale * _pressScale, _pressDuration).SetEase(Ease.InQuad))
                .Append(_handRoot.DOScale(_baseScale, _pressDuration).SetEase(Ease.OutQuad))
                .AppendInterval(_loopPause)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetTarget(_handRoot);
        }

        public void StopAnimation()
        {
            _loopTween?.Kill();
            _loopTween = null;

            if (_handRoot != null)
                _handRoot.DOKill();

            gameObject.SetActive(false);
        }

        void EnsureInitialized()
        {
            if (_initialized) return;

            if (_handRoot == null)
                _handRoot = transform;

            _renderer = _handRoot.GetComponent<SpriteRenderer>();

            if (_baseScale.sqrMagnitude < 0.0001f)
            {
                var current = _handRoot.localScale;
                _baseScale = current.sqrMagnitude > 0.0001f ? current : new Vector3(0.2f, 0.2f, 1f);
            }

            _initialized = true;
        }
    }
}
