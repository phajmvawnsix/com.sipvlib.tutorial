using System;
using SiPVLib.Config;
using SiPVLib.UI;
using SiPVLib.UI.BaseTypes;
using SiPVLib.UI.Config;

namespace SiPVLib.Tutorial.Config.TutorialNodeTargetConditions
{
    [TutorialConditionLabel("View Inactive", "#DC3545")]
    [Serializable]
    public class TutorialNodeTargetConditionViewInactive : TutorialNodeTargetCondition
    {
        [ConfigRef(typeof(ViewConfig))] public string viewId;
        public ViewLayer layer;
        
        public override bool IsConditionMet()
        {
            return !UIManager.Instance.IsViewActive(viewId, layer);
        }

        public override string InvalidError()
        {
            if (string.IsNullOrWhiteSpace(viewId)) return "viewId is empty.";
            return null;
        }

#if UNITY_EDITOR
        public override string EditorSummary => string.IsNullOrEmpty(viewId) ? "?" : $"{layer} | {viewId}";
#endif
    }
}