using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SiPVLib.Tutorial
{
    /// <summary>
    /// Tags a UI element so tutorial actions (e.g. Force Touch) can find it by id and detect a tap on it.
    /// Attach to any interactable UI element and give it a unique <see cref="TargetId"/>.
    /// If the element has a <see cref="Button"/> the click is auto-hooked; otherwise the component
    /// also implements <see cref="IPointerClickHandler"/> so any element with a raycast target reports taps.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialTarget : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string _targetId;

        /// <summary>Unique id used by tutorial actions to locate this element.</summary>
        public string TargetId => _targetId;

        /// <summary>The element's RectTransform, used to position highlight/finger overlays.</summary>
        public RectTransform RectTransform { get; private set; }

        /// <summary>Raised when the user taps this element.</summary>
        public event Action Clicked;

        private Button _button;

        private void Awake()
        {
            RectTransform = transform as RectTransform;
            _button       = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(RaiseClicked);
            TutorialTargetRegistry.Register(this);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(RaiseClicked);
            TutorialTargetRegistry.Unregister(this);
        }

        // Reports taps for non-Button elements (Images, custom widgets) that are raycast targets.
        public void OnPointerClick(PointerEventData eventData)
        {
            // Avoid double-firing when a Button already handled the click.
            if (_button == null) RaiseClicked();
        }

        private void RaiseClicked() => Clicked?.Invoke();
    }
}
