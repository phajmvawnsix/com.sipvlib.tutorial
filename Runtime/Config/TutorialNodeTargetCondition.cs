using System;

namespace SiPVLib.Tutorial.Config
{
    [Serializable]
    public abstract class TutorialNodeTargetCondition
    {
        public string targetNodeId;

        /// <summary>Evaluate the condition at runtime. Returns true if this branch should be taken.</summary>
        public abstract bool IsConditionMet();

        /// <summary>
        /// Returns a non-null error message if this condition is misconfigured, or null when valid.
        /// Used by the Tutorial Graph editor validator.
        /// </summary>
        public virtual string InvalidError() => null;

#if UNITY_EDITOR
        /// <summary>Short human-readable summary of the key condition fields, shown on the graph port label.</summary>
        public virtual string EditorSummary => string.Empty;
#endif
    }
}