using System;
using Cysharp.Threading.Tasks;
using SiPVLib.Config;
using SiPVLib.Debugging;
using SiPVLib.UI;
using SiPVLib.UI.Config;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    /// <summary>What to do if the user does not tap the target within <c>softHintDelay</c> seconds.</summary>
    public enum HintTimeoutAction
    {
        None,
        Skip,
        Pause
    }

    /// <summary>
    /// Forces the user to tap a specific UI element (tagged with <see cref="TutorialTarget"/>) before the
    /// tutorial continues. Optionally dims the screen and shows an animated finger via an overlay view.
    /// A soft hint can skip or pause the tutorial if the user does not tap within a delay.
    /// </summary>
    [TutorialActionLabel("Force Touch", "#FF5722")]
    [Serializable]
    public class TutorialActionForceTouch : TutorialAction
    {
        [Tooltip("Id of the TutorialTarget component on the element the user must tap.")]
        public string targetId;

        [Tooltip("Optional Tutorial-layer overlay prefab (ViewConfig) that draws the dim/finger.")]
        [ConfigRef(typeof(ViewConfig))] public string overlayViewId;

        public bool dimScreen;
        public bool showFinger = true;

        [Tooltip("Seconds to wait for a tap before applying onTimeout. 0 = wait forever.")]
        public float softHintDelay;

#if ODIN_INSPECTOR
        [ShowIf("@softHintDelay > 0")]
#endif
        public HintTimeoutAction onTimeout = HintTimeoutAction.None;

        public override string InvalidError() =>
            string.IsNullOrWhiteSpace(targetId) ? "[ForceTouch] targetId is empty" : null;

        [NonSerialized] private TutorialTarget          _target;
        [NonSerialized] private TutorialOverlayView      _overlay;
        [NonSerialized] private System.Threading.CancellationTokenSource _hintCts;

        protected override void OnStart()
        {
            if (!TutorialTargetRegistry.TryGet(targetId, out var target))
            {
                // Target missing (wrong id, or element not yet spawned). Don't hard-block the tutorial.
                CustomLog.LogWarning($"[ForceTouch] No active TutorialTarget with id '{targetId}'. Completing.");
                Complete();
                return;
            }

            _target = target;
            _target.Clicked += OnTargetClicked;

            if (!string.IsNullOrWhiteSpace(overlayViewId))
                ShowOverlayAsync(target).Forget();

            if (softHintDelay > 0f && onTimeout != HintTimeoutAction.None)
            {
                _hintCts = new System.Threading.CancellationTokenSource();
                SoftHintAsync(_hintCts.Token).Forget();
            }
        }

        private async UniTaskVoid ShowOverlayAsync(TutorialTarget target)
        {
            var view = await UIManager.Instance.ShowTutorial<TutorialOverlayView>(overlayViewId,
                new TutorialOverlayParam
                {
                    target     = target.RectTransform,
                    dimScreen  = dimScreen,
                    showFinger = showFinger
                });

            // The action may have finished (tap/skip) while the overlay was spawning.
            if (!IsRunning)
            {
                UIManager.Instance?.HideTutorial(view);
                return;
            }

            _overlay = view;
        }

        private async UniTaskVoid SoftHintAsync(System.Threading.CancellationToken token)
        {
            var canceled = await UniTask.Delay(TimeSpan.FromSeconds(softHintDelay), cancellationToken: token)
                                        .SuppressCancellationThrow();
            if (canceled || !IsRunning) return;

            var tutorialId = _ownerTutorialConfig?.Id;
            if (string.IsNullOrEmpty(tutorialId)) return;

            switch (onTimeout)
            {
                case HintTimeoutAction.Skip:  TutorialManager.Instance?.SkipTutorial(tutorialId);  break;
                case HintTimeoutAction.Pause: TutorialManager.Instance?.PauseTutorial(tutorialId); break;
            }
        }

        private void OnTargetClicked() => Complete();

        protected override void OnComplete()
        {
            if (_target != null)
            {
                _target.Clicked -= OnTargetClicked;
                _target = null;
            }

            if (_hintCts != null)
            {
                _hintCts.Cancel();
                _hintCts.Dispose();
                _hintCts = null;
            }

            if (_overlay != null)
            {
                UIManager.Instance?.HideTutorial(_overlay);
                _overlay = null;
            }
        }

#if UNITY_EDITOR
        public override string EditorSummary => targetId;
#endif
    }
}
