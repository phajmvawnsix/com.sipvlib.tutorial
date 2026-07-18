using System;
using UnityEngine;
using UnityEngine.Events;

namespace SiPVLib.Tutorial.Config
{
    [Serializable]
    public abstract class TutorialAction
    {
        public abstract string InvalidError();

        // ── Runtime state ──────────────────────────────────────────────────

        public bool IsRunning   { get; protected set; }
        public bool IsCompleted { get; protected set; }
        
        [NonSerialized] public UnityEvent<TutorialAction> onCompleted = new();

        [NonSerialized] protected TutorialNode   _ownerNode;
        [NonSerialized] protected TutorialConfig       _ownerTutorialConfig;

        /// <summary>Called once before Start so actions can reference their owning node/tutorial.</summary>
        public void Init(TutorialNode node, TutorialConfig tutorialConfig)
        {
            _ownerNode     = node;
            _ownerTutorialConfig = tutorialConfig;
        }

        protected virtual bool IsCanStart() => true;
        
        public void Start()
        {
            if (IsRunning || IsCompleted) return;
            if (!IsCanStart()) return;
            IsRunning = true;
            OnStart();
        }

        protected abstract void OnStart();

        public void Complete()
        {
            if (IsCompleted) return;
            IsCompleted = true;
            IsRunning   = false;
            OnComplete();
            onCompleted?.Invoke(this);
        }

        /// <summary>Abort without triggering <see cref="onCompleted"/>. Calls <see cref="OnComplete"/> for cleanup (e.g. unsubscribing events).</summary>
        public void Cancel()
        {
            if (!IsRunning) return;
            IsRunning = false;
            OnComplete(); // reuses cleanup/unsubscribe logic
        }

        protected abstract void OnComplete();

        // ── Editor ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
        public Vector2 graphPosition;
        public virtual string EditorSummary => string.Empty;
#endif
    }
}