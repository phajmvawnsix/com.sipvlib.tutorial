using Cysharp.Threading.Tasks;
using SiPVLib.UI.Component;
using UnityEngine;

namespace SiPVLib.Tutorial
{
    /// <summary>
    /// Optional visual aid for <c>TutorialActionForceTouch</c>: a full-screen dim and an animated
    /// finger/arrow pointed at the target element. Put this component on the root of a Tutorial-layer
    /// UI prefab and register the prefab as a <c>ViewConfig</c>; wire the serialized fields in the
    /// Inspector. See <c>Tutorial/README.md</c> for the prefab setup steps.
    ///
    /// The dim/finger are purely cosmetic — input blocking and tap detection are handled by the
    /// action + <see cref="TutorialTarget"/>, so this view never needs to raycast the target itself.
    /// </summary>
    public sealed class TutorialOverlayView : UIView
    {
        [Tooltip("Full-screen semi-transparent panel shown when dimScreen is requested.")]
        [SerializeField] private GameObject _dim;

        [Tooltip("Finger/arrow graphic moved onto the target element.")]
        [SerializeField] private RectTransform _finger;

        [Tooltip("Pixels the finger bobs toward/away from the target while animating.")]
        [SerializeField] private float _fingerBob = 12f;

        [Tooltip("Bobs per second.")]
        [SerializeField] private float _fingerBobSpeed = 2f;

        private RectTransform _target;
        private Vector3       _fingerBase;
        private bool          _animateFinger;

        public override UniTask Show()
        {
            Apply(UIParam as TutorialOverlayParam);
            return base.Show();
        }

        private void Apply(TutorialOverlayParam param)
        {
            _target        = param?.target;
            _animateFinger = param != null && param.showFinger && _finger != null && _target != null;

            if (_dim != null)
                _dim.SetActive(param != null && param.dimScreen);

            if (_finger != null)
                _finger.gameObject.SetActive(_animateFinger);

            if (_animateFinger)
            {
                // Assumes this overlay's canvas shares the target canvas render mode/camera.
                _finger.position = _target.position;
                _fingerBase      = _finger.localPosition;
            }
        }

        private void Update()
        {
            if (!_animateFinger || _target == null) return;

            // Keep tracking the target in case it moves/scrolls, plus a gentle bob.
            _finger.position = _target.position;
            _fingerBase      = _finger.localPosition;
            var bob = Mathf.Sin(Time.unscaledTime * _fingerBobSpeed * Mathf.PI) * _fingerBob;
            _finger.localPosition = _fingerBase + new Vector3(0f, bob, 0f);
        }
    }
}
