using System;

namespace SiPVLib.Tutorial.Config.TutorialNodeTargetConditions
{
    /// <summary>
    /// A catch-all / "else" condition that always evaluates to true.
    /// Place this as the last condition in a <see cref="TutorialNodeConditional"/> to create an explicit
    /// default branch when no other condition is met. If no Default condition is present, the node's
    /// <c>nextNodeId</c> acts as the automatic fallback.
    /// </summary>
    [TutorialConditionLabel("Default (Always True)", "#6C757D")]
    [Serializable]
    public class TutorialNodeTargetConditionDefault : TutorialNodeTargetCondition
    {
        public override bool IsConditionMet() => true;

        public override string InvalidError() => null; // always valid

#if UNITY_EDITOR
        public override string EditorSummary => "Always True";
#endif
    }
}

