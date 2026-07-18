using System;

namespace SiPVLib.Tutorial.Config.TutorialNodeTargetConditions
{
    /// <summary>
    /// Branches to <see cref="SiPVLib.Tutorial.Config.TutorialNodeTargetCondition.targetNodeId"/> when the
    /// specified tutorial is currently running (has been started and not yet completed or skipped).
    /// </summary>
    [TutorialConditionLabel("Tutorial Active", "#17A2B8")]
    [Serializable]
    public class TutorialNodeTargetConditionTutorialActive : TutorialNodeTargetCondition
    {
        /// <summary>ID of the tutorial whose active state to check.</summary>
        public string tutorialId;

        public override bool IsConditionMet()
        {
            if (string.IsNullOrEmpty(tutorialId)) return false;

            var mgr = TutorialManager.Instance;
            return mgr != null && mgr.IsTutorialActive(tutorialId);
        }

        public override string InvalidError()
        {
            if (string.IsNullOrWhiteSpace(tutorialId)) return "tutorialId is empty.";
            return null;
        }

#if UNITY_EDITOR
        public override string EditorSummary =>
            string.IsNullOrEmpty(tutorialId) ? "?" : $"Active: {tutorialId}";
#endif
    }
}

