namespace SiPVLib.Tutorial
{
    /// <summary>
    /// Centralised keys for Tutorial-related UserData entries.
    /// Eliminates magic-string duplication across TutorialConfig, TutorialManager,
    /// and the TutorialCompleted condition.
    /// </summary>
    internal static class TutorialKeys
    {
        private const string CheckpointPrefix = "Tutorial_Checkpoint_";
        private const string CompletedSuffix  = "_completed";
        private const string ActivePrefix     = "Tutorial_Active_";
        private const string PausedPrefix      = "Tutorial_Paused_";

        /// <summary>Key used to store the current checkpoint node ID for a tutorial.</summary>
        public static string CheckpointKey(string tutorialId) =>
            $"{CheckpointPrefix}{tutorialId}";

        /// <summary>Key used to mark a tutorial as fully completed.</summary>
        public static string CompletedKey(string tutorialId) =>
            $"{CheckpointPrefix}{tutorialId}{CompletedSuffix}";

        /// <summary>Key marking a tutorial as currently active (mid-run). Enables auto-resume after an app restart.</summary>
        public static string ActiveKey(string tutorialId) =>
            $"{ActivePrefix}{tutorialId}";

        /// <summary>Key marking a tutorial as paused, so a killed app relaunches into the paused state.</summary>
        public static string PausedKey(string tutorialId) =>
            $"{PausedPrefix}{tutorialId}";
    }
}

