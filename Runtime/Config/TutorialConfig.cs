using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SiPVLib.Config.Configs;
using SiPVLib.Debugging;
using SiPVLib.UserData;
using SiPVLib.Utilities.Extensions;
using UnityEngine;

namespace SiPVLib.Tutorial.Config
{
    public class TutorialConfig : GameConfig
    {
        public int  priority;
        public bool isRepeatable;
        public bool isSkippable;
        [SerializeReference] public List<TutorialNode> nodes;

        // ── Runtime state ──────────────────────────────────────────────────

        protected bool _isRuntimeInstance;

        [NonSerialized] private TutorialNode                     _currentNode;
        [NonSerialized] private Dictionary<string, TutorialNode> _nodeById;
        [NonSerialized] private TutorialRuntimeContext           _context;

        /// <summary>ID of the node currently running, or null when idle/finished.</summary>
        public string CurrentNodeId => _currentNode?.id;

        /// <summary>True while this run is paused.</summary>
        public bool IsPaused => _context != null && _context.IsPaused;

        public TutorialConfig CloneAsRuntimeInstance()
        {
            var clone = this.DeepClone();
            clone._isRuntimeInstance = true;
            return clone;
        }

        /// <summary>Returns the saved checkpoint node ID, or null if none exists (first run).</summary>
        public string GetLastCheckpointId()
        {
            var mgr = UserDataManager.Instance;
            if (mgr == null || !mgr.IsInitialized) return null;

            var val = mgr.Get<string>(TutorialKeys.CheckpointKey(_id));
            return string.IsNullOrEmpty(val) ? null : val;
        }

        /// <summary>Returns true if this tutorial is eligible to start.</summary>
        public bool IsCanStart()
        {
            if (nodes == null || nodes.Count == 0) return false;
            if (isRepeatable) return true;

            var mgr = UserDataManager.Instance;
            if (mgr == null || !mgr.IsInitialized) return true; // assume first run when data unavailable

            return !mgr.HasKey(TutorialKeys.CompletedKey(_id));
        }

        /// <summary>Begins the tutorial. Must be called on a cloned runtime instance.</summary>
        public bool Start()
        {
            if (!_isRuntimeInstance)
            {
                CustomLog.LogError($"[Tutorial] '{_id}' must be started via CloneAsRuntimeInstance().");
                return false;
            }

            if (nodes == null || nodes.Count == 0)
            {
                CustomLog.LogWarning($"[Tutorial] '{_id}' has no nodes.");
                return false;
            }

            if (!IsCanStart())
            {
                CustomLog.LogWarning($"[Tutorial] '{_id}' IsCanStart() returned false.");
                return false;
            }

            // Fresh per-run context (pause flag + cancellation)
            _context = new TutorialRuntimeContext();

            // Build O(1) node lookup dictionary
            _nodeById = new Dictionary<string, TutorialNode>(nodes.Count);
            foreach (var n in nodes)
                if (n != null && !string.IsNullOrEmpty(n.id))
                    _nodeById[n.id] = n;

            // Clear checkpoint for repeatable tutorials so they restart from the beginning
            var mgr = UserDataManager.Instance;
            if (isRepeatable && mgr != null && mgr.IsInitialized)
                mgr.Delete(TutorialKeys.CheckpointKey(_id));

            // Resolve entry node: resume from checkpoint, otherwise start at index 0
            var checkpointId = GetLastCheckpointId();
            TutorialNode entryNode = null;

            if (!string.IsNullOrEmpty(checkpointId))
                _nodeById.TryGetValue(checkpointId, out entryNode);

            entryNode ??= nodes[0];

            if (entryNode == null)
            {
                CustomLog.LogError($"[Tutorial] '{_id}' could not resolve an entry node.");
                return false;
            }

            SetCurrentNode(entryNode);
            return true;
        }

        private void SetCurrentNode(TutorialNode node)
        {
            _currentNode             = node;
            _currentNode.onCompleted = GoNextNode;
            _currentNode.InitActions(this);
            TutorialManager.Instance?.RaiseNodeEnter(_id, node.id);
            _currentNode.StartNode(_context);
        }

        private void GoNextNode() => GoNextNodeAsync().Forget();

        private async UniTaskVoid GoNextNodeAsync()
        {
            var finished = _currentNode;
            if (finished == null) return;

            TutorialManager.Instance?.RaiseNodeComplete(_id, finished.id);

            // Gate the node-to-node advance on pause; a running node's in-flight waits are not
            // interrupted, but the tutorial will not move to the next node while paused.
            if (_context != null)
            {
                await _context.WaitWhilePaused();
                if (_context.IsCancelled) return;
            }

            // A cancel or restart during the pause replaced/cleared the current node.
            if (_currentNode != finished) return;

            var nextId = finished.GetNextNodeId();
            TutorialNode nextNode = null;
            if (!string.IsNullOrEmpty(nextId))
                _nodeById?.TryGetValue(nextId, out nextNode);

            if (nextNode == null)
            {
                // Tutorial finished. Compute first-time BEFORE writing the completion flag,
                // otherwise the flag we just set makes it always read as a repeat.
                var mgr       = UserDataManager.Instance;
                var firstTime = mgr == null || !mgr.IsInitialized || !mgr.HasKey(TutorialKeys.CompletedKey(_id));

                if (mgr != null && mgr.IsInitialized)
                {
                    mgr.Delete(TutorialKeys.CheckpointKey(_id));
                    mgr.Set(TutorialKeys.CompletedKey(_id), true);
                }

                _currentNode = null;
                TutorialManager.Instance?.OnTutorialCompleted(_id, firstTime);
                return;
            }

            SetCurrentNode(nextNode);
        }

        /// <summary>Saves the given nodeId as the current checkpoint.</summary>
        public void OnCheckPointReached(string nodeId)
        {
            var mgr = UserDataManager.Instance;
            if (mgr == null || !mgr.IsInitialized) return;
            mgr.Set(TutorialKeys.CheckpointKey(_id), nodeId);
            TutorialManager.Instance?.RaiseCheckpoint(_id, nodeId);
        }

        /// <summary>Pauses node-to-node progression. In-flight action waits keep running.</summary>
        public void Pause() => _context?.Pause();

        /// <summary>Resumes a paused run.</summary>
        public void Resume() => _context?.Resume();

        /// <summary>Cancels the running node chain without completing the tutorial.</summary>
        public void Cancel()
        {
            _context?.Cancel();
            _currentNode?.Cancel();
            _currentNode = null;
        }

        // ── Editor ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
        [Serializable]
        public class TutorialGraphAnnotation
        {
            public string  text     = "Note";
            public Vector2 position;
            public Vector2 size     = new Vector2(200, 100);
        }

        [HideInInspector] public List<TutorialGraphAnnotation> graphAnnotations;
#endif
    }
}