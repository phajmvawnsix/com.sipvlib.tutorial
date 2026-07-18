using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SiPVLib.Config;
using SiPVLib.Event;
using SiPVLib.Debugging;
using SiPVLib.Tutorial.Config;
using SiPVLib.UserData;
using SiPVLib.Utilities;

namespace SiPVLib.Tutorial
{
    public class TutorialManager : MonoSingleton<TutorialManager>
    {
        // ── Event keys ─────────────────────────────────────────────────────
        // All fired through EventManager.Invoke<T>. Subscribe with EventManager.Add<T>(key, handler).

        /// <summary>Fired after a Tutorial starts. Payload: <see cref="TutorialStartEvent"/>.</summary>
        public const string EventTutorialStart = "Tutorial.Start";

        /// <summary>Fired after a Tutorial ends (completed or skipped). Payload: <see cref="TutorialEndEvent"/>.</summary>
        public const string EventTutorialEnd = "Tutorial.End";

        /// <summary>Fired when a Tutorial is skipped (also raises <see cref="EventTutorialEnd"/>). Payload: <see cref="TutorialSkipEvent"/>.</summary>
        public const string EventTutorialSkip = "Tutorial.Skip";

        /// <summary>Fired when a Tutorial is paused. Payload: <see cref="TutorialPauseEvent"/>.</summary>
        public const string EventTutorialPause = "Tutorial.Pause";

        /// <summary>Fired when a paused Tutorial resumes. Payload: <see cref="TutorialResumeEvent"/>.</summary>
        public const string EventTutorialResume = "Tutorial.Resume";

        /// <summary>Fired when a node becomes the current node. Payload: <see cref="TutorialNodeEvent"/>.</summary>
        public const string EventTutorialNodeEnter = "Tutorial.NodeEnter";

        /// <summary>Fired when a node finishes all its actions. Payload: <see cref="TutorialNodeEvent"/>.</summary>
        public const string EventTutorialNodeComplete = "Tutorial.NodeComplete";

        /// <summary>Fired when a checkpoint is saved. Payload: <see cref="TutorialCheckpointEvent"/>.</summary>
        public const string EventTutorialCheckpoint = "Tutorial.Checkpoint";

        // ── State ──────────────────────────────────────────────────────────

        private TutorialConfig[] _allTutorials = Array.Empty<TutorialConfig>();
        private Dictionary<string, TutorialConfig> _tutorialsById = new();
        private readonly Dictionary<string, TutorialConfig> _activeTutorials = new();

        public bool IsInitialized { get; private set; }

        // ── Init ───────────────────────────────────────────────────────────

        public UniTask<bool> Init(Action onSuccess = null, Action<string> onError = null)
        {
            if (IsInitialized) return UniTask.FromResult(true);

            // Load all Tutorial configs sorted by priority
            _allTutorials = ConfigManager.GetAll<TutorialConfig>(ConfigLocation.Local);
            Array.Sort(_allTutorials, (a, b) => a.priority.CompareTo(b.priority));

            // Build O(1) lookup dictionary
            _tutorialsById = new Dictionary<string, TutorialConfig>(_allTutorials.Length);
            foreach (var t in _allTutorials)
                if (t != null && !string.IsNullOrEmpty(t.Id))
                    _tutorialsById[t.Id] = t;

            IsInitialized = true;

            // Restore any tutorial that was still active when the app was last closed.
            ResumeActiveTutorials();

            onSuccess?.Invoke();
            return UniTask.FromResult(true);
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Starts the tutorial with the given ID if it is eligible.</summary>
        public bool StartTutorial(string tutorialId)
        {
            if (!IsInitialized)
            {
                CustomLog.LogError("[TutorialManager] Not initialized. Call Init() first.");
                return false;
            }

            if (_activeTutorials.ContainsKey(tutorialId))
            {
                CustomLog.LogWarning($"[TutorialManager] Tutorial '{tutorialId}' is already active.");
                return false;
            }

            var config = FindTutorial(tutorialId);
            if (config == null)
            {
                CustomLog.LogError($"[TutorialManager] Tutorial '{tutorialId}' not found in configs.");
                return false;
            }

            if (!config.IsCanStart())
            {
                CustomLog.LogWarning($"[TutorialManager] Tutorial '{tutorialId}' cannot start (already completed or not eligible).");
                return false;
            }

            var firstTime = IsFirstCompletion(tutorialId);

            var instance = config.CloneAsRuntimeInstance();
            if (!instance.Start()) return false;

            _activeTutorials[tutorialId] = instance;
            SetMarker(TutorialKeys.ActiveKey(tutorialId), true);

            EventManager.Invoke(EventTutorialStart, new TutorialStartEvent
            {
                tutorialId  = tutorialId,
                isFirstTime = firstTime
            });

            return true;
        }

        /// <summary>Skips the active tutorial if it is skippable.</summary>
        public bool SkipTutorial(string tutorialId)
        {
            if (!_activeTutorials.TryGetValue(tutorialId, out var instance)) return false;

            var config = FindTutorial(tutorialId);
            if (config != null && !config.isSkippable)
            {
                CustomLog.LogWarning($"[TutorialManager] Tutorial '{tutorialId}' is not skippable.");
                return false;
            }

            instance.Cancel();
            _activeTutorials.Remove(tutorialId);
            ClearRunMarkers(tutorialId);

            EventManager.Invoke(EventTutorialSkip, new TutorialSkipEvent { tutorialId = tutorialId });
            EventManager.Invoke(EventTutorialEnd, new TutorialEndEvent
            {
                tutorialId  = tutorialId,
                isSkipped   = true,
                isFirstTime = false
            });

            return true;
        }

        /// <summary>Pauses the active tutorial. Node-to-node progression halts until <see cref="ResumeTutorial"/>.</summary>
        public bool PauseTutorial(string tutorialId)
        {
            if (!_activeTutorials.TryGetValue(tutorialId, out var instance)) return false;
            if (instance.IsPaused) return false;

            instance.Pause();
            SetMarker(TutorialKeys.PausedKey(tutorialId), true);

            EventManager.Invoke(EventTutorialPause, new TutorialPauseEvent
            {
                tutorialId = tutorialId,
                nodeId     = instance.CurrentNodeId
            });

            return true;
        }

        /// <summary>Resumes a paused tutorial.</summary>
        public bool ResumeTutorial(string tutorialId)
        {
            if (!_activeTutorials.TryGetValue(tutorialId, out var instance)) return false;
            if (!instance.IsPaused) return false;

            instance.Resume();
            DeleteMarker(TutorialKeys.PausedKey(tutorialId));

            EventManager.Invoke(EventTutorialResume, new TutorialResumeEvent
            {
                tutorialId = tutorialId,
                nodeId     = instance.CurrentNodeId
            });

            return true;
        }

        /// <summary>Returns whether the tutorial with given ID is currently running.</summary>
        public bool IsTutorialActive(string tutorialId) => _activeTutorials.ContainsKey(tutorialId);

        /// <summary>Returns whether the tutorial with given ID is running and paused.</summary>
        public bool IsTutorialPaused(string tutorialId) =>
            _activeTutorials.TryGetValue(tutorialId, out var instance) && instance.IsPaused;

        // ── Internal callbacks from Tutorial/Node ──────────────────────────

        /// <summary>Called by checkpoint actions to persist the current progress.</summary>
        internal void OnTutorialNodeCheckpoint(string tutorialId, string nodeId)
        {
            if (_activeTutorials.TryGetValue(tutorialId, out var instance))
                instance.OnCheckPointReached(nodeId);
        }

        /// <summary>Called by TutorialConfig when its last node finishes.</summary>
        internal void OnTutorialCompleted(string tutorialId, bool isFirstTime)
        {
            _activeTutorials.Remove(tutorialId);
            ClearRunMarkers(tutorialId);

            EventManager.Invoke(EventTutorialEnd, new TutorialEndEvent
            {
                tutorialId  = tutorialId,
                isSkipped   = false,
                isFirstTime = isFirstTime
            });
        }

        /// <summary>Raises <see cref="EventTutorialNodeEnter"/>. Called by TutorialConfig.</summary>
        internal void RaiseNodeEnter(string tutorialId, string nodeId) =>
            EventManager.Invoke(EventTutorialNodeEnter, new TutorialNodeEvent { tutorialId = tutorialId, nodeId = nodeId });

        /// <summary>Raises <see cref="EventTutorialNodeComplete"/>. Called by TutorialConfig.</summary>
        internal void RaiseNodeComplete(string tutorialId, string nodeId) =>
            EventManager.Invoke(EventTutorialNodeComplete, new TutorialNodeEvent { tutorialId = tutorialId, nodeId = nodeId });

        /// <summary>Raises <see cref="EventTutorialCheckpoint"/>. Called by TutorialConfig.</summary>
        internal void RaiseCheckpoint(string tutorialId, string nodeId) =>
            EventManager.Invoke(EventTutorialCheckpoint, new TutorialCheckpointEvent { tutorialId = tutorialId, nodeId = nodeId });

        // ── Helpers ────────────────────────────────────────────────────────

        private TutorialConfig FindTutorial(string tutorialId) =>
            _tutorialsById.TryGetValue(tutorialId, out var cfg) ? cfg : null;

        /// <summary>Returns true if the tutorial has not yet been completed (no completion flag in UserData).</summary>
        private bool IsFirstCompletion(string tutorialId)
        {
            var mgr = UserDataManager.Instance;
            return mgr == null || !mgr.IsInitialized || !mgr.HasKey(TutorialKeys.CompletedKey(tutorialId));
        }

        /// <summary>Re-starts any tutorial whose Active marker survived the last session, restoring paused state.</summary>
        private void ResumeActiveTutorials()
        {
            var mgr = UserDataManager.Instance;
            if (mgr == null || !mgr.IsInitialized) return;

            foreach (var config in _allTutorials)
            {
                if (config == null || string.IsNullOrEmpty(config.Id)) continue;

                var id = config.Id;
                if (!mgr.HasKey(TutorialKeys.ActiveKey(id))) continue;

                var wasPaused = mgr.HasKey(TutorialKeys.PausedKey(id));

                // StartTutorial resumes from the saved checkpoint (see TutorialConfig.Start).
                if (!StartTutorial(id))
                {
                    // No longer eligible (e.g. completed elsewhere) — clear the stale marker.
                    ClearRunMarkers(id);
                    continue;
                }

                if (wasPaused) PauseTutorial(id);
            }
        }

        private void ClearRunMarkers(string tutorialId)
        {
            DeleteMarker(TutorialKeys.ActiveKey(tutorialId));
            DeleteMarker(TutorialKeys.PausedKey(tutorialId));
        }

        private static void SetMarker(string key, bool value)
        {
            var mgr = UserDataManager.Instance;
            if (mgr != null && mgr.IsInitialized) mgr.Set(key, value);
        }

        private static void DeleteMarker(string key)
        {
            var mgr = UserDataManager.Instance;
            if (mgr != null && mgr.IsInitialized) mgr.Delete(key);
        }
    }

    /// <summary>Payload for <see cref="TutorialManager.EventTutorialStart"/>.</summary>
    public struct TutorialStartEvent
    {
        public string tutorialId;
        public bool   isFirstTime;
    }

    /// <summary>Payload for <see cref="TutorialManager.EventTutorialEnd"/>.</summary>
    public struct TutorialEndEvent
    {
        public string tutorialId;
        public bool   isSkipped;
        public bool   isFirstTime;
    }

    /// <summary>Payload for <see cref="TutorialManager.EventTutorialSkip"/>.</summary>
    public struct TutorialSkipEvent
    {
        public string tutorialId;
    }

    /// <summary>Payload for <see cref="TutorialManager.EventTutorialPause"/>.</summary>
    public struct TutorialPauseEvent
    {
        public string tutorialId;
        public string nodeId;
    }

    /// <summary>Payload for <see cref="TutorialManager.EventTutorialResume"/>.</summary>
    public struct TutorialResumeEvent
    {
        public string tutorialId;
        public string nodeId;
    }

    /// <summary>Payload for <see cref="TutorialManager.EventTutorialNodeEnter"/> and <see cref="TutorialManager.EventTutorialNodeComplete"/>.</summary>
    public struct TutorialNodeEvent
    {
        public string tutorialId;
        public string nodeId;
    }

    /// <summary>Payload for <see cref="TutorialManager.EventTutorialCheckpoint"/>.</summary>
    public struct TutorialCheckpointEvent
    {
        public string tutorialId;
        public string nodeId;
    }
}
