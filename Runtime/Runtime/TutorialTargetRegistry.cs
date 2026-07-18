using System.Collections.Generic;
using SiPVLib.Debugging;

namespace SiPVLib.Tutorial
{
    /// <summary>
    /// Runtime lookup of <see cref="TutorialTarget"/> elements by string id. Populated by targets
    /// registering themselves in OnEnable and unregistering in OnDisable. Main-thread only — the
    /// registry is touched exclusively from Unity UI callbacks and tutorial actions, so no locking
    /// is needed.
    /// </summary>
    public static class TutorialTargetRegistry
    {
        private static readonly Dictionary<string, TutorialTarget> Targets = new();

        public static void Register(TutorialTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.TargetId)) return;

            if (Targets.TryGetValue(target.TargetId, out var existing) && existing != target)
                CustomLog.LogWarning($"[TutorialTargetRegistry] Duplicate target id '{target.TargetId}' — overwriting previous registration.");

            Targets[target.TargetId] = target;
        }

        public static void Unregister(TutorialTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.TargetId)) return;

            // Only remove if the stored entry is this exact instance (avoid clobbering a re-register).
            if (Targets.TryGetValue(target.TargetId, out var existing) && existing == target)
                Targets.Remove(target.TargetId);
        }

        public static bool TryGet(string targetId, out TutorialTarget target)
        {
            if (string.IsNullOrEmpty(targetId)) { target = null; return false; }
            return Targets.TryGetValue(targetId, out target) && target != null;
        }
    }
}
