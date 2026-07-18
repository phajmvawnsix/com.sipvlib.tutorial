using System;
using SiPVLib.Debugging;
using UnityEngine;

namespace SiPVLib.Tutorial.Config
{
    [TutorialNodeLabel("Conditional", "#FF8C00")]
    [Serializable]
    public class TutorialNodeConditional : TutorialNode
    {
        [SerializeReference] public TutorialNodeTargetCondition[] conditions;

        public override string GetNextNodeId()
        {
            if (conditions != null)
                foreach (var condition in conditions)
                    if (condition != null && condition.IsConditionMet())
                        return condition.targetNodeId;

            // Fallback to linear nextNodeId if no condition is met
            var fallback = base.GetNextNodeId();
            if (string.IsNullOrEmpty(fallback))
                CustomLog.LogWarning($"[TutorialNodeConditional] Node '{id}': no condition was met and nextNodeId is empty — tutorial will end here.");
            return fallback;
        }
    }
}