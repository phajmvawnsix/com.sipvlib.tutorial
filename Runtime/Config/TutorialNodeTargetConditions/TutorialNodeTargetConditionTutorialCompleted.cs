using System;
using SiPVLib.UserData;

namespace SiPVLib.Tutorial.Config.TutorialNodeTargetConditions
{
    /// <summary>
    /// Branches to <see cref="SiPVLib.Tutorial.Config.TutorialNodeTargetCondition.targetNodeId"/> when the
    /// specified tutorial has been fully completed (its completion flag exists in user-data).
    /// </summary>
    [TutorialConditionLabel("Tutorial Completed", "#20C997")]
    [Serializable]
    public class TutorialNodeTargetConditionTutorialCompleted : TutorialNodeTargetCondition
    {
        /// <summary>ID of the tutorial to check.</summary>
        public string tutorialId;

        public override bool IsConditionMet()
        {
            if (string.IsNullOrEmpty(tutorialId)) return false;

            var mgr = UserDataManager.Instance;
            if (mgr == null || !mgr.IsInitialized) return false;

            return mgr.HasKey(TutorialKeys.CompletedKey(tutorialId));
        }

        public override string InvalidError()
        {
            if (string.IsNullOrWhiteSpace(tutorialId)) return "tutorialId is empty.";
            return null;
        }

#if UNITY_EDITOR
        public override string EditorSummary =>
            string.IsNullOrEmpty(tutorialId) ? "?" : $"Completed: {tutorialId}";
#endif
    }
}

