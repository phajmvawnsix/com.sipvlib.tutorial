using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SiPVLib.Tutorial.Config
{
    [TutorialNodeLabel("Node", "#4A90D9")]
    [Serializable]
    public class TutorialNode
    {
        public string id;
        public string nextNodeId;
        public float  delayTime;
        public bool   blockInput = true;
        [SerializeReference] public TutorialAction[] actions;
        [SerializeReference] public TutorialAction[] onCompleteActions;

        // ── Runtime state ──────────────────────────────────────────────────

        /// <summary>Fired when this node finishes all actions and onCompleteActions. Wired by Tutorial.GoNextNode.</summary>
        [NonSerialized] public Action onCompleted;

        [NonSerialized] private bool                   _isCancelled;
        [NonSerialized] private int                    _completedActionsCount;
        [NonSerialized] private int                    _completedOnCompleteActionsCount;
        [NonSerialized] private TutorialRuntimeContext _context;

        protected virtual bool IsCanStart() => true;

        public void StartNode(TutorialRuntimeContext context)
        {
            _context     = context;
            _isCancelled = false;
            StartNodeAsync().Forget();
        }

        private async UniTaskVoid StartNodeAsync()
        {
            if (delayTime > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delayTime));

            if (_isCancelled) return;

            // Hold the node's actions from starting while the tutorial is paused.
            if (_context != null)
            {
                await _context.WaitWhilePaused();
                if (_isCancelled || _context.IsCancelled) return;
            }

            OnNodeStarted();

            if (actions == null || actions.Length == 0)
            {
                StartOnCompleteActions();
                return;
            }

            _completedActionsCount = 0;
            foreach (var action in actions)
            {
                action.onCompleted.AddListener(OnActionCompleted);
                action.Start();
            }
        }

        protected virtual void OnNodeStarted() { }

        /// <summary>Initialises all actions with their owning node and tutorial context before Start is called.</summary>
        public void InitActions(TutorialConfig tutorialConfig)
        {
            if (actions != null)
                foreach (var action in actions)
                    action.Init(this, tutorialConfig);

            if (onCompleteActions != null)
                foreach (var action in onCompleteActions)
                    action.Init(this, tutorialConfig);
        }

        protected void OnActionCompleted(TutorialAction action)
        {
            if (_isCancelled) return;
            if (actions == null) return;

            // O(1) counter instead of O(n) scan
            _completedActionsCount++;
            if (_completedActionsCount < actions.Length) return;

            StartOnCompleteActions();
        }

        protected void StartOnCompleteActions()
        {
            if (_isCancelled) return;

            if (onCompleteActions == null || onCompleteActions.Length == 0)
            {
                OnNodeCompleted();
                return;
            }

            _completedOnCompleteActionsCount = 0;
            foreach (var action in onCompleteActions)
            {
                action.onCompleted.AddListener(OnOnCompleteActionCompleted);
                action.Start();
            }
        }

        private void OnOnCompleteActionCompleted(TutorialAction action)
        {
            if (_isCancelled) return;
            if (onCompleteActions == null) return;

            // O(1) counter instead of O(n) scan
            _completedOnCompleteActionsCount++;
            if (_completedOnCompleteActionsCount < onCompleteActions.Length) return;

            OnNodeCompleted();
        }

        protected virtual void OnNodeCompleted()
        {
            if (_isCancelled) return;
            onCompleted?.Invoke();
        }

        public virtual string GetNextNodeId() => nextNodeId;

        /// <summary>Cancels this node and all running actions without triggering OnCompleted.</summary>
        public void Cancel()
        {
            _isCancelled = true;
            onCompleted  = null;

            if (actions != null)
                foreach (var action in actions)
                {
                    action.onCompleted.RemoveListener(OnActionCompleted);
                    action.Cancel();
                }

            if (onCompleteActions != null)
                foreach (var action in onCompleteActions)
                {
                    action.onCompleted.RemoveListener(OnOnCompleteActionCompleted);
                    action.Cancel();
                }
        }

        // ── Editor ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
        public Vector2 graphPosition;
#endif
    }
}